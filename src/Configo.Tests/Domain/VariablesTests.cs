using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Configo.Database.Tables;
using Configo.Domain;

namespace Configo.Tests.Domain;

public class VariablesJsonSerializerTests
{
    private readonly VariablesJsonSerializer _jsonSerializer = new VariablesJsonSerializer();

    private string NormalizeJson(string json)
    {
        var jsonObject = Normalize(JsonNode.Parse(json))!;
        return jsonObject.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.Strict,
        });
    }

    private JsonNode? Normalize(JsonNode? node)
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

    [Fact]
    public void SimpleKeyValue()
    {
        // Arrange
        var variables = new List<VariableForConfigModel>
        {
            new VariableForConfigModel { Key = "Foo", Value = "Bar", ValueType = VariableValueType.String }
        };
        var expected = """"
                       {
                           "Foo": "Bar"
                       }
                       """";

        // Act
        var actual = _jsonSerializer.SerializeToJson(variables);

        // Assert
        Assert.Equal(NormalizeJson(expected), NormalizeJson(actual));
    }

    [Fact]
    public void SimpleObject()
    {
        // Arrange
        var variables = new List<VariableForConfigModel>
        {
            new VariableForConfigModel { Key = "Foo:Bar", Value = "Test", ValueType = VariableValueType.String }
        };
        var expected = """"
                       {
                           "Foo":
                           {
                               "Bar": "Test"
                           }
                       }
                       """";

        // Act
        var actual = _jsonSerializer.SerializeToJson(variables);

        // Assert
        Assert.Equal(NormalizeJson(expected), NormalizeJson(actual));
    }

    [Fact]
    public void Array()
    {
        // Arrange
        var variables = new List<VariableForConfigModel>
        {
            new VariableForConfigModel { Key = "Foo:0", Value = "Test1", ValueType = VariableValueType.String },
            new VariableForConfigModel { Key = "Foo:1", Value = "Test2", ValueType = VariableValueType.String }
        };
        var expected = """"
                       {
                           "Foo": [ "Test1", "Test2" ]
                       }
                       """";

        // Act
        var actual = _jsonSerializer.SerializeToJson(variables);

        // Assert
        Assert.Equal(NormalizeJson(expected), NormalizeJson(actual));
    }

    [Fact]
    public void ComplexObject()
    {
        // Arrange
        var variables = new List<VariableForConfigModel>
        {
            new VariableForConfigModel
                { Key = "Panel Members:0:FirstName", Value = "Stephen", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Panel Members:0:LastName", Value = "Fry", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Panel Members:1:FirstName", Value = "Alan", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Panel Members:1:LastName", Value = "Davies", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Number Of Guests", Value = "145", ValueType = VariableValueType.Number },
            new VariableForConfigModel { Key = "Was Recorded", Value = "true", ValueType = VariableValueType.Boolean },
        };
        var expected = """"
                       {
                           "Panel Members": [
                                { "FirstName": "Stephen", "LastName": "Fry" },
                                {  "FirstName": "Alan", "LastName": "Davies" }
                            ],
                            "Number Of Guests": 145,
                            "Was Recorded": true
                       }
                       """";

        // Act
        var actual = _jsonSerializer.SerializeToJson(variables);

        // Assert
        Assert.Equal(NormalizeJson(expected), NormalizeJson(actual));
    }

    [Fact]
    public void EdgeCases()
    {
        // Arrange
        var variables = new List<VariableForConfigModel>
        {
            new VariableForConfigModel
                { Key = "0", Value = "Arrays can't exist at the root level", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "1", Value = "So they should be properties instead", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "DeletedVariables:1", Value = "Sometimes variables", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "DeletedVariables:3", Value = "Get deleted", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "DeletedVariables:5", Value = "Which causes gaps", ValueType = VariableValueType.String },
        };
        var expected = """"
                       {
                           "0": "Arrays can't exist at the root level",
                           "1": "So they should be properties instead",
                           "DeletedVariables": [ "Sometimes variables", "Get deleted", "Which causes gaps" ]
                       }
                       """";

        // Act
        var actual = _jsonSerializer.SerializeToJson(variables);

        // Assert
        Assert.Equal(NormalizeJson(expected), NormalizeJson(actual));
    }
}

public class VariablesJsonDeserializerTests
{
    private readonly VariablesJsonDeserializer _jsonDeserializer = new VariablesJsonDeserializer();

    [Fact]
    public void SimpleKeyValue()
    {
        // Arrange
        var json = """"
                   {
                       "Foo": "Bar"
                   }
                   """";
        var expected = new List<VariableForConfigModel>
        {
            new VariableForConfigModel { Key = "Foo", Value = "Bar", ValueType = VariableValueType.String }
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SimpleObject()
    {
        // Arrange
        var json = """"
                   {
                       "Foo":
                       {
                           "Bar": "Test"
                       }
                   }
                   """";
        var expected = new List<VariableForConfigModel>
        {
            new VariableForConfigModel { Key = "Foo:Bar", Value = "Test", ValueType = VariableValueType.String }
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Array()
    {
        // Arrange
        var json = """"
                   {
                       "Foo": [ "Test1", "Test2" ]
                   }
                   """";
        var expected = new List<VariableForConfigModel>
        {
            new VariableForConfigModel { Key = "Foo:0", Value = "Test1", ValueType = VariableValueType.String },
            new VariableForConfigModel { Key = "Foo:1", Value = "Test2", ValueType = VariableValueType.String }
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json);

        // Assert
        Assert.Equal(expected, actual);
    }
    
    

    [Fact]
    public void ComplexObject()
    {
        // Arrange
        var json = """"
                       {
                           "Panel Members": [
                                { "FirstName": "Stephen", "LastName": "Fry" },
                                {  "FirstName": "Alan", "LastName": "Davies" }
                            ],
                            "Number Of Guests": 145,
                            "Was Recorded": true
                       }
                       """";
        var expected = new List<VariableForConfigModel>
        {
            new VariableForConfigModel
                { Key = "Panel Members:0:FirstName", Value = "Stephen", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Panel Members:0:LastName", Value = "Fry", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Panel Members:1:FirstName", Value = "Alan", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Panel Members:1:LastName", Value = "Davies", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "Number Of Guests", Value = "145", ValueType = VariableValueType.Number },
            new VariableForConfigModel { Key = "Was Recorded", Value = "true", ValueType = VariableValueType.Boolean },
        };
        
        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EdgeCases()
    {
        // Arrange
        var json = """"
                   {
                       "0": "Arrays can't exist at the root level",
                       "1": "So they should be properties instead",
                   }
                   """";
        var expected = new List<VariableForConfigModel>
        {
            new VariableForConfigModel
                { Key = "0", Value = "Arrays can't exist at the root level", ValueType = VariableValueType.String },
            new VariableForConfigModel
                { Key = "1", Value = "So they should be properties instead", ValueType = VariableValueType.String },
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json);

        // Assert
        Assert.Equal(expected, actual);
    }
}
