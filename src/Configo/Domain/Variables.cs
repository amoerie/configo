using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Configo.Database;
using Configo.Database.Tables;
using Microsoft.EntityFrameworkCore;

namespace Configo.Domain;

public sealed record VariableForConfigModel
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required VariableValueType ValueType { get; set; }
}

public sealed class VariableManager
{
    private readonly IDbContextFactory<ConfigoDbContext> _dbContextFactory;
    private readonly ILogger<VariableManager> _logger;

    public VariableManager(IDbContextFactory<ConfigoDbContext> dbContextFactory, ILogger<VariableManager> logger)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<VariableForConfigModel>> GetConfigAsync(int apiKeyId, CancellationToken cancellationToken)
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
            .Select(variable => new VariableForConfigModel
            {
                Key = variable.Key,
                Value = variable.Value,
                ValueType = variable.ValueType
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Got {NumberOfVariables} variables for API key {ApiKeyId}", apiKeyId);

        return variables;
    }
}

public class VariablesJsonSerializer
{
    public string SerializeToJson(List<VariableForConfigModel> variables)
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

        return root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.Strict
        });
    }
}

public class VariablesJsonDeserializer
{
    public List<VariableForConfigModel> DeserializeFromJson(string json)
    {
        return Deserialize(json).ToList();
        
        static IEnumerable<VariableForConfigModel> Deserialize(string json)
        {
            var jsonObject = JsonNode.Parse(json)!.AsObject();

            var stack = new Stack<(JsonNode Node, string Path)>();
            stack.Push((jsonObject, ""));

            while (stack.Count > 0)
            {
                var (node, path) = stack.Pop();

                switch (node)
                {
                    case JsonValue jsonValue:
                        var stringValue = jsonValue.GetValue<string>();
                        if (jsonValue.TryGetValue(out bool _))
                        {
                            yield return new VariableForConfigModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.Boolean
                            };
                        }
                        else if (jsonValue.TryGetValue(out double _))
                        {
                            yield return new VariableForConfigModel
                            {
                                Key = path,
                                Value = stringValue,
                                ValueType = VariableValueType.Number
                            };
                        }
                        else
                        {
                            yield return new VariableForConfigModel
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
