using Microsoft.Extensions.Configuration.Json;

namespace Configo.Client.Configuration;

internal sealed class ConfigoJsonStreamConfigurationProvider : JsonStreamConfigurationProvider
{
    public ConfigoJsonStreamConfigurationProvider(JsonStreamConfigurationSource source) : base(source)
    {
        
    }

    public IDictionary<string, string?> Parsed => base.Data;
}
