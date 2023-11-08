using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

public sealed record VariableModel
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required VariableValueType ValueType { get; set; }
}

public sealed record VariablesEditModel
{
    public required string Json { get; set; }

    public required List<int> ApplicationIds { get; set; }

    public required List<int> TagIds { get; set; }
}

public sealed class VariableManager
{
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ILogger<VariableManager> _logger;
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

    public async Task<string> GetConfigAsync(int apiKeyId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting config for API key {ApiKeyId}", apiKeyId);

        var applications = dbContext.Applications;
        var apiKeys = dbContext.ApiKeys;
        var tags = dbContext.Tags;
        var apiKeyTags = dbContext.ApiKeyTags;
        var variables = dbContext.Variables;
        var tagVariables = dbContext.TagVariables;
        var applicationVariables = dbContext.ApplicationVariables;

        // Get API key
        var apiKey = await apiKeys.SingleAsync(a => a.Id == apiKeyId, cancellationToken);

        // Get tags of API key
        var tagIdsOfApiKey = await tags
            .Where(tag => apiKeyTags.Any(at => at.TagId == tag.Id && at.ApiKeyId == apiKeyId))
            .Select(tag => tag.Id)
            .ToListAsync(cancellationToken);

        // Get variables of API key
        var variablesOfApiKey = await variables
            // If a variable is linked to any other tag than the API key tags, then it is not relevant to this API key
            .Where(v => !tagVariables.Any(tv => tv.VariableId == v.Id && !tagIdsOfApiKey.Contains(tv.TagId)))
            // If a variable is linked to one or more applications and the current application is not one of them, then it is not relevant to this API key
            .Where(v => !applicationVariables.Any(av => av.VariableId == v.Id)
                        || applicationVariables.Any(av => av.VariableId == v.Id && av.ApplicationId == apiKey.ApplicationId))
            // For each variable, we want to calculate a specificity to decide which variable overrides which other variable
            .Select(v => new
            {
                Variable = new VariableModel
                {
                    Key = v.Key,
                    Value = v.Value,
                    ValueType = v.ValueType
                },
                Specificity = tagVariables.Count(tv => tv.VariableId == v.Id && tagIdsOfApiKey.Contains(tv.TagId))
                              + applicationVariables.Count(av => av.VariableId == v.Id && av.ApplicationId == apiKey.ApplicationId)
            })
            .ToListAsync(cancellationToken);

        // Take into account that we might have overlapping variables with the same key. In that case, the most "specific" variable wins
        var variablesOfApiKeyWithHighestSpecificity = variablesOfApiKey
            .GroupBy(v => v.Variable.Key)
            .Select(g => g.MaxBy(v => v.Specificity)!.Variable)
            .ToList();

        _logger.LogInformation("Got {NumberOfVariables} variables for API key {ApiKeyId}", variablesOfApiKeyWithHighestSpecificity.Count, apiKeyId);

        return _serializer.SerializeToJson(variablesOfApiKeyWithHighestSpecificity);
    }

    public async Task SaveAsync(VariablesEditModel model, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tagIds = model.TagIds;
        var applicationIds = model.ApplicationIds;
        _logger.LogDebug("Saving variables for applications {@ApplicationIds} and tags {@TagIds}", applicationIds, tagIds);

        var variables = dbContext.Variables;
        var tagVariables = dbContext.TagVariables;
        var applicationVariables = dbContext.ApplicationVariables;

        IQueryable<VariableRecord> existingVariablesQuery = variables;

        existingVariablesQuery = existingVariablesQuery.Where(v =>
            !tagVariables.Any(tv => tv.VariableId == v.Id && !tagIds.Contains(tv.TagId))
            && tagVariables.Count(tv => tv.VariableId == v.Id) == tagIds.Count);

        existingVariablesQuery = existingVariablesQuery.Where(v =>
            !applicationVariables.Any(av => av.VariableId == v.Id && !applicationIds.Contains(av.ApplicationId))
            && applicationVariables.Count(av => av.VariableId == v.Id) == applicationIds.Count);

        var existingVariables = await existingVariablesQuery.ToListAsync(cancellationToken);
        var existingVariablesByKey = existingVariables.GroupBy(v => v.Key).ToDictionary(group => group.Key, values => values.ToList());
        var newVariables = _deserializer.DeserializeFromJson(model.Json);

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
            }

            foreach (var tagId in tagIds)
            {
                tagVariables.Add(new TagVariableRecord
                {
                    TagId = tagId,
                    VariableId = addedVariable.Id
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class VariablesJsonSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
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

    public List<VariableModel> DeserializeFromJson(string json)
    {
        return Deserialize(json, _nodeOptions, _documentOptions).OrderBy(v => v.Key).ToList();

        static IEnumerable<VariableModel> Deserialize(
            string json,
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
                                ValueType = VariableValueType.Boolean
                            };
                        }
                        else if (jsonValue.TryGetValue(out double _))
                        {
                            yield return new VariableModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.Number
                            };
                        }
                        else
                        {
                            yield return new VariableModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.String
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
