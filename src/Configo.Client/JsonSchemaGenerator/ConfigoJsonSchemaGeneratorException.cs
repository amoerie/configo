using System.Runtime.Serialization;

namespace Configo.Client.JsonSchemaGenerator;

/// <summary>
/// The exception that occurs when something went wrong while trying to generate a JSON schema
/// </summary>
[Serializable]
public sealed class ConfigoJsonSchemaGeneratorException : Exception
{
    /// <inheritdoc />
    [Obsolete]
    public ConfigoJsonSchemaGeneratorException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    
    /// <inheritdoc />
    public ConfigoJsonSchemaGeneratorException(string? message) : base(message) { }
    
    /// <inheritdoc />
    public ConfigoJsonSchemaGeneratorException(string? message, Exception? innerException) : base(message, innerException) { }
}
