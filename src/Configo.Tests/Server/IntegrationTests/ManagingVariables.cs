using Configo.Server.Blazor;
using Configo.Server.Domain;
using MudBlazor;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingVariables : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagGroupModel _environments = default!;
    private TagGroupModel _machines = default!;
    private TagModel _benelux = default!;
    private TagModel _nordics = default!;
    private TagModel _blade1 = default!;
    private TagModel _blade2 = default!;
    private ApplicationModel _processor = default!;
    private ApplicationModel _router = default!;
    private ApplicationModel _otherApplication = default!;
    private TagModel _otherTag = default!;
    private string _blade1Variables = default!;
    private string _blade2Variables = default!;
    private string _beneluxVariables = default!;
    private string _nordicsVariables = default!;
    private string _beneluxBlade1Variables = default!;
    private string _beneluxBlade2Variables = default!;
    private string _nordicsBlade1Variables = default!;
    private string _nordicsBlade2Variables = default!;
    private string _processorVariables = default!;
    private string _routerVariables = default!;
    private string _processorBlade1Variables = default!;
    private string _processorBlade2Variables = default!;
    private string _routerBlade1Variables = default!;
    private string _routerBlade2Variables = default!;
    private string _processorBeneluxVariables = default!;
    private string _processorNordicsVariables = default!;
    private string _routerBeneluxVariables = default!;
    private string _routerNordicsVariables = default!;
    private string _processorBeneluxBlade1Variables = default!;
    private string _processorBeneluxBlade2Variables = default!;
    private string _processorNordicsBlade1Variables = default!;
    private string _processorNordicsBlade2Variables = default!;
    private string _routerBeneluxBlade1Variables = default!;
    private string _routerBeneluxBlade2Variables = default!;
    private string _routerNordicsBlade1Variables = default!;
    private string _routerNordicsBlade2Variables = default!;

    public ManagingVariables(IntegrationTestFixture fixture, ITestOutputHelper output)
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

        _environments = new TagGroupModel { Name = "Environments", Icon = TagGroupIcon.GetByName(Icons.Material.Filled.Factory)};
        _machines = new TagGroupModel { Name = "Machines", Icon = TagGroupIcon.GetByName(Icons.Material.Filled.Computer)};
        var otherTagGroup = new TagGroupModel { Name = "Other", Icon = TagGroupIcon.GetByName(Icons.Material.Filled.QuestionMark) };
        await tagGroupManager.SaveTagGroupAsync(_environments, cancellationToken);
        await tagGroupManager.SaveTagGroupAsync(_machines, cancellationToken);
        await tagGroupManager.SaveTagGroupAsync(otherTagGroup, cancellationToken);
        _benelux = new TagModel { Name = "Benelux", GroupId = _environments.Id, GroupIcon = _environments.Icon };
        _nordics = new TagModel { Name = "Nordics", GroupId = _environments.Id, GroupIcon = _environments.Icon };
        _blade1 = new TagModel { Name = "Blade 1", GroupId = _machines.Id, GroupIcon = _machines.Icon };
        _blade2 = new TagModel { Name = "Blade 2", GroupId = _machines.Id, GroupIcon = _machines.Icon };
        _otherTag = new TagModel { Name = "Other Tag", GroupId = otherTagGroup.Id, GroupIcon = otherTagGroup.Icon };
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
            ApplicationIds = new List<int> { _otherApplication.Id },
            TagIds = new List<int> { _otherTag.Id }
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
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _blade1.Id }
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
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _blade2.Id }
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
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _benelux.Id }
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
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _nordics.Id }
        };
        await variableManager.SaveAsync(beneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsVariablesModel, cancellationToken);

        // Environment + Machine specific config
        _beneluxBlade1Variables = """"
                                  {
                                       "EnvironmentMachine": "Benelux+Blade1",
                                       "Common": "Common from Benelux+Blade1"
                                  }
                                  """";
        var beneluxBlade1VariablesModel = new VariablesEditModel
        {
            Json = _beneluxBlade1Variables,
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _benelux.Id, _blade1.Id }
        };
        _beneluxBlade2Variables = """"
                                  {
                                       "EnvironmentMachine": "Benelux+Blade2",
                                       "Common": "Common from Benelux+Blade2"
                                  }
                                  """";
        var beneluxBlade2VariablesModel = new VariablesEditModel
        {
            Json = _beneluxBlade2Variables,
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _benelux.Id, _blade2.Id }
        };
        _nordicsBlade1Variables = """"
                                  {
                                       "EnvironmentMachine": "Nordics+Blade1",
                                       "Common": "Common from Nordics+Blade1"
                                  }
                                  """";
        var nordicsBlade1VariablesModel = new VariablesEditModel
        {
            Json = _nordicsBlade1Variables,
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _nordics.Id, _blade1.Id }
        };
        _nordicsBlade2Variables = """"
                                  {
                                       "EnvironmentMachine": "Nordics+Blade2",
                                       "Common": "Common from Nordics+Blade2"
                                  }
                                  """";
        var nordicsBlade2VariablesModel = new VariablesEditModel
        {
            Json = _nordicsBlade2Variables,
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _nordics.Id, _blade2.Id }
        };
        await variableManager.SaveAsync(beneluxBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(beneluxBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsBlade2VariablesModel, cancellationToken);

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
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int>()
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
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int>()
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
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _blade1.Id }
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
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _blade2.Id }
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
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _blade1.Id }
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
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _blade2.Id }
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
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _benelux.Id }
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
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _nordics.Id }
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
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _benelux.Id }
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
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _nordics.Id }
        };
        await variableManager.SaveAsync(processorBeneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorNordicsVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBeneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerNordicsVariablesModel, cancellationToken);

        // Application + Environment + Machine specific config
        _processorBeneluxBlade1Variables = """"
                                           {
                                               "ApplicationEnvironmentMachine": "Processor+Benelux+Blade1",
                                               "Common": "Common from Processor+Benelux+Blade1"
                                           }
                                           """";
        var processorBeneluxBlade1VariablesModel = new VariablesEditModel
        {
            Json = _processorBeneluxBlade1Variables,
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _benelux.Id, _blade1.Id }
        };
        _processorBeneluxBlade2Variables = """"
                                           {
                                                "ApplicationEnvironmentMachine": "Processor+Benelux+Blade2",
                                                "Common": "Common from Processor+Benelux+Blade2"
                                           }
                                           """";
        var processorBeneluxBlade2VariablesModel = new VariablesEditModel
        {
            Json = _processorBeneluxBlade2Variables,
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _benelux.Id, _blade2.Id }
        };
        _processorNordicsBlade1Variables = """"
                                           {
                                                "ApplicationEnvironmentMachine": "Processor+Nordics+Blade1",
                                                "Common": "Common from Processor+Nordics+Blade1"
                                           }
                                           """";
        var processorNordicsBlade1VariablesModel = new VariablesEditModel
        {
            Json = _processorNordicsBlade1Variables,
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _nordics.Id, _blade1.Id }
        };
        _processorNordicsBlade2Variables = """"
                                           {
                                                "ApplicationEnvironmentMachine": "Processor+Nordics+Blade2",
                                                "Common": "Common from Processor+Nordics+Blade2"
                                           }
                                           """";
        var processorNordicsBlade2VariablesModel = new VariablesEditModel
        {
            Json = _processorNordicsBlade2Variables,
            ApplicationIds = new List<int> { _processor.Id },
            TagIds = new List<int> { _nordics.Id, _blade2.Id }
        };
        _routerBeneluxBlade1Variables = """"
                                        {
                                             "ApplicationEnvironmentMachine": "Router+Benelux+Blade1",
                                             "Common": "Common from Router+Benelux+Blade1"
                                        }
                                        """";
        var routerBeneluxBlade1VariablesModel = new VariablesEditModel
        {
            Json = _routerBeneluxBlade1Variables,
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _benelux.Id, _blade1.Id }
        };
        _routerBeneluxBlade2Variables = """"
                                        {
                                            "ApplicationEnvironmentMachine": "Router+Benelux+Blade2",
                                            "Common": "Common from Router+Benelux+Blade2"
                                        }
                                        """";
        var routerBeneluxBlade2VariablesModel = new VariablesEditModel
        {
            Json = _routerBeneluxBlade2Variables,
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _benelux.Id, _blade2.Id }
        };
        _routerNordicsBlade1Variables = """"
                                        {
                                            "ApplicationEnvironmentMachine": "Router+Nordics+Blade1",
                                            "Common": "Common from Router+Nordics+Blade1"
                                        }
                                        """";
        var routerNordicsBlade1VariablesModel = new VariablesEditModel
        {
            Json = _routerNordicsBlade1Variables,
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _nordics.Id, _blade1.Id }
        };
        _routerNordicsBlade2Variables = """"
                                        {
                                             "ApplicationEnvironmentMachine": "Router+Nordics+Blade2",
                                             "Common": "Common from Router+Nordics+Blade2"
                                        }
                                        """";
        var routerNordicsBlade2VariablesModel = new VariablesEditModel
        {
            Json = _routerNordicsBlade2Variables,
            ApplicationIds = new List<int> { _router.Id },
            TagIds = new List<int> { _nordics.Id, _blade2.Id }
        };
        await variableManager.SaveAsync(processorBeneluxBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorBeneluxBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorNordicsBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorNordicsBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBeneluxBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBeneluxBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerNordicsBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerNordicsBlade2VariablesModel, cancellationToken);
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
        var cancellationToken = default(CancellationToken);
        
        // Processor runs on both blades for both environments
        // Router runs on blade 1 for benelux and on blade 2 for nordics
        var processorBlade1BeneluxApiKeyModel = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id, _blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade2BeneluxApiKeyModel = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _benelux.Id, _blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade1NordicsApiKeyModel = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _nordics.Id, _blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade2NordicsApiKeyModel = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _nordics.Id, _blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBlade1BeneluxApiKeyModel = new ApiKeyModel
        {
            ApplicationId = _router.Id,
            TagIds = new List<int> { _benelux.Id, _blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBlade2NordicsApiKeyModel = new ApiKeyModel
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
        var processorBlade1BeneluxApiKey = await apiKeyManager.SaveApiKeyAsync(processorBlade1BeneluxApiKeyModel, cancellationToken);
        var processorBlade2BeneluxApiKey = await apiKeyManager.SaveApiKeyAsync(processorBlade2BeneluxApiKeyModel, cancellationToken);
        var processorBlade1NordicsApiKey = await apiKeyManager.SaveApiKeyAsync(processorBlade1NordicsApiKeyModel, cancellationToken);
        var processorBlade2NordicsApiKey = await apiKeyManager.SaveApiKeyAsync(processorBlade2NordicsApiKeyModel, cancellationToken);
        var routerBlade1BeneluxApiKey = await apiKeyManager.SaveApiKeyAsync(routerBlade1BeneluxApiKeyModel, cancellationToken);
        var routerBlade2NordicsApiKey = await apiKeyManager.SaveApiKeyAsync(routerBlade2NordicsApiKeyModel, cancellationToken);
        _ = await apiKeyManager.SaveApiKeyAsync(otherApiKeyModel, cancellationToken);

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
                "ApplicationEnvironmentMachine": "Processor+Benelux+Blade1",
                "ApplicationMachine": "Processor+Blade1",
                "Common": "Common from Processor+Benelux+Blade1",
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
                "ApplicationEnvironmentMachine": "Processor+Benelux+Blade2",
                "ApplicationMachine": "Processor+Blade2",
                "Common": "Common from Processor+Benelux+Blade2",
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
                "ApplicationEnvironmentMachine": "Processor+Nordics+Blade1",
                "ApplicationMachine": "Processor+Blade1",
                "Common": "Common from Processor+Nordics+Blade1",
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
                "ApplicationEnvironmentMachine": "Processor+Nordics+Blade2",
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
                "ApplicationEnvironmentMachine": "Router+Benelux+Blade1",
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
                "ApplicationEnvironmentMachine": "Router+Nordics+Blade2",
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
        var cancellationToken = default(CancellationToken);
        
        // Act
        var actualBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _blade1.Id },
            cancellationToken
        );

        var actualBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _blade2.Id },
            cancellationToken
        );

        var actualBeneluxVariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _benelux.Id },
            cancellationToken
        );

        var actualNordicsVariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _nordics.Id },
            cancellationToken
        );

        var actualBeneluxBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _benelux.Id, _blade1.Id },
            cancellationToken
        );

        var actualBeneluxBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _benelux.Id, _blade2.Id },
            cancellationToken
        );

        var actualNordicsBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _nordics.Id, _blade1.Id },
            cancellationToken
        );

        var actualNordicsBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _nordics.Id, _blade2.Id },
            cancellationToken
        );

        var actualProcessorVariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int>(),
            cancellationToken
        );

        var actualRouterVariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int>(),
            cancellationToken
        );

        var actualProcessorBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _blade1.Id },
            cancellationToken
        );

        var actualProcessorBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _blade2.Id },
            cancellationToken
        );

        var actualRouterBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _blade1.Id },
            cancellationToken
        );

        var actualRouterBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _blade2.Id },
            cancellationToken
        );

        var actualProcessorBeneluxVariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _benelux.Id },
            cancellationToken
        );

        var actualProcessorNordicsVariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _nordics.Id },
            cancellationToken
        );

        var actualRouterBeneluxVariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _benelux.Id },
            cancellationToken
        );

        var actualRouterNordicsVariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _nordics.Id },
            cancellationToken
        );

        var actualProcessorBeneluxBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _benelux.Id, _blade1.Id },
            cancellationToken
        );

        var actualProcessorBeneluxBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _benelux.Id, _blade2.Id },
            cancellationToken
        );

        var actualProcessorNordicsBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _nordics.Id, _blade1.Id },
            cancellationToken
        );

        var actualProcessorNordicsBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _processor.Id },
            new List<int> { _nordics.Id, _blade2.Id },
            cancellationToken
        );

        var actualRouterBeneluxBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _benelux.Id, _blade1.Id },
            cancellationToken
        );

        var actualRouterBeneluxBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _benelux.Id, _blade2.Id },
            cancellationToken
        );

        var actualRouterNordicsBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _nordics.Id, _blade1.Id },
            cancellationToken
        );

        var actualRouterNordicsBlade2VariablesModel = await variableManager.GetConfigAsync(
            new List<int> { _router.Id },
            new List<int> { _nordics.Id, _blade2.Id },
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
            JsonNormalizer.Normalize(_beneluxBlade1Variables),
            JsonNormalizer.Normalize(actualBeneluxBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_beneluxBlade2Variables),
            JsonNormalizer.Normalize(actualBeneluxBlade2VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_nordicsBlade1Variables),
            JsonNormalizer.Normalize(actualNordicsBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_nordicsBlade2Variables),
            JsonNormalizer.Normalize(actualNordicsBlade2VariablesModel)
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
        Assert.Equal(
            JsonNormalizer.Normalize(_processorBeneluxBlade1Variables),
            JsonNormalizer.Normalize(actualProcessorBeneluxBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorBeneluxBlade2Variables),
            JsonNormalizer.Normalize(actualProcessorBeneluxBlade2VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorNordicsBlade1Variables),
            JsonNormalizer.Normalize(actualProcessorNordicsBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_processorNordicsBlade2Variables),
            JsonNormalizer.Normalize(actualProcessorNordicsBlade2VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerBeneluxBlade1Variables),
            JsonNormalizer.Normalize(actualRouterBeneluxBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerBeneluxBlade2Variables),
            JsonNormalizer.Normalize(actualRouterBeneluxBlade2VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerNordicsBlade1Variables),
            JsonNormalizer.Normalize(actualRouterNordicsBlade1VariablesModel)
        );
        Assert.Equal(
            JsonNormalizer.Normalize(_routerNordicsBlade2Variables),
            JsonNormalizer.Normalize(actualRouterNordicsBlade2VariablesModel)
        );

    }
    
    [Fact]
    public async Task UpdatingExistingConfig()
    {
        // Arrange
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = default(CancellationToken);
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
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { _blade1.Id }
        }, cancellationToken);
        
        var actualBlade1VariablesModel = await variableManager.GetConfigAsync(
            new List<int>(),
            new List<int> { _blade1.Id },
            cancellationToken
        );
        
        // Assert
        Assert.Equal(
            JsonNormalizer.Normalize(newBlade1Variables),
            JsonNormalizer.Normalize(actualBlade1VariablesModel)
        );
    }
}
