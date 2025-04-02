using System.Net;
using System.Net.Http.Headers;
using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class GetConfigEndpointTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagModel _benelux = null!;
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
        await tagGroupManager.SaveTagGroupAsync(environments, cancellationToken);
        _benelux = new TagModel { Name = "Benelux", TagGroupId = environments.Id };
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
            TagId = null
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
            TagId = _benelux.Id
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
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
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
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow.AddDays(-5),
            ActiveUntilUtc = DateTime.UtcNow.AddDays(-1),
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
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
            TagIds = new List<int> { _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow.AddDays(1),
            ActiveUntilUtc = DateTime.UtcNow.AddDays(5),
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
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
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await httpClient.SendAsync(request, cancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
