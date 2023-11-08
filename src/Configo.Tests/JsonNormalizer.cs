using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Configo.Tests;

public class JsonNormalizer
{
    public static string Normalize(string json)
    {
        var jsonObject = Normalize(JsonNode.Parse(json))!;
        return jsonObject.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.Strict,
        });
    }

    private static JsonNode? Normalize(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }

        switch (node)
        {
            case JsonArray jsonArray:
                var normalizedArray = new JsonArray();
                foreach (var property in jsonArray)
                {
                    normalizedArray.Add(Normalize(property));
                }

                return normalizedArray;
            case JsonObject jsonObject:
                var normalizedObject = new JsonObject();
                foreach (var property in jsonObject.OrderBy(p => p.Key))
                {
                    var key = property.Key;
                    var value = property.Value;
                    normalizedObject.Add(key, Normalize(value));
                }

                return normalizedObject;
            case JsonValue jsonValue:
                var normalizedValue = JsonNode.Parse(jsonValue.ToJsonString())!.AsValue();
                return normalizedValue;
            default:
                throw new ArgumentOutOfRangeException(nameof(node));
        }
    }
}
