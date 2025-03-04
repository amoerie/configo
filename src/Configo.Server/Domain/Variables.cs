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
    public required int? TagId { get; set; }
}

public sealed record VariablesEditModel
{
    public required string Json { get; set; }
    public required List<int> ApplicationIds { get; set; }
    public required int? TagId { get; set; }
}

public sealed record VariablesPendingChanges(List<VariablesEditModel> EditModels)
{
    public VariablesPendingChanges() : this(new List<VariablesEditModel>()) {}
}

public sealed record VariableManager
{
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ILogger<VariableManager> _logger;
    private readonly VariablesPendingChanges _pendingChanges;
    private readonly SemaphoreSlim _pendingChangesLock = new SemaphoreSlim(1, 1);
    private readonly VariablesJsonDeserializer _deserializer;
    private readonly VariablesJsonSerializer _serializer;

    public VariableManager(
        IDbContextFactory<ConfigoDbContext> dbContextFactory,
        ILogger<VariableManager> logger,
        VariablesPendingChanges pendingChanges,
        VariablesJsonDeserializer deserializer,
        VariablesJsonSerializer serializer)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pendingChanges = pendingChanges ?? throw new ArgumentNullException(nameof(pendingChanges));
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
        var apiKey = await apiKeys.SingleAsync(a => a.Id == apiKeyId, cancellationToken);

        // Get tags of API key
        var tagIds = await apiKeyTags
            .Where(akt => akt.ApiKeyId == apiKeyId)
            .OrderBy(akt => akt.Order)
            .Select(akt => akt.TagId)
            .ToListAsync(cancellationToken);

        var applicationIds = new List<int> { apiKey.ApplicationId };

        return await GetMergedConfigAsync(applicationIds, tagIds, cancellationToken);
    }

    /// <summary>
    /// Gets configuration variables that are defined for the specified applications and tags, or a subset thereof
    /// If multiple variables with the same key exist, the one linked to the last tag will win
    /// </summary>
    public async Task<string> GetMergedConfigAsync(List<int> applicationIds, List<int> tagIds, CancellationToken cancellationToken)
    {
        return await GetMergedConfigAsync(applicationIds, tagIds, includePendingChanges: false, cancellationToken);
    }

    /// <summary>
    /// Gets configuration variables that are defined for the specified applications and tags, or a subset thereof, including any pending changes
    /// The most specific variable is the one constrained to the highest number of applications and tags
    /// </summary>
    public async Task<string> GetMergedConfigWithPendingChangesAsync(List<int> applicationIds, List<int> tagIds, CancellationToken cancellationToken)
    {
        return await GetMergedConfigAsync(applicationIds, tagIds, includePendingChanges: true, cancellationToken);
    }
    
    private async Task<string> GetMergedConfigAsync(List<int> applicationIds, List<int> tagIds, bool includePendingChanges, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting merged config for applications {@ApplicationIds} and tags {@TagIds}", applicationIds, tagIds);

        var variablesDbSet = dbContext.Variables;
        var applicationVariablesDbSet = dbContext.ApplicationVariables;
        
        var variablesQuery = variablesDbSet
            // If a variable is linked to any other tag, then it is not relevant
            .Where(v => v.TagId == null || tagIds.Contains(v.TagId.Value));

        if (applicationIds.Count > 0)
        {
            variablesQuery = variablesQuery.Where(v => 
                applicationVariablesDbSet.Any(av => av.VariableId == v.Id && applicationIds.Contains(av.ApplicationId))
                || !applicationVariablesDbSet.Any(av => av.VariableId == v.Id));
        }
        else
        {
            variablesQuery = variablesQuery.Where(v => !applicationVariablesDbSet.Any(av => av.VariableId == v.Id));
        }
        
        var variables = await variablesQuery
            .Select(v => new VariableModel
                {
                    Key = v.Key,
                    Value = v.Value,
                    ValueType = v.ValueType,
                    TagId = v.TagId,
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

            var matchingPendingChanges = pendingChanges.Where(c => c.TagId is null || tagIds.Contains(c.TagId.Value));

            if (applicationIds.Count > 0)
            {
                matchingPendingChanges = matchingPendingChanges.Where(c => c.ApplicationIds.Any(applicationIds.Contains) || c.ApplicationIds.Count == 0);
            }
            else
            {
                matchingPendingChanges = matchingPendingChanges.Where(c => c.ApplicationIds.Count == 0);
            }

            foreach (var pendingChange in matchingPendingChanges)
            {
                var pendingVariables = _deserializer.DeserializeFromJson(pendingChange.Json, pendingChange.TagId);
                variables.AddRange(pendingVariables);
            }
        }

        // Take into account that we might have overlapping variables with the same key. In that case, the most "specific" variable wins
        var mergedVariables = variables
            .GroupBy(v => v.Key)
            .Select(group => group.MaxBy(g => g.TagId is null ? -1 : tagIds.IndexOf(g.TagId.Value))!)
            .ToList();

        _logger.LogInformation("Got {NumberOfVariables} variables for applications {@ApplicationIds} and tags {@TagIds}", mergedVariables.Count, applicationIds, tagIds);

        return _serializer.SerializeToJson(mergedVariables);
    }
    
    /// <summary>
    /// Gets configuration variables that are defined exactly for this combination of applications and tag
    /// </summary>
    public async Task<string> GetConfigAsync(List<int> applicationIds, int? tagId, CancellationToken cancellationToken)
    {
        return await GetConfigAsync(applicationIds, tagId, includePendingChanges: false, cancellationToken);
    }
    
    /// <summary>
    /// Gets configuration variables that are defined exactly for this combination of applications and tags, including any pending changes
    /// </summary>
    public async Task<string> GetConfigWithPendingChangesAsync(List<int> applicationIds, int? tagId, CancellationToken cancellationToken)
    {
        return await GetConfigAsync(applicationIds, tagId, includePendingChanges: true, cancellationToken);
    }
    
    /// <summary>
    /// Gets configuration variables that are defined exactly for this combination of applications and tag
    /// </summary>
    public async Task<string> GetConfigAsync(List<int> applicationIds, int? tagId, bool includePendingChanges, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting config for applications {@ApplicationIds} and tag {@TagId}", applicationIds, tagId);

        var variables = dbContext.Variables;
        var applicationVariables = dbContext.ApplicationVariables;

        var variablesQuery = variables
            .Where(variable => variable.TagId == tagId)
            .Where(variable => !applicationVariables.Any(av => av.VariableId == variable.Id && !applicationIds.Contains(av.ApplicationId)));

        foreach (var applicationId in applicationIds)
        {
            variablesQuery = variablesQuery.Where(variable => applicationVariables.Any(av => av.VariableId == variable.Id && av.ApplicationId == applicationId));
        }

        var matchingVariables = await variablesQuery
            .Select(variable => new VariableModel
            {
                Key = variable.Key,
                Value = variable.Value,
                ValueType = variable.ValueType,
                TagId = variable.TagId,
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
                .Where(pendingChange => pendingChange.TagId == tagId)
                .Where(pendingChange => pendingChange.ApplicationIds.All(applicationIds.Contains));

            foreach (var applicationId in applicationIds)
            {
                matchingPendingChanges = matchingPendingChanges.Where(pendingChange => pendingChange.ApplicationIds.Contains(applicationId));
            }
            
            foreach (var pendingChange in matchingPendingChanges)
            {
                matchingVariables.AddRange(_deserializer.DeserializeFromJson(pendingChange.Json, pendingChange.TagId));
            }
            
            // Ensure that duplicate keys are pruned, but that pending changes win over variables from the database
            matchingVariables = matchingVariables.AsEnumerable()
                .Reverse()
                .DistinctBy(v => v.Key)
                .Reverse()
                .ToList();
        }

        _logger.LogInformation("Got {NumberOfVariables} variables for applications {@ApplicationIds} and tag {@TagId}", matchingVariables.Count, applicationIds, tagId);

        return _serializer.SerializeToJson(matchingVariables);
    }
    
    public async Task SaveToPendingAsync(VariablesEditModel model, CancellationToken cancellationToken)
    {
        model.ApplicationIds.Sort();
        await _pendingChangesLock.WaitAsync(cancellationToken);
        try
        {
            var existing = _pendingChanges.EditModels.SingleOrDefault(m =>
                m.ApplicationIds.SequenceEqual(model.ApplicationIds)
                && m.TagId == model.TagId);

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
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tagId = model.TagId;
        var applicationIds = model.ApplicationIds;
        _logger.LogDebug("Saving variables for applications {@ApplicationIds} and tag {@TagId}", applicationIds, tagId);

        var variables = dbContext.Variables;
        var applicationVariables = dbContext.ApplicationVariables;

        IQueryable<VariableRecord> existingVariablesQuery = variables;

        existingVariablesQuery = existingVariablesQuery.Where(v => v.TagId == tagId);

        existingVariablesQuery = existingVariablesQuery.Where(v =>
            !applicationVariables.Any(av => av.VariableId == v.Id && !applicationIds.Contains(av.ApplicationId))
            && applicationVariables.Count(av => av.VariableId == v.Id) == applicationIds.Count);

        var existingVariables = await existingVariablesQuery.AsTracking().ToListAsync(cancellationToken);
        var existingVariablesByKey = existingVariables.GroupBy(v => v.Key).ToDictionary(group => group.Key, values => values.ToList());
        var newVariables = _deserializer.DeserializeFromJson(model.Json, model.TagId);

        // Add or update variables
        var addedVariables = new List<VariableRecord>();
        foreach (var newVariable in newVariables)
        {
            var key = newVariable.Key;
            var value = newVariable.Value;
            var valueType = newVariable.ValueType;

            if (existingVariablesByKey.Remove(key, out var existingVariablesWithSameKey))
            {
                var existingVariable = existingVariablesWithSameKey[0];
                if (!string.Equals(value, existingVariable.Value)
                    || valueType != existingVariable.ValueType)
                {
                    existingVariable.Value = value;
                    existingVariable.ValueType = valueType;
                    existingVariable.UpdatedAtUtc = DateTime.UtcNow;
                    _logger.LogInformation("Updated existing variable: {@Variable}", existingVariable);
                }
                
                // Somehow we managed to have duplicate keys for the same tag + application combinations, clean those up now
                foreach (var variableToDelete in existingVariablesWithSameKey.Skip(1))
                {
                    _logger.LogWarning("Deleted existing variable because of duplicate key: {@Variable}", existingVariable);
                    dbContext.Variables.Remove(variableToDelete);
                }
            }
            else
            {
                var variableToAdd = new VariableRecord
                {
                    Key = key,
                    Value = value,
                    ValueType = valueType,
                    TagId = tagId,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                variables.Add(variableToAdd);

                addedVariables.Add(variableToAdd);
            }
        }

        // Delete old variables
        foreach (var group in existingVariablesByKey.Values)
        {
            foreach (var variableToDelete in group)
            {
                _logger.LogInformation("Deleted existing variable: {@Variable}", variableToDelete);
                variables.Remove(variableToDelete);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        // Link new variables to applications and tags
        foreach (var addedVariable in addedVariables)
        {
            _logger.LogInformation("Added new variable: {@Variable}", addedVariable);

            foreach (var applicationId in applicationIds)
            {
                applicationVariables.Add(new ApplicationVariableRecord
                {
                    ApplicationId = applicationId,
                    VariableId = addedVariable.Id
                });
                
                _logger.LogInformation("Linked variable {@VariableId} to application {ApplicationId}", addedVariable, applicationId);
            }
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
                            jsonValue = JsonValue.Create(value)!;
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

    public List<VariableModel> DeserializeFromJson(string json, int? tagId)
    {
        return Deserialize(json, tagId, _nodeOptions, _documentOptions).OrderBy(v => v.Key).ToList();

        static IEnumerable<VariableModel> Deserialize(
            string json,
            int? tagId,
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
                                TagId = tagId
                            };
                        }
                        else if (jsonValue.TryGetValue(out double _))
                        {
                            yield return new VariableModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.Number,
                                TagId = tagId
                            };
                        }
                        else
                        {
                            yield return new VariableModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.String,
                                TagId = tagId
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
