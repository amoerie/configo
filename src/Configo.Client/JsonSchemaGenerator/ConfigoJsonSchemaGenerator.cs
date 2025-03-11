using Namotion.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonSchema.Generation;
using NJsonSchema.NewtonsoftJson.Generation;

namespace Configo.Client.JsonSchemaGenerator;

internal sealed class ConfigoJsonSchemaGenerator
{
    private readonly ILogger<ConfigoJsonSchemaGenerator> _logger;
    private readonly IOptions<ConfigoJsonSchemaGeneratorOptions> _options;
    private readonly DefaultJsonSchemaProvider _defaultJsonSchemaProvider;
    private readonly BoundConfigurationSectionsDiscoverer _boundConfigurationSectionsDiscoverer;
    private readonly ConnectionStringsDiscoverer _connectionStringsDiscoverer;

    private readonly JsonSchemaGeneratorSettings _jsonSchemaGeneratorSettings = new NewtonsoftJsonSchemaGeneratorSettings
    {
        DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
        DefaultDictionaryValueReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
        UseXmlDocumentation = true,
        XmlDocumentationFormatting = XmlDocsFormattingMode.None,
        SerializerSettings = new JsonSerializerSettings
        {
            Converters =
            {
                new StringEnumConverter()
            },
            DefaultValueHandling = DefaultValueHandling.Include,
            Formatting = Formatting.Indented,
            ContractResolver = new ConfigoJsonContractResolver()
        },
        // NJSonSchema does not support serializing schemas with System.Text.Json (yet)
        // See https://github.com/RicoSuter/NSwag/issues/2243 
    };

    public ConfigoJsonSchemaGenerator(
        ILogger<ConfigoJsonSchemaGenerator> logger,
        IOptions<ConfigoJsonSchemaGeneratorOptions> options,
        DefaultJsonSchemaProvider defaultJsonSchemaProvider,
        BoundConfigurationSectionsDiscoverer boundConfigurationSectionsDiscoverer,
        ConnectionStringsDiscoverer connectionStringsDiscoverer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _defaultJsonSchemaProvider = defaultJsonSchemaProvider ?? throw new ArgumentNullException(nameof(defaultJsonSchemaProvider));
        _boundConfigurationSectionsDiscoverer = boundConfigurationSectionsDiscoverer ?? throw new ArgumentNullException(nameof(boundConfigurationSectionsDiscoverer));
        _connectionStringsDiscoverer = connectionStringsDiscoverer ?? throw new ArgumentNullException(nameof(connectionStringsDiscoverer));
    }

    public async Task<JsonSchema> GenerateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = _options.Value;

        _logger.LogDebug("Generating JSON schema");

        var connectionStrings = await _connectionStringsDiscoverer.DiscoverAsync(cancellationToken);
        var boundConfigurationSections = new Dictionary<string, Type>();

        if (options.AutoDiscoverBoundConfigurationSections)
        {
            _logger.LogDebug("Discovering bound configuration sections");

            var discoveredBoundConfigurationSections = await _boundConfigurationSectionsDiscoverer.DiscoverAsync(cancellationToken);

            foreach (var entry in discoveredBoundConfigurationSections)
            {
                boundConfigurationSections.Add(entry.Key, entry.Value);
            }
        }

        if (options.AdditionalBoundConfigurationSections != null)
        {
            _logger.LogDebug("Applying additional bound configuration sections");

            foreach (var entry in options.AdditionalBoundConfigurationSections)
            {
                boundConfigurationSections[entry.Key] = entry.Value;
            }
        }
        
        var defaultJsonSchema = await _defaultJsonSchemaProvider.ProvideAsync(cancellationToken);
        
        var schema = new JsonSchema();
        var resolver = new JsonSchemaResolver(schema, _jsonSchemaGeneratorSettings);
        var generator = new NJsonSchema.Generation.JsonSchemaGenerator(_jsonSchemaGeneratorSettings);
            
        schema.SchemaVersion = defaultJsonSchema.SchemaVersion;
        schema.Title = defaultJsonSchema.Title;
        schema.Type = defaultJsonSchema.Type;

        // Definitions
        var connectionStringsDefinition = defaultJsonSchema.Definitions["connectionStrings"]!;
        foreach (var connectionString in connectionStrings)
        {
            connectionStringsDefinition.Properties.Add(connectionString, new JsonSchemaProperty
            {
                Type = JsonObjectType.String,
                Description = $"A valid connection string to the {connectionString} database. See https://www.connectionstrings.com on how to make one for your database"
            });
        }
        
        foreach (var definition in defaultJsonSchema.Definitions)
        {
            schema.Definitions.Add(definition.Key, definition.Value);
        }

        // Properties
        foreach (var property in defaultJsonSchema.Properties)
        {
            schema.Properties[property.Key] = property.Value;
        }

        foreach (var boundConfigurationSection in boundConfigurationSections)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var configurationSection = boundConfigurationSection.Key;
            var configurationSectionSchema = generator.Generate<JsonSchemaProperty>(boundConfigurationSection.Value, resolver);
            schema.Properties[configurationSection] = configurationSectionSchema;
        }
        
        // Hardcoded configuration section: Configo
        var configoOptionsSchema = generator.Generate<JsonSchemaProperty>(typeof(ConfigoOptions), resolver);
        schema.Properties["Configo"] = configoOptionsSchema;
        
        _logger.LogDebug("Generated JSON schema successfully");

        return schema;
    }
}
