using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingVariables : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagModel _benelux = null!;
    private TagModel _nordics = null!;
    private TagModel _blade1 = null!;
    private TagModel _blade2 = null!;
    private ApplicationModel _processor = null!;
    private ApplicationModel _router = null!;
    private ApplicationModel _otherApplication = null!;
    private TagModel _otherTag = null!;
    private string _blade1Variables = null!;
    private string _blade2Variables = null!;
    private string _beneluxVariables = null!;
    private string _nordicsVariables = null!;
    private string _processorVariables = null!;
    private string _routerVariables = null!;
    private string _processorBlade1Variables = null!;
    private string _processorBlade2Variables = null!;
    private string _routerBlade1Variables = null!;
    private string _routerBlade2Variables = null!;
    private string _processorBeneluxVariables = null!;
    private string _processorNordicsVariables = null!;
    private string _routerBeneluxVariables = null!;
    private string _routerNordicsVariables = null!;

    public ManagingVariables(IntegrationTestFixture fixture, ITestOutputHelper output)
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
        _nordics = new TagModel { Name = "Nordics" };
        _blade1 = new TagModel { Name = "Blade 1" };
        _blade2 = new TagModel { Name = "Blade 2" };
        _otherTag = new TagModel { Name = "Other Tag" };
        await tagManager.SaveTagAsync(_benelux, cancellationToken);
        await tagManager.SaveTagAsync(_nordics, cancellationToken);
        await tagManager.SaveTagAsync(_blade1, cancellationToken);
        await tagManager.SaveTagAsync(_blade2, cancellationToken);
        await tagManager.SaveTagAsync(_otherTag, cancellationToken);
        _processor = new ApplicationModel { Name = "Processor" };
        _router = new ApplicationModel { Name = "Router" };
        _otherApplication = new ApplicationModel { Name = "Other" };
        await applicationManager.SaveApplicationAsync(_processor, cancellationToken);
        await applicationManager.SaveApplicationAsync(_router, cancellationToken);
        await applicationManager.SaveApplicationAsync(_otherApplication, cancellationToken);

        // Other config
        var otherVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "Some": "Other"
                   }
                   """",
            ApplicationIds = [_otherApplication.Id],
            TagId = _otherTag.Id
        };
        await variableManager.SaveAsync(otherVariablesModel, cancellationToken);

        // Machine specific config
        _blade1Variables = """"
                           {
                                "Machine": "Blade1",
                                "Common": "Common from Blade1"
                           }
                           """";
        var blade1VariablesModel = new VariablesEditModel
        {
            Json = _blade1Variables,
            ApplicationIds = [_processor.Id, _router.Id],
            TagId = _blade1.Id
        };
        _blade2Variables = """"
                           {
                                "Machine": "Blade2",
                                "Common": "Common from Blade2"
                           }
                           """";
        var blade2VariablesModel = new VariablesEditModel
        {
            Json = _blade2Variables,
            ApplicationIds = [_processor.Id, _router.Id],
            TagId = _blade2.Id
        };
        await variableManager.SaveAsync(blade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(blade2VariablesModel, cancellationToken);

        // Environment specific config
        _beneluxVariables = """"
                            {
                                 "Environment": "Benelux",
                                 "Common": "Common from Benelux"
                            }
                            """";
        var beneluxVariablesModel = new VariablesEditModel
        {
            Json = _beneluxVariables,
            ApplicationIds = [_processor.Id, _router.Id],
            TagId = _benelux.Id
        };
        _nordicsVariables = """"
                            {
                                 "Environment": "Nordics",
                                 "Common": "Common from Nordics"
                            }
                            """";
        var nordicsVariablesModel = new VariablesEditModel
        {
            Json = _nordicsVariables,
            ApplicationIds = [_processor.Id, _router.Id],
            TagId = _nordics.Id
        };
        await variableManager.SaveAsync(beneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsVariablesModel, cancellationToken);

        // Application specific config
        _processorVariables = """"
                              {
                                  "Application": "Processor",
                                  "Common": "Common from Processor"
                              }
                              """";
        var processorVariablesModel = new VariablesEditModel
        {
            Json = _processorVariables,
            ApplicationIds = [_processor.Id],
            TagId = null
        };
        _routerVariables = """"
                           {
                               "Application": "Router",
                               "Common": "Common from Router"
                           }
                           """";
        var routerVariablesModel = new VariablesEditModel
        {
            Json = _routerVariables,
            ApplicationIds = [_router.Id],
            TagId = null
        };
        await variableManager.SaveAsync(processorVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerVariablesModel, cancellationToken);

        // Application + Machine specific config
        _processorBlade1Variables = """"
                                    {
                                        "ApplicationMachine": "Processor+Blade1",
                                        "Common": "Common from Processor+Blade1"
                                    }
                                    """";
        var processorBlade1VariablesModel = new VariablesEditModel
        {
            Json = _processorBlade1Variables,
            ApplicationIds = [_processor.Id],
            TagId = _blade1.Id
        };
        _processorBlade2Variables = """"
                                    {
                                         "ApplicationMachine": "Processor+Blade2",
                                         "Common": "Common from Processor+Blade2"
                                    }
                                    """";
        var processorBlade2VariablesModel = new VariablesEditModel
        {
            Json = _processorBlade2Variables,
            ApplicationIds = [_processor.Id],
            TagId = _blade2.Id
        };
        _routerBlade1Variables = """"
                                 {
                                      "ApplicationMachine": "Router+Blade1",
                                      "Common": "Common from Router+Blade1"
                                 }
                                 """";
        var routerBlade1VariablesModel = new VariablesEditModel
        {
            Json = _routerBlade1Variables,
            ApplicationIds = [_router.Id],
            TagId = _blade1.Id
        };
        _routerBlade2Variables = """"
                                 {
                                      "ApplicationMachine": "Router+Blade2",
                                      "Common": "Common from Router+Blade2"
                                 }
                                 """";
        var routerBlade2VariablesModel = new VariablesEditModel
        {
            Json = _routerBlade2Variables,
            ApplicationIds = [_router.Id],
            TagId = _blade2.Id
        };
        await variableManager.SaveAsync(processorBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBlade2VariablesModel, cancellationToken);

        // Application + Environment specific config
        _processorBeneluxVariables = """"
                                     {
                                         "ApplicationEnvironment": "Processor+Benelux",
                                         "Common": "Common from Processor+Benelux"
                                     }
                                     """";
        var processorBeneluxVariablesModel = new VariablesEditModel
        {
            Json = _processorBeneluxVariables,
            ApplicationIds = [_processor.Id],
            TagId = _benelux.Id
        };
        _processorNordicsVariables = """"
                                     {
                                          "ApplicationEnvironment": "Processor+Nordics",
                                          "Common": "Common from Processor+Nordics"
                                     }
                                     """";
        var processorNordicsVariablesModel = new VariablesEditModel
        {
            Json = _processorNordicsVariables,
            ApplicationIds = [_processor.Id],
            TagId = _nordics.Id
        };
        _routerBeneluxVariables = """"
                                  {
                                       "ApplicationEnvironment": "Router+Benelux",
                                       "Common": "Common from Router+Benelux"
                                  }
                                  """";
        var routerBeneluxVariablesModel = new VariablesEditModel
        {
            Json = _routerBeneluxVariables,
            ApplicationIds = [_router.Id],
            TagId = _benelux.Id
        };
        _routerNordicsVariables = """"
                                  {
                                       "ApplicationEnvironment": "Router+Nordics",
                                       "Common": "Common from Router+Nordics"
                                  }
                                  """";
        var routerNordicsVariablesModel = new VariablesEditModel
        {
            Json = _routerNordicsVariables,
            ApplicationIds = [_router.Id],
            TagId = _nordics.Id
        };
        await variableManager.SaveAsync(processorBeneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorNordicsVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBeneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerNordicsVariablesModel, cancellationToken);
    }

    public Task DisposeAsync()
    {
        return _fixture.ResetAsync();
    }

    [Fact]
    public async Task GettingConfigViaApiKey()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        // Processor runs on both blades for both environments
        // Router runs on blade 1 for benelux and on blade 2 for nordics
        var processorBlade1BeneluxApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id, _blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade2BeneluxApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id, _blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade1NordicsApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _nordics.Id, _blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade2NordicsApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _nordics.Id, _blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBlade1BeneluxApiKey = new ApiKeyModel
        {
            ApplicationId = _router.Id,
            TagIds = new List<int> { _benelux.Id, _blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBlade2NordicsApiKey = new ApiKeyModel
        {
            ApplicationId = _router.Id,
            TagIds = new List<int> { _nordics.Id, _blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var otherApiKeyModel = new ApiKeyModel
        {
            ApplicationId = _otherApplication.Id,
            TagIds = new List<int> { _otherTag.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        await apiKeyManager.SaveApiKeyAsync(processorBlade1BeneluxApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(processorBlade2BeneluxApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(processorBlade1NordicsApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(processorBlade2NordicsApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(routerBlade1BeneluxApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(routerBlade2NordicsApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(otherApiKeyModel, cancellationToken);

        // Act
        var processorBlade1BeneluxConfig = await variableManager.GetMergedConfigAsync(processorBlade1BeneluxApiKey.Id, cancellationToken);
        var processorBlade2BeneluxConfig = await variableManager.GetMergedConfigAsync(processorBlade2BeneluxApiKey.Id, cancellationToken);
        var processorBlade1NordicsConfig = await variableManager.GetMergedConfigAsync(processorBlade1NordicsApiKey.Id, cancellationToken);
        var processorBlade2NordicsConfig = await variableManager.GetMergedConfigAsync(processorBlade2NordicsApiKey.Id, cancellationToken);
        var routerBlade1BeneluxConfig = await variableManager.GetMergedConfigAsync(routerBlade1BeneluxApiKey.Id, cancellationToken);
        var routerBlade2NordicsConfig = await variableManager.GetMergedConfigAsync(routerBlade2NordicsApiKey.Id, cancellationToken);

        // Assert
        var expectedProcessorBlade1BeneluxConfig =
            """
            {
                "Application": "Processor",
                "ApplicationEnvironment": "Processor+Benelux",
                "ApplicationMachine": "Processor+Blade1",
                "Common": "Common from Blade1",
                "Environment": "Benelux",
                "EnvironmentMachine": "Benelux+Blade1",
                "Machine": "Blade1"
            }
            """;
        var expectedProcessorBlade2BeneluxConfig =
            """
            {
                "Application": "Processor",
                "ApplicationEnvironment": "Processor+Benelux",
                "ApplicationMachine": "Processor+Blade2",
                "Common": "Common from Blade2",
                "Environment": "Benelux",
                "EnvironmentMachine": "Benelux+Blade2",
                "Machine": "Blade2"
            }
            """;
        var expectedProcessorBlade1NordicsConfig =
            """
            {
                "Application": "Processor",
                "ApplicationEnvironment": "Processor+Nordics",
                "ApplicationMachine": "Processor+Blade1",
                "Common": "Common from Blade1",
                "Environment": "Nordics",
                "EnvironmentMachine": "Nordics+Blade1",
                "Machine": "Blade1"
            }
            """;
        var expectedProcessorBlade2NordicsConfig =
            """
            {
                "Application": "Processor",
                "ApplicationEnvironment": "Processor+Nordics",
                "ApplicationMachine": "Processor+Blade2",
                "Common": "Common from Processor+Nordics+Blade2",
                "Environment": "Nordics",
                "EnvironmentMachine": "Nordics+Blade2",
                "Machine": "Blade2"
            }
            """;
        var expectedRouterBlade1BeneluxConfig =
            """
            {
                "Application": "Router",
                "ApplicationEnvironment": "Router+Benelux",
                "ApplicationMachine": "Router+Blade1",
                "Common": "Common from Router+Benelux+Blade1",
                "Environment": "Benelux",
                "EnvironmentMachine": "Benelux+Blade1",
                "Machine": "Blade1"
            }
            """;
        var expectedRouterBlade2NordicsConfig =
            """
            {
                "Application": "Router",
                "ApplicationEnvironment": "Router+Nordics",
                "ApplicationMachine": "Router+Blade2",
                "Common": "Common from Router+Nordics+Blade2",
                "Environment": "Nordics",
                "EnvironmentMachine": "Nordics+Blade2",
                "Machine": "Blade2"
            }
            """;
        Assert.Equal(JsonNormalizer.Normalize(expectedProcessorBlade1BeneluxConfig), JsonNormalizer.Normalize(processorBlade1BeneluxConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedProcessorBlade2BeneluxConfig), JsonNormalizer.Normalize(processorBlade2BeneluxConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedProcessorBlade1NordicsConfig), JsonNormalizer.Normalize(processorBlade1NordicsConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedProcessorBlade2NordicsConfig), JsonNormalizer.Normalize(processorBlade2NordicsConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedRouterBlade1BeneluxConfig), JsonNormalizer.Normalize(routerBlade1BeneluxConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedRouterBlade2NordicsConfig), JsonNormalizer.Normalize(routerBlade2NordicsConfig));
    }

    [Fact]
    public async Task GettingConfigViaApplicationIdsAndTagIds()
    {
        // Arrange
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        // Act
        var actualBlade1VariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id, _router.Id],
            _blade1.Id,
            cancellationToken
        );

        var actualBlade2VariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id, _router.Id],
            _blade2.Id,
            cancellationToken
        );

        var actualBeneluxVariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id, _router.Id],
            _benelux.Id,
            cancellationToken
        );

        var actualNordicsVariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id, _router.Id],
            _nordics.Id,
            cancellationToken
        );

        var actualProcessorVariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id],
            null,
            cancellationToken
        );

        var actualRouterVariablesModel = await variableManager.GetConfigAsync(
            [_router.Id],
            null,
            cancellationToken
        );

        var actualProcessorBlade1VariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id],
            _blade1.Id,
            cancellationToken
        );

        var actualProcessorBlade2VariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id],
            _blade2.Id,
            cancellationToken
        );

        var actualRouterBlade1VariablesModel = await variableManager.GetConfigAsync(
            [_router.Id],
            _blade1.Id,
            cancellationToken
        );

        var actualRouterBlade2VariablesModel = await variableManager.GetConfigAsync(
            [_router.Id],
            _blade2.Id,
            cancellationToken
        );

        var actualProcessorBeneluxVariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id],
            _benelux.Id,
            cancellationToken
        );

        var actualProcessorNordicsVariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id],
            _nordics.Id,
            cancellationToken
        );

        var actualRouterBeneluxVariablesModel = await variableManager.GetConfigAsync(
            [_router.Id],
            _benelux.Id,
            cancellationToken
        );

        var actualRouterNordicsVariablesModel = await variableManager.GetConfigAsync(
            [_router.Id],
            _nordics.Id,
            cancellationToken
        );

        // Assert
        Assert.Equal(
            JsonNormalizer.Normalize(_blade1Variables),
            JsonNormalizer.Normalize(actualBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_blade2Variables),
            JsonNormalizer.Normalize(actualBlade2VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_beneluxVariables),
            JsonNormalizer.Normalize(actualBeneluxVariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_nordicsVariables),
            JsonNormalizer.Normalize(actualNordicsVariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorVariables),
            JsonNormalizer.Normalize(actualProcessorVariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerVariables),
            JsonNormalizer.Normalize(actualRouterVariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorBlade1Variables),
            JsonNormalizer.Normalize(actualProcessorBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorBlade2Variables),
            JsonNormalizer.Normalize(actualProcessorBlade2VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerBlade1Variables),
            JsonNormalizer.Normalize(actualRouterBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerBlade2Variables),
            JsonNormalizer.Normalize(actualRouterBlade2VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorBeneluxVariables),
            JsonNormalizer.Normalize(actualProcessorBeneluxVariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorNordicsVariables),
            JsonNormalizer.Normalize(actualProcessorNordicsVariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerBeneluxVariables),
            JsonNormalizer.Normalize(actualRouterBeneluxVariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerNordicsVariables),
            JsonNormalizer.Normalize(actualRouterNordicsVariablesModel)
        );
    }

    [Fact]
    public async Task UpdatingExistingConfig()
    {
        // Arrange
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;
        var newBlade1Variables = """"
                                 {
                                      "Machine": "Blade1-Modified",
                                      "Common": "Common from Blade1-Modified"
                                 }
                                 """";

        // Ensure we are really testing something
        Assert.NotEqual(
            JsonNormalizer.Normalize(_blade1Variables),
            JsonNormalizer.Normalize(newBlade1Variables)
        );

        // Act
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = newBlade1Variables,
            ApplicationIds = [_processor.Id, _router.Id],
            TagId = _blade1.Id
        }, cancellationToken);

        var actualBlade1VariablesModel = await variableManager.GetConfigAsync(
            [_processor.Id, _router.Id],
            _blade1.Id,
            cancellationToken
        );

        // Assert
        Assert.Equal(
            JsonNormalizer.Normalize(newBlade1Variables),
            JsonNormalizer.Normalize(actualBlade1VariablesModel)
        );
    }
}
