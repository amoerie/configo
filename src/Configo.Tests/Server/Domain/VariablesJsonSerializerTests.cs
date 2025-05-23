﻿using Configo.Database.Tables;
using Configo.Server.Domain;

namespace Configo.Tests.Server.Domain;

public class VariablesJsonSerializerTests
{
    private readonly VariablesJsonSerializer _jsonSerializer = new VariablesJsonSerializer();

    [Fact]
    public void SimpleKeyValue()
    {
        // Arrange
        var variables = new List<VariableModel>
        {
            new VariableModel { Key = "Foo", Value = "Bar", ValueType = VariableValueType.String, TagId = null }
        };
        var expected = """"
                       {
                           "Foo": "Bar"
                       }
                       """";

        // Act
        var actual = _jsonSerializer.SerializeToJson(variables);

        // Assert
        Assert.Equal(JsonNormalizer.Normalize(expected), JsonNormalizer.Normalize(actual));
    }

    [Fact]
    public void SimpleObject()
    {
        // Arrange
        var variables = new List<VariableModel>
        {
            new VariableModel { Key = "Foo:Bar", Value = "Test", ValueType = VariableValueType.String, TagId = null }
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
        Assert.Equal(JsonNormalizer.Normalize(expected), JsonNormalizer.Normalize(actual));
    }

    [Fact]
    public void Array()
    {
        // Arrange
        var variables = new List<VariableModel>
        {
            new VariableModel { Key = "Foo:0", Value = "Test1", ValueType = VariableValueType.String, TagId = null },
            new VariableModel { Key = "Foo:1", Value = "Test2", ValueType = VariableValueType.String, TagId = null }
        };
        var expected = """"
                       {
                           "Foo": [ "Test1", "Test2" ]
                       }
                       """";

        // Act
        var actual = _jsonSerializer.SerializeToJson(variables);

        // Assert
        Assert.Equal(JsonNormalizer.Normalize(expected), JsonNormalizer.Normalize(actual));
    }

    [Fact]
    public void ComplexObject()
    {
        // Arrange
        var variables = new List<VariableModel>
        {
            new VariableModel
                { Key = "Panel Members:0:FirstName", Value = "Stephen", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "Panel Members:0:LastName", Value = "Fry", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "Panel Members:1:FirstName", Value = "Alan", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "Panel Members:1:LastName", Value = "Davies", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "Number Of Guests", Value = "145", ValueType = VariableValueType.Number, TagId = null },
            new VariableModel { Key = "Was Recorded", Value = "true", ValueType = VariableValueType.Boolean, TagId = null },
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
        Assert.Equal(JsonNormalizer.Normalize(expected), JsonNormalizer.Normalize(actual));
    }

    [Fact]
    public void EdgeCases()
    {
        // Arrange
        var variables = new List<VariableModel>
        {
            new VariableModel
                { Key = "0", Value = "Arrays can't exist at the root level", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "1", Value = "So they should be properties instead", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "DeletedVariables:1", Value = "Sometimes variables", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "DeletedVariables:3", Value = "Get deleted", ValueType = VariableValueType.String, TagId = null },
            new VariableModel
                { Key = "DeletedVariables:5", Value = "Which causes gaps", ValueType = VariableValueType.String, TagId = null },
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
        Assert.Equal(JsonNormalizer.Normalize(expected), JsonNormalizer.Normalize(actual));
    }
}