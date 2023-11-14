using System.Runtime.Serialization;

namespace Configo.Client.Configuration;

/// <summary>
/// The exception that occurs when something failed in the Configo configuration source
/// </summary>
[Serializable]
public sealed class ConfigoConfigurationException : Exception
{
    /// <inheritdoc />
    public ConfigoConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    
    /// <inheritdoc />
    public ConfigoConfigurationException(string? message) : base(message) { }
    
    /// <inheritdoc />
    public ConfigoConfigurationException(string? message, Exception? innerException) : base(message, innerException) { }
}
