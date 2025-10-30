using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Server.Domain;

public sealed record VariableModel
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required VariableValueType ValueType { get; set; }
    public required int TagId { get; set; }
    public required int ApplicationId { get; set; }
}

public sealed record VariablesEditModel
{
    public required string Json { get; set; }
    public required int TagId { get; set; }
    public required int ApplicationId { get; set; }
}

public sealed record VariablesPendingChanges(List<VariablesEditModel> EditModels)
{
    public VariablesPendingChanges() : this(new List<VariablesEditModel>()) { }
}

public sealed record VariableManager
{
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ILogger<VariableManager> _logger;
    private readonly VariablesPendingChanges _pendingChanges = new([]);
    private readonly SemaphoreSlim _pendingChangesLock = new(1, 1);
    private readonly VariablesJsonDeserializer _deserializer;
    private readonly VariablesJsonSerializer _serializer;

    public VariableManager(
        IDbContextFactory<ConfigoDbContext> dbContextFactory,
        ILogger<VariableManager> logger,
        VariablesJsonDeserializer deserializer,
        VariablesJsonSerializer serializer)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// Gets configuration variables that are defined for the specified API key.
    /// An API key is linked to one application and zero or more tags
    /// If multiple variables with the same key exist, the one linked to the last tag will win
    /// </summary>
    public async Task<string> GetMergedConfigAsync(int apiKeyId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting config for API key {ApiKeyId}", apiKeyId);

        var apiKeys = dbContext.ApiKeys;
        var apiKeyTags = dbContext.ApiKeyTags;

        // Get API key
        var apiKey = await apiKeys.FirstOrDefaultAsync(a => a.Id == apiKeyId, cancellationToken);

        if (apiKey is null)
        {
            throw new ArgumentException("API key with id {ApiKeyId} not found", nameof(apiKeyId));
        }

        // Get tags of API key
        var tagIds = await apiKeyTags
            .Where(akt => akt.ApiKeyId == apiKeyId)
            .Join(dbContext.Tags, a => a.TagId, t => t.Id, (apiKeyTag, tag) => new { ApiKeyTag = apiKeyTag, Tag = tag })
            .Join(dbContext.TagGroups, a => a.Tag.TagGroupId, t => t.Id,
                (apiKeyTagAndTag, tagGroup) => new { apiKeyTagAndTag.ApiKeyTag, apiKeyTagAndTag.Tag, TagGroup = tagGroup })
            .OrderBy(x => x.TagGroup.Order)
            .Select(x => x.Tag.Id)
            .ToListAsync(cancellationToken);

        return await GetMergedConfigAsync(apiKey.ApplicationId, tagIds, cancellationToken);
    }

    /// <summary>
    /// Gets configuration variables that are defined for the specified tags, or a subset thereof
    /// If multiple variables with the same key exist, the one linked to the last tag will win
    /// </summary>
    public async Task<string> GetMergedConfigAsync(int applicationId, List<int> tagIds, CancellationToken cancellationToken)
    {
        return await GetMergedConfigAsync(applicationId, tagIds, includePendingChanges: false, cancellationToken);
    }

    /// <summary>
    /// Gets configuration variables that are defined for the specified applications and tags, or a subset thereof, including any pending changes
    /// The most specific variable is the one constrained to the highest number of applications and tags
    /// </summary>
    public async Task<string> GetMergedConfigWithPendingChangesAsync(int applicationId, List<int> tagIds, CancellationToken cancellationToken)
    {
        return await GetMergedConfigAsync(applicationId, tagIds, includePendingChanges: true, cancellationToken);
    }

    public async Task<List<VariablesEditModel>> GetPendingChangesAsync(CancellationToken cancellationToken)
    {
        List<VariablesEditModel> pendingChanges;
        
        await _pendingChangesLock.WaitAsync(cancellationToken);
        try
        {
            pendingChanges = _pendingChanges.EditModels.ToList();
        }
        finally
        {
            _pendingChangesLock.Release();
        }

        return pendingChanges;
    }

    private async Task<string> GetMergedConfigAsync(int applicationId, List<int> tagIds, bool includePendingChanges, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting merged config for tags {@TagIds}", tagIds);

        var variablesDbSet = dbContext.Variables;

        var variablesQuery = variablesDbSet
            // If a variable is linked to any other tag or any other application, then it is not relevant
            .Where(v => tagIds.Contains(v.TagId) && v.ApplicationId == applicationId);

        var variables = await variablesQuery
            .Select(v => new VariableModel
            {
                Key = v.Key,
                Value = v.Value,
                ValueType = v.ValueType,
                TagId = v.TagId,
                ApplicationId = v.ApplicationId
            })
            .ToListAsync(cancellationToken);

        if (includePendingChanges)
        {
            var pendingChanges = await GetPendingChangesAsync(cancellationToken);
            var matchingPendingChanges = pendingChanges.Where(c => tagIds.Contains(c.TagId) && c.ApplicationId == applicationId);

            foreach (var pendingChange in matchingPendingChanges)
            {
                var pendingVariables = _deserializer.DeserializeFromJson(pendingChange.Json, pendingChange.TagId, pendingChange.ApplicationId);
                variables.AddRange(pendingVariables);
            }
        }

        // Take into account that we might have overlapping variables with the same key. In that case, the most "specific" variable wins
        var mergedVariables = variables
            .GroupBy(v => v.Key)
            .Select(group => group.MaxBy(g => tagIds.IndexOf(g.TagId))!)
            .ToList();

        _logger.LogInformation("Got {NumberOfVariables} variables for tags {@TagIds}", mergedVariables.Count, tagIds);

        return _serializer.SerializeToJson(mergedVariables);
    }

    /// <summary>
    /// Gets configuration variables that are defined exactly for this tag
    /// </summary>
    public async Task<string> GetConfigAsync(int tagId, int applicationId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(tagId);
        return await GetConfigAsync(tagId, applicationId, includePendingChanges: false, cancellationToken);
    }

    /// <summary>
    /// Gets configuration variables that are defined exactly for this tag, including any pending changes
    /// </summary>
    public async Task<string> GetConfigWithPendingChangesAsync(int tagId, int applicationId, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(tagId);
        return await GetConfigAsync(tagId, applicationId, includePendingChanges: true, cancellationToken);
    }

    /// <summary>
    /// Gets configuration variables that are defined exactly for this tag
    /// </summary>
    public async Task<string> GetConfigAsync(int tagId, int applicationId, bool includePendingChanges, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(tagId);
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting config for tag {@TagId}", tagId);

        var variables = dbContext.Variables;

        var variablesQuery = variables
            .Where(variable => variable.TagId == tagId && variable.ApplicationId == applicationId);

        var matchingVariables = await variablesQuery
            .Select(variable => new VariableModel
            {
                Key = variable.Key,
                Value = variable.Value,
                ValueType = variable.ValueType,
                TagId = variable.TagId,
                ApplicationId = variable.ApplicationId
            })
            .ToListAsync(cancellationToken);

        if (includePendingChanges)
        {
            List<VariablesEditModel> pendingChanges;
            await _pendingChangesLock.WaitAsync(cancellationToken);
            try
            {
                pendingChanges = _pendingChanges.EditModels.ToList();
            }
            finally
            {
                _pendingChangesLock.Release();
            }

            // Ensure that the last changes have highest priority
            pendingChanges.Reverse();

            var matchingPendingChanges = pendingChanges
                .Where(pendingChange => pendingChange.TagId == tagId && pendingChange.ApplicationId == applicationId);

            foreach (var pendingChange in matchingPendingChanges)
            {
                matchingVariables.AddRange(_deserializer.DeserializeFromJson(pendingChange.Json, pendingChange.TagId, pendingChange.ApplicationId));
            }

            // Ensure that duplicate keys are pruned, but that pending changes win over variables from the database
            matchingVariables = matchingVariables.AsEnumerable()
                .Reverse()
                .DistinctBy(v => v.Key)
                .Reverse()
                .ToList();
        }

        _logger.LogInformation("Got {NumberOfVariables} variables for tag {@TagId}", matchingVariables.Count, tagId);

        return _serializer.SerializeToJson(matchingVariables);
    }

    public async Task SaveToPendingAsync(VariablesEditModel model, CancellationToken cancellationToken)
    {
        await _pendingChangesLock.WaitAsync(cancellationToken);
        try
        {
            var existing = _pendingChanges.EditModels.SingleOrDefault(m => m.TagId == model.TagId && m.ApplicationId == model.ApplicationId);
            if (existing != null)
            {
                existing.Json = model.Json;
            }
            else
            {
                _pendingChanges.EditModels.Add(model);
            }
        }
        finally
        {
            _pendingChangesLock.Release();
        }
    }

    public async Task SaveAsync(VariablesEditModel model, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentOutOfRangeException.ThrowIfZero(model.TagId);
        ArgumentOutOfRangeException.ThrowIfZero(model.ApplicationId);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tagId = model.TagId;
        var applicationId = model.ApplicationId;
        _logger.LogDebug("Saving variables for tag {TagId} and application {ApplicationId}", tagId, applicationId);

        var variables = dbContext.Variables;
        var existingVariables = await variables.Where(v => v.TagId == tagId && v.ApplicationId == applicationId).AsTracking().ToListAsync(cancellationToken);
        var newVariables = _deserializer.DeserializeFromJson(model.Json, model.TagId, model.ApplicationId);

        // Add or update variables
        var savedVariables = new List<VariableRecord>();
        foreach (var newVariable in newVariables)
        {
            var key = newVariable.Key;
            var value = newVariable.Value;
            var valueType = newVariable.ValueType;

            var existingVariable = existingVariables.SingleOrDefault(v => v.Key == key);
            if (existingVariable is not null)
            {
                if (!string.Equals(value, existingVariable.Value) || valueType != existingVariable.ValueType)
                {
                    existingVariable.Value = value;
                    existingVariable.ValueType = valueType;
                    existingVariable.UpdatedAtUtc = DateTime.UtcNow;
                    _logger.LogInformation("Updated existing variable: {@Variable}", existingVariable);
                }
                else
                {
                    _logger.LogDebug("Skipped updating existing variable because value or value type did not change: {@Variable}", existingVariable);
                }

                savedVariables.Add(existingVariable);
            }
            else
            {
                var variableToAdd = new VariableRecord
                {
                    Key = key,
                    Value = value,
                    ValueType = valueType,
                    TagId = tagId,
                    ApplicationId = applicationId,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                variables.Add(variableToAdd);

                savedVariables.Add(variableToAdd);
            }
        }

        // Delete old variables
        var variablesToDelete = existingVariables.Except(savedVariables).ToList();
        foreach (var variableToDelete in variablesToDelete)
        {
            _logger.LogInformation("Deleted existing variable: {@Variable}", variableToDelete);
            variables.Remove(variableToDelete);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SavePendingChangesAsync(CancellationToken cancellationToken)
    {
        var pendingChanges = new List<VariablesEditModel>();
        await _pendingChangesLock.WaitAsync(cancellationToken);
        try
        {
            pendingChanges.AddRange(_pendingChanges.EditModels);
            _pendingChanges.EditModels.Clear();
        }
        finally
        {
            _pendingChangesLock.Release();
        }

        foreach (var pendingChange in pendingChanges)
        {
            await SaveAsync(pendingChange, cancellationToken);
        }
    }

    public async Task DiscardPendingChangesAsync(CancellationToken cancellationToken)
    {
        await _pendingChangesLock.WaitAsync(cancellationToken);
        try
        {
            _pendingChanges.EditModels.Clear();
        }
        finally
        {
            _pendingChangesLock.Release();
        }
    }

    public async Task DiscardPendingChangeAsync(VariablesEditModel pendingChange, CancellationToken cancellationToken)
    {
        await _pendingChangesLock.WaitAsync(cancellationToken);
        try
        {
            _pendingChanges.EditModels.RemoveAll(e => e.TagId == pendingChange.TagId && e.ApplicationId == pendingChange.ApplicationId);
        }
        finally
        {
            _pendingChangesLock.Release();
        }
    }

    public async Task<bool> HasPendingChangesAsync(CancellationToken cancellationToken)
    {
        await _pendingChangesLock.WaitAsync(cancellationToken);
        try
        {
            return _pendingChanges.EditModels.Count > 0;
        }
        finally
        {
            _pendingChangesLock.Release();
        }
    }
}

public class VariablesJsonSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.Strict,
    };

    public string SerializeToJson(List<VariableModel> variables)
    {
        // Enforce ascending order of variable keys
        variables = variables.OrderBy(v => v.Key).ToList();

        var root = new JsonObject();
        var indexLookup = new Dictionary<string, int>();
        foreach (var variable in variables)
        {
            var key = variable.Key;
            var value = variable.Value;
            var valueType = variable.ValueType;

            var path = key.Split(":");
            JsonNode parent = root;

            // For example, Foo:Bar=Test should become  "Foo": { "Bar": "Test }
            for (var index = 0; index < path.Length; index++)
            {
                string current = path[index];
                var isLast = index == path.Length - 1;
                if (!isLast)
                {
                    // We have to either prep an object or an array
                    var next = path[index + 1];
                    var isArray = int.TryParse(next, out _);

                    JsonNode? node;
                    switch (parent)
                    {
                        case JsonArray parentArray:
                            var originalParentIndex = int.Parse(current);
                            var parentPath = parentArray.GetPath();
                            var indexLookupKey = parentPath + ":" + originalParentIndex;
                            if (indexLookup.TryGetValue(indexLookupKey, out var parentIndex))
                            {
                                node = parentArray[parentIndex]!;
                            }
                            else
                            {
                                node = isArray ? new JsonArray() : new JsonObject();

                                parentArray.Add(node);
                                indexLookup.Add(indexLookupKey, parentArray.Count - 1);
                            }

                            break;
                        case JsonObject parentObject:
                            node = parentObject[current];

                            if (node == null)
                            {
                                node = isArray ? new JsonArray() : new JsonObject();

                                parentObject.Add(current, node);
                            }

                            break;
                        default:
                            throw new UnreachableException();
                    }

                    parent = node;
                }
                else
                {
                    // We have to write the value
                    JsonValue jsonValue;
                    switch (valueType)
                    {
                        case VariableValueType.String:
                            jsonValue = JsonValue.Create(value);
                            break;
                        case VariableValueType.Number:
                            jsonValue = JsonValue.Create(long.Parse(value));
                            break;
                        case VariableValueType.Boolean:
                            jsonValue = JsonValue.Create(bool.Parse(value));
                            break;
                        default:
                            throw new UnreachableException();
                    }

                    switch (parent)
                    {
                        case JsonArray jsonArray:
                            jsonArray.Add(jsonValue);
                            break;
                        case JsonObject jsonObject:
                            jsonObject.Add(current, jsonValue);
                            break;
                        default:
                            throw new UnreachableException();
                    }
                }
            }
        }

        return root.ToJsonString(_jsonSerializerOptions);
    }
}

public class VariablesJsonDeserializer
{
    private readonly JsonNodeOptions _nodeOptions = new JsonNodeOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly JsonDocumentOptions _documentOptions = new JsonDocumentOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public List<VariableModel> DeserializeFromJson(string json, int tagId, int applicationId)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }
        
        return Deserialize(json, tagId, applicationId, _nodeOptions, _documentOptions).OrderBy(v => v.Key).ToList();

        static IEnumerable<VariableModel> Deserialize(
            string json,
            int tagId,
            int applicationId,
            JsonNodeOptions nodeOptions,
            JsonDocumentOptions documentOptions)
        {
            var jsonObject = JsonNode.Parse(json, nodeOptions, documentOptions)!.AsObject();

            var stack = new Stack<(JsonNode Node, string Path)>();
            stack.Push((jsonObject, ""));

            while (stack.Count > 0)
            {
                var (node, path) = stack.Pop();

                switch (node)
                {
                    case JsonValue jsonValue:
                        var stringValue = jsonValue.ToString();
                        if (jsonValue.TryGetValue(out bool _))
                        {
                            yield return new VariableModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.Boolean,
                                TagId = tagId,
                                ApplicationId = applicationId
                            };
                        }
                        else if (jsonValue.TryGetValue(out double _))
                        {
                            yield return new VariableModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.Number,
                                TagId = tagId,
                                ApplicationId = applicationId
                            };
                        }
                        else
                        {
                            yield return new VariableModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.String,
                                TagId = tagId,
                                ApplicationId = applicationId
                            };
                        }

                        break;
                    case JsonObject nestedJsonObject:
                        foreach (var property in nestedJsonObject)
                        {
                            var nextPath = path == ""
                                ? property.Key
                                : $"{path}:{property.Key}";

                            var nextNode = property.Value;
                            if (nextNode != null)
                            {
                                stack.Push((nextNode, nextPath));
                            }
                        }

                        break;
                    case JsonArray jsonArray:
                        for (var i = jsonArray.Count - 1; i >= 0; i--)
                        {
                            var nextPath = $"{path}:{i}";
                            var nextNode = jsonArray[i];
                            if (nextNode != null)
                            {
                                stack.Push((nextNode, nextPath));
                            }
                        }

                        break;
                }
            }
        }
    }
}
