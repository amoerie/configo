using Configo.Database.Tables;
using Configo.Server.Domain;

namespace Configo.Tests.Server.Domain;

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
        var expected = new List<VariableModel>
        {
            new VariableModel { Key = "Foo", Value = "Bar", ValueType = VariableValueType.String, TagId = null }
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json, null);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WithTagId()
    {
        // Arrange
        var json = """"
                   {
                       "Foo": "Bar"
                   }
                   """";
        var expected = new List<VariableModel>
        {
            new VariableModel { Key = "Foo", Value = "Bar", ValueType = VariableValueType.String, TagId = 7 }
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json, 7);

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
        var expected = new List<VariableModel>
        {
            new VariableModel { Key = "Foo:Bar", Value = "Test", ValueType = VariableValueType.String, TagId = null }
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json, null);

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
        var expected = new List<VariableModel>
        {
            new VariableModel { Key = "Foo:0", Value = "Test1", ValueType = VariableValueType.String, TagId = null },
            new VariableModel { Key = "Foo:1", Value = "Test2", ValueType = VariableValueType.String, TagId = null }
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json, null);

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
        var expected = new List<VariableModel>
        {
            new VariableModel { Key = "Number Of Guests", Value = "145", ValueType = VariableValueType.Number, TagId = null },
            new VariableModel { Key = "Panel Members:0:FirstName", Value = "Stephen", ValueType = VariableValueType.String, TagId = null },
            new VariableModel { Key = "Panel Members:0:LastName", Value = "Fry", ValueType = VariableValueType.String, TagId = null },
            new VariableModel { Key = "Panel Members:1:FirstName", Value = "Alan", ValueType = VariableValueType.String, TagId = null },
            new VariableModel { Key = "Panel Members:1:LastName", Value = "Davies", ValueType = VariableValueType.String, TagId = null },
            new VariableModel { Key = "Was Recorded", Value = "true", ValueType = VariableValueType.Boolean, TagId = null },
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json, null);

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
                       "1": "So they should be properties instead"
                   }
                   """";
        var expected = new List<VariableModel>
        {
            new VariableModel
                { Key = "0", Value = "Arrays can't exist at the root level", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "1", Value = "So they should be properties instead", ValueType = VariableValueType.String, TagId = null },
        };

        // Act
        var actual = _jsonDeserializer.DeserializeFromJson(json, null);

        // Assert
        Assert.Equal(expected, actual);
    }
}
