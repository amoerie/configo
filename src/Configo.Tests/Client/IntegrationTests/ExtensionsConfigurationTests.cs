using Configo.Client;
using Configo.Client.Configuration;
using Configo.Server.Domain;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace Configo.Tests.Client.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ExtensionsConfigurationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagModel _benelux = null!;
    private ApplicationModel _processor = null!;
    private string _processorVariables = null!;
    private string _processorBeneluxVariables = null!;

    public ExtensionsConfigurationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _fixture.SetOutput(output);
    }

    public async Task InitializeAsync()
    {
        var tagManager = _fixture.GetRequiredService<TagManager>();
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        _benelux = new TagModel { Name = "Benelux" };
        await tagManager.SaveTagAsync(_benelux, cancellationToken);
        _processor = new ApplicationModel { Name = "Processor" };
        await applicationManager.SaveApplicationAsync(_processor, cancellationToken);

        // Application specific config without tag
        _processorVariables = """{ "Application": { "Name": "Processor" } }""";
        var processorVariablesModel = new VariablesEditModel
        {
            Json = _processorVariables,
            ApplicationIds = [_processor.Id],
            TagId = null
        };
        await variableManager.SaveAsync(processorVariablesModel, cancellationToken);

        // Application + Environment specific config
        _processorBeneluxVariables = """{ "ApplicationEnvironment": { "Name": "Processor+Benelux" } }""";
        var processorBeneluxVariablesModel = new VariablesEditModel
        {
            Json = _processorBeneluxVariables,
            ApplicationIds = [_processor.Id],
            TagId = _benelux.Id
        };
        await variableManager.SaveAsync(processorBeneluxVariablesModel, cancellationToken);
    }

    public Task DisposeAsync()
    {
        return _fixture.ResetAsync();
    }

    [Fact]
    public async Task ValidApiKey()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();

        // Processor runs in benelux
        var apiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
            Key = "",
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        var configuration = new ConfigurationBuilder()
            .AddConfigo(httpClient, new ConfigoOptions { Url = null, ApiKey = apiKey.Key })
            .Build();

        // Assert
        configuration.GetChildren().Count().Should().Be(2);
        configuration["Application:Name"].Should().Be("Processor");
        configuration["ApplicationEnvironment:Name"].Should().Be("Processor+Benelux");
    }

    [Fact]
    public async Task ValidApiKeyWithCacheFileName()
    {
        // Arrange
        var cacheFileName = Path.Combine(Path.GetTempPath(), $"appsettings.configo.{Guid.NewGuid()}.json");
        var cancellationToken = CancellationToken.None;
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();

        // Processor runs in benelux
        var apiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
            Key = "",
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        try
        {
            // Act
            var configuration = new ConfigurationBuilder()
                .AddConfigo(httpClient, new ConfigoOptions { Url = null, ApiKey = apiKey.Key, CacheFileName = cacheFileName })
                .Build();

            // Assert
            configuration.GetChildren().Count().Should().Be(2);
            configuration["Application:Name"].Should().Be("Processor");
            configuration["ApplicationEnvironment:Name"].Should().Be("Processor+Benelux");

            configuration = new ConfigurationBuilder()
                .AddJsonFile(cacheFileName, optional: false, reloadOnChange: false)
                .Build();

            configuration.GetChildren().Count().Should().Be(2);
            configuration["Application:Name"].Should().Be("Processor");
            configuration["ApplicationEnvironment:Name"].Should().Be("Processor+Benelux");
        }
        finally
        {
            try
            {
                File.Delete(cacheFileName);
            }
            catch (IOException)
            {
                // Ignored
            }
        }
    }

    [Fact]
    public async Task ValidApiKeyWithReload()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        using var httpClient = _fixture.CreateClient();

        // Processor runs in benelux
        var apiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
            Key = ""
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        var configuration = new ConfigurationBuilder()
            .AddConfigo(httpClient, new ConfigoOptions
            {
                Url = null,
                ApiKey = apiKey.Key,
                ReloadInterval = TimeSpan.FromSeconds(1),
            })
            .Build();

        // Update config
        _processorVariables = """{ "Application": { "Name": "Processor Updated" } }""";
        var processorVariablesModel = new VariablesEditModel
        {
            Json = _processorVariables,
            ApplicationIds = [_processor.Id],
            TagId = null
        };
        await variableManager.SaveAsync(processorVariablesModel, cancellationToken);

        // Allow some time for reload interval to trigger
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        // Assert
        configuration.GetChildren().Count().Should().Be(2);
        configuration["Application:Name"].Should().Be("Processor Updated");
        configuration["ApplicationEnvironment:Name"].Should().Be("Processor+Benelux");
    }

    [Fact]
    public void InvalidApiKey()
    {
        // Arrange
        var apiKeyGenerator = _fixture.GetRequiredService<ApiKeyGenerator>();
        using var httpClient = _fixture.CreateClient();
        var apiKey = apiKeyGenerator.Generate(64);

        // Act
        var configurationBuilder = new ConfigurationBuilder()
            .AddConfigo(httpClient, new ConfigoOptions
            {
                Url = null,
                ApiKey = apiKey,
                ReloadInterval = null,
                CacheFileName = null
            });

        // Assert
        configurationBuilder.Invoking(b => b.Build()).Should().Throw<ConfigoConfigurationException>();
    }

    [Fact]
    public void InvalidUrl()
    {
        // Arrange
        var apiKeyGenerator = _fixture.GetRequiredService<ApiKeyGenerator>();
        var unknownHost = Guid.NewGuid().ToString().Replace("-", "");
        var apiKey = apiKeyGenerator.Generate(64);

        // Act
        var configurationBuilder = new ConfigurationBuilder()
            .AddConfigo(new ConfigoOptions
            {
                Url = $"https://{unknownHost}",
                ApiKey = apiKey,
                ReloadInterval = null,
                CacheFileName = null
            });

        // Assert
        configurationBuilder.Invoking(b => b.Build()).Should().Throw<ConfigoConfigurationException>();
    }
}
