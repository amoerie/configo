using System.Net;
using System.Net.Http.Headers;
using Configo.Database;
using Configo.Server.Caching;
using Configo.Server.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class GetConfigEndpointTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagFormModel _global = null!;
    private TagFormModel _benelux = null!;
    private ApplicationModel _processor = null!;
    private string _globalVariables = null!;
    private string _beneluxVariables = null!;

    public GetConfigEndpointTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _fixture.SetOutput(output);
    }

    public async Task InitializeAsync()
    {
        var tagManager = _fixture.GetRequiredService<TagManager>();
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        var environments = new TagGroupModel { Name = "Environments" };
        var globalGroup = new TagGroupModel { Name = "Global" };
        await tagGroupManager.SaveTagGroupAsync(environments, cancellationToken);
        await tagGroupManager.SaveTagGroupAsync(globalGroup, cancellationToken);
        _global = new TagFormModel { Name = "Global", TagGroupId = globalGroup.Id };
        _benelux = new TagFormModel { Name = "Benelux", TagGroupId = environments.Id };
        await tagManager.SaveTagAsync(_global, cancellationToken);
        await tagManager.SaveTagAsync(_benelux, cancellationToken);
        _processor = new ApplicationModel { Name = "Processor" };
        await applicationManager.SaveApplicationAsync(_processor, cancellationToken);

        _globalVariables = """"
                           {
                               "Company": "Lexisoft",
                           }
                           """";
        var globalVariables = new VariablesEditModel
        {
            Json = _globalVariables,
            TagId = _global.Id,
            ApplicationId = _processor.Id
        };
        await variableManager.SaveAsync(globalVariables, cancellationToken);

        _beneluxVariables = """"
                            {
                                "Environment": "Benelux",
                            }
                            """";
        var beneluxVariables = new VariablesEditModel
        {
            Json = _beneluxVariables,
            TagId = _benelux.Id,
            ApplicationId = _processor.Id
        };
        await variableManager.SaveAsync(beneluxVariables, cancellationToken);
    }

    public Task DisposeAsync()
    {
        return _fixture.ResetAsync();
    }

    [Fact]
    public async Task ValidApiKey()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = CancellationToken.None;

        // Processor runs in benelux
        var apiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _global.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Key);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var actualConfig = await response.Content.ReadAsStringAsync(cancellationToken);

        // Assert
        var expectedConfig =
            """
            {
                "Company": "Lexisoft",
                "Environment": "Benelux"
            }
            """;
        Assert.Equal(JsonNormalizer.Normalize(expectedConfig), JsonNormalizer.Normalize(actualConfig));
    }

    [Fact]
    public async Task ExpiredApiKey()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = CancellationToken.None;

        // Processor runs in benelux
        var apiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _global.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow.AddDays(-5),
            ActiveUntilUtc = DateTime.UtcNow.AddDays(-1),
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Key);

        var response = await httpClient.SendAsync(request, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InactiveApiKey()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = CancellationToken.None;

        // Processor runs in benelux
        var apiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _global.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow.AddDays(1),
            ActiveUntilUtc = DateTime.UtcNow.AddDays(5),
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Key);

        var response = await httpClient.SendAsync(request, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownApiKey()
    {
        // Arrange
        var apiKeyGenerator = _fixture.GetRequiredService<ApiKeyGenerator>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = CancellationToken.None;

        // Processor runs in benelux
        var apiKey = apiKeyGenerator.Generate(64);

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(request, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Caching()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        var cacheManager = _fixture.GetRequiredService<CacheManager>();
        var dbContextFactory = _fixture.GetRequiredService<IDbContextFactory<ConfigoDbContext>>();
        var cancellationToken = CancellationToken.None;

        // Processor runs in benelux
        var apiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _global.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        var config1 = await GetConfig();

        // Now clear the database without expiring the cache
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await dbContext.Variables.ExecuteDeleteAsync(cancellationToken);
        }

        // Now get the config again, it should have remained the same value
        var config2 = await GetConfig();
        
        // Now expire the cache entry
        await cacheManager.ExpireConfigByApiKeyIdAsync(apiKey.Id, cancellationToken);
        var config3 = await GetConfig();

        // Assert
        var expectedConfig1 =
            """
            {
                "Company": "Lexisoft",
                "Environment": "Benelux"
            }
            """;
        var expectedConfig2 =
            """
            {
                "Company": "Lexisoft",
                "Environment": "Benelux"
            }
            """;
        var expectedConfig3 =
            """
            {
            }
            """;
        Assert.Equal(JsonNormalizer.Normalize(expectedConfig1), JsonNormalizer.Normalize(config1));
        Assert.Equal(JsonNormalizer.Normalize(expectedConfig2), JsonNormalizer.Normalize(config2));
        Assert.Equal(JsonNormalizer.Normalize(expectedConfig3), JsonNormalizer.Normalize(config3));

        async Task<string> GetConfig()
        {
            using var httpClient = _fixture.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Key);

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }
}
