﻿using System.Net;
using System.Net.Http.Headers;
using Configo.Server.Blazor;
using Configo.Server.Domain;
using MudBlazor;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class GetConfigEndpointTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagGroupModel _environments = default!;
    private TagModel _benelux = default!;
    private ApplicationModel _processor = default!;
    private string _beneluxVariables = default!;
    private string _processorVariables = default!;
    private string _processorBeneluxVariables = default!;

    public GetConfigEndpointTests(IntegrationTestFixture fixture, ITestOutputHelper output)
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
        _processor = new ApplicationModel { Name = "Processor" };
        await applicationManager.SaveApplicationAsync(_processor, cancellationToken);

        // Environment specific config
        _beneluxVariables = """"
                           {
                                "Environment": "Benelux",
                           }
                           """";
        var beneluxVariablesModel = new VariablesEditModel
        {
            Json = _beneluxVariables,
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _benelux.Id }
        };
        await variableManager.SaveAsync(beneluxVariablesModel, cancellationToken);

        // Application specific config
        _processorVariables = """"
                              {
                                  "Application": "Processor",
                              }
                              """";
        var processorVariablesModel = new VariablesEditModel
        {
            Json = _processorVariables,
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int>()
        };
        await variableManager.SaveAsync(processorVariablesModel, cancellationToken);

        // Application + Environment specific config
        _processorBeneluxVariables = """"
                                     {
                                         "ApplicationEnvironment": "Processor+Benelux",
                                     }
                                     """";
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
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = default(CancellationToken);
        
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
                "Application": "Processor",
                "ApplicationEnvironment": "Processor+Benelux",
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
        var cancellationToken = default(CancellationToken);
        
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task InactiveApiKey()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = default(CancellationToken);
        
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
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task UnknownApiKey()
    {
        // Arrange
        var apiKeyGenerator = _fixture.GetRequiredService<ApiKeyGenerator>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = default(CancellationToken);
        
        // Processor runs in benelux
        var apiKey = apiKeyGenerator.Generate(64);

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/config");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        
        var response = await httpClient.SendAsync(request, cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
