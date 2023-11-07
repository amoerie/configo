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
    public required List<int> TagIds { get; set; }
}

public sealed class VariableManager
{
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ILogger<VariableManager> _logger;
    private readonly VariablesJsonDeserializer _deserializer;

    public VariableManager(
        IDbContextFactory<ConfigoDbContext> dbContextFactory,
        ILogger<VariableManager> logger,
        VariablesJsonDeserializer deserializer)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    }

    public async Task<List<VariableModel>> GetConfigAsync(int apiKeyId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Getting config for API key {ApiKeyId}", apiKeyId);

        // Get tags of API key
        var tags = await dbContext.Tags
            .GroupJoin(dbContext.ApiKeyTags.Where(apiKeyTag => apiKeyTag.ApiKeyId == apiKeyId),
                t => t.Id,
                a => a.TagId,
                (tag, _) => tag)
            .ToListAsync(cancellationToken);

        var tagIds = tags.Select(t => t.Id).ToList();

        // Get variables that are linked to ALL tags
        var variables = await dbContext.TagVariables
            .Where(tv => tagIds.Contains(tv.TagId))
            .GroupBy(tv => tv.VariableId)
            .Where(g => g.Count() == tags.Count)
            .Select(tv => tv.Key)
            .Join(dbContext.Variables,
                variableId => variableId,
                variable => variable.Id,
                (variableId, variable) => variable)
            .Select(variable => new VariableModel
            {
                Key = variable.Key,
                Value = variable.Value,
                ValueType = variable.ValueType
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfVariables} variables for API key {ApiKeyId}", apiKeyId);

        return variables;
    }

    public async Task SaveAsync(VariablesEditModel model, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        _logger.LogDebug("Saving variables for tags {@TagIds}", model.TagIds);
        
        // Get tags
        var tags = await dbContext.Tags
            .Where(tag => model.TagIds.Contains(tag.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        // Get existing variables
        var existingVariables = await dbContext.TagVariables
            .Where(tv => model.TagIds.Contains(tv.TagId))
            .GroupBy(tv => tv.VariableId)
            .Where(g => g.Count() == tags.Count)
            .Select(tv => tv.Key)
            .Join(dbContext.Variables,
                variableId => variableId,
                variable => variable.Id,
                (variableId, variable) => variable)
            .AsTracking()
            .ToListAsync(cancellationToken);
        
        var newVariables = _deserializer.DeserializeFromJson(model.Json);
        
        
        
        

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
