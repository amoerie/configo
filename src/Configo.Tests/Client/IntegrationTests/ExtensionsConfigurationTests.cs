using Configo.Client;
using Configo.Client.Configuration;
using Configo.Server.Blazor;
using Configo.Server.Domain;
using Microsoft.Extensions.Configuration;
using MudBlazor;
using Xunit.Abstractions;

namespace Configo.Tests.Client.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ExtensionsConfigurationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagGroupModel _environments = default!;
    private TagModel _benelux = default!;
    private ApplicationModel _processor = default!;
    private string _beneluxVariables = default!;
    private string _processorVariables = default!;
    private string _processorBeneluxVariables = default!;

    public ExtensionsConfigurationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _fixture.SetOutput(output);
    }

    public async Task InitializeAsync()
    {
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        var tagManager = _fixture.GetRequiredService<TagManager>();
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        _environments = new TagGroupModel { Name = "Environments", Icon = TagGroupIcon.GetByName(Icons.Material.Filled.Map)};
        await tagGroupManager.SaveTagGroupAsync(_environments, cancellationToken);
        _benelux = new TagModel { Name = "Benelux", GroupId = _environments.Id, GroupIcon = _environments.Icon };
        await tagManager.SaveTagAsync(_benelux, cancellationToken);
        var processorModel = new ApplicationModel { Name = "Processor" };
        _processor = await applicationManager.SaveApplicationAsync(processorModel, cancellationToken);

        // Environment specific config
        _beneluxVariables = """{ "Environment": { "Name": "Benelux" } }""";
        var beneluxVariablesModel = new VariablesEditModel
        {
            Json = _beneluxVariables,
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _benelux.Id }
        };
        await variableManager.SaveAsync(beneluxVariablesModel, cancellationToken);

        // Application specific config
        _processorVariables = """{ "Application": { "Name": "Processor" } }""";
        var processorVariablesModel = new VariablesEditModel
        {
            Json = _processorVariables,
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int>()
        };
        await variableManager.SaveAsync(processorVariablesModel, cancellationToken);

        // Application + Environment specific config
        _processorBeneluxVariables = """{ "ApplicationEnvironment": { "Name": "Processor+Benelux" } }""";
        var processorBeneluxVariablesModel = new VariablesEditModel
        {
            Json = _processorBeneluxVariables,
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _benelux.Id }
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
        var cancellationToken = default(CancellationToken);
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();
        
        // Processor runs in benelux
        var apiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var apiKey = await apiKeyManager.SaveApiKeyAsync(apiKeyModel, cancellationToken);

        // Act
        var configuration = new ConfigurationBuilder()
            .AddConfigo(httpClient, new ConfigoOptions { Url = null, ApiKey = apiKey.Key })
            .Build();
        
        // Assert
        configuration.GetChildren().Count().Should().Be(3);
        configuration["Environment:Name"].Should().Be("Benelux");
        configuration["Application:Name"].Should().Be("Processor");
        configuration["ApplicationEnvironment:Name"].Should().Be("Processor+Benelux");
    }

    [Fact]
    public async Task ValidApiKeyWithCacheFileName()
    {
        // Arrange
        var cacheFileName = Path.Combine(Path.GetTempPath(), $"appsettings.configo.{Guid.NewGuid()}.json");
        var cancellationToken = default(CancellationToken);
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();
        
        // Processor runs in benelux
        var apiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var apiKey = await apiKeyManager.SaveApiKeyAsync(apiKeyModel, cancellationToken);

        try
        {
            // Act
            var configuration = new ConfigurationBuilder()
                .AddConfigo(httpClient, new ConfigoOptions { Url = null, ApiKey = apiKey.Key, CacheFileName = cacheFileName })
                .Build();

            // Assert
            configuration.GetChildren().Count().Should().Be(3);
            configuration["Environment:Name"].Should().Be("Benelux");
            configuration["Application:Name"].Should().Be("Processor");
            configuration["ApplicationEnvironment:Name"].Should().Be("Processor+Benelux");

            configuration = new ConfigurationBuilder()
                .AddJsonFile(cacheFileName, optional: false, reloadOnChange: false)
                .Build();
            
            configuration.GetChildren().Count().Should().Be(3);
            configuration["Environment:Name"].Should().Be("Benelux");
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
        var cancellationToken = default(CancellationToken);
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        using var httpClient = _fixture.CreateClient();
        
        // Processor runs in benelux
        var apiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var apiKey = await apiKeyManager.SaveApiKeyAsync(apiKeyModel, cancellationToken);

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
        _beneluxVariables = """{ "Environment": { "Name": "Benelux Updated" } }""";
        var beneluxVariablesModel = new VariablesEditModel
        {
            Json = _beneluxVariables,
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _benelux.Id }
        };
        await variableManager.SaveAsync(beneluxVariablesModel, cancellationToken);
        
        // Allow some time for reload interval to trigger
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        // Assert
        configuration.GetChildren().Count().Should().Be(3);
        configuration["Environment:Name"].Should().Be("Benelux Updated");
        configuration["Application:Name"].Should().Be("Processor");
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
