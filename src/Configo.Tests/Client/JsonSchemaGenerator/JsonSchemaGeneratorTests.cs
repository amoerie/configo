using Configo.Client.JsonSchemaGenerator;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Configo.Tests.Client.JsonSchemaGenerator;

public class JsonSchemaGeneratorTests
{
    private readonly IntegrationTestOutputAccessor _integrationTestOutputAccessor;

    public JsonSchemaGeneratorTests(ITestOutputHelper output)
    {
        _integrationTestOutputAccessor = new IntegrationTestOutputAccessor
        {
            Output = output
        };
    }

    [Fact]
    public async Task ShouldProduceValidSchema()
    {
        // Arrange
        var cancellationToken = default(CancellationToken);

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:Configo", "Server=.;Database=Configo;Integrated Security=True;Encrypt=False" }
            })
            .Build();
        services.AddSingleton(configuration);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new IntegrationTestLoggerProvider(_integrationTestOutputAccessor));
            logging.SetMinimumLevel(LogLevel.Trace);
        });
        services.AddConfigoJsonSchemaGenerator(o =>
        {
            o.OnlyInEnvironment = null;
            o.AutoDiscoverBoundConfigurationSections = true;
        });
        services.AddOptions<SeqOptions>().BindConfiguration(SeqOptions.ConfigurationSectionName);
        services.AddOptions<RabbitMqOptions>().BindConfiguration(RabbitMqOptions.ConfigurationSectionName);
        services.AddOptions<OtherOptions>().BindConfiguration(OtherOptions.ConfigurationSectionName);

        var serviceProvider = services.BuildServiceProvider();
        
        var generator = serviceProvider.GetRequiredService<ConfigoJsonSchemaGenerator>();

        // Act
        var schema = await generator.GenerateAsync(cancellationToken);

        // Assert
        var actual = schema.ToJson(Formatting.Indented);
        var expected = await File.ReadAllTextAsync("./Client/JsonSchemaGenerator/JsonSchemaGeneratorTests.schema.json", cancellationToken);
        JsonNormalizer.Normalize(actual)
            .Should().Be(JsonNormalizer.Normalize(expected));
    }

    /// <summary>
    /// Configures Seq logging
    /// </summary>
    private class SeqOptions
    {
        public const string ConfigurationSectionName = "Seq";

        /// <summary>
        /// The URL to Seq
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// The API key to Seq
        /// </summary>
        public string? ApiKey { get; set; }
    }

    /// <summary>
    /// Configures Rabbit MQ connectivity
    /// </summary>
    public sealed record RabbitMqOptions
    {
        internal const string ConfigurationSectionName = "RabbitMq";

        /// <summary>
        /// The RabbitMq servers
        /// Specify more than one server to support one node in the cluster going offline
        /// </summary>
        public RabbitMqServerConfig[]? Servers { get; set; }

        /// <summary>
        /// The virtual host
        /// </summary>
        public string? VHost { get; set; }

        /// <summary>
        /// The username to authenticate with
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// The password to authenticate with
        /// </summary>
        public string? Password { get; set; }
    }

    /// <summary>
    /// Configures a specific Rabbit MQ server
    /// </summary>
    public sealed record RabbitMqServerConfig
    {
        /// <summary>
        /// The host name of the machine running Rabbit MQ
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// The port where Rabbit MQ is reachable
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// The management port where Rabbit MQ can be dynamically managed
        /// </summary>
        public int? ManagementPort { get; set; }
    }

    public sealed record OtherOptions
    {
        internal const string ConfigurationSectionName = "Other";

        /// <summary>
        /// A date time
        /// </summary>
        public DateTime? DateTime { get; set; }
        
        /// <summary>
        /// A time span
        /// </summary>
        public TimeSpan? TimeSpan { get; set; }
        
        /// <summary>
        /// A date only
        /// </summary>
        public DateOnly? DateOnly { get; set; }
        
        /// <summary>
        /// A time only
        /// </summary>
        public TimeOnly? TimeOnly { get; set; }
        
        /// <summary>
        /// Some strings in an IEnumerable
        /// </summary>
        public IEnumerable<string>? StringsEnumerable { get; set; }
        
        /// <summary>
        /// Some strings in an array
        /// </summary>
        public string[]? StringsArray { get; set; }
        
        /// <summary>
        /// Some strings in an IList
        /// </summary>
        public IList<string>? StringsIList { get; set; }
        
        /// <summary>
        /// Some strings in an IList
        /// </summary>
        public List<string>? StringsList { get; set; }
        
        /// <summary>
        /// Some strings in a Dictionary
        /// </summary>
        public Dictionary<string, string>? StringsDictionary { get; set; }
        
        /// <summary>
        /// Some strings in an IDictionary
        /// </summary>
        public IDictionary<string, string>? StringsIDictionary { get; set; }
    }
}
