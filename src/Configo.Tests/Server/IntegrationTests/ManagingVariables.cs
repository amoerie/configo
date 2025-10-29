using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingVariables : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagFormModel _acceptation = null!;
    private TagFormModel _production = null!;
    private TagFormModel _benelux = null!;
    private TagFormModel _france = null!;
    private TagFormModel _otherTag = null!;
    private ApplicationModel _processor = null!;
    private ApplicationModel _router = null!;
    private ApplicationModel _otherApplication = null!;

    public ManagingVariables(IntegrationTestFixture fixture, ITestOutputHelper output)
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

        // Tag groups
        var environments = new TagGroupModel { Name = "Environments", Order = 1 };
        var regions = new TagGroupModel { Name = "Regions", Order = 2 };
        var otherTagGroup = new TagGroupModel { Name = "OtherTagGroup", Order = 3 };
        await tagGroupManager.SaveTagGroupAsync(regions, cancellationToken);
        await tagGroupManager.SaveTagGroupAsync(environments, cancellationToken);
        await tagGroupManager.SaveTagGroupAsync(otherTagGroup, cancellationToken);

        // Tags
        _acceptation = new TagFormModel { Name = "Acceptation", TagGroupId = environments.Id };
        _production = new TagFormModel { Name = "Production", TagGroupId = environments.Id };
        _benelux = new TagFormModel { Name = "Benelux", TagGroupId = regions.Id };
        _france = new TagFormModel { Name = "France", TagGroupId = regions.Id };
        _otherTag = new TagFormModel { Name = "Other Tag", TagGroupId = otherTagGroup.Id };
        await tagManager.SaveTagAsync(_benelux, cancellationToken);
        await tagManager.SaveTagAsync(_france, cancellationToken);
        await tagManager.SaveTagAsync(_acceptation, cancellationToken);
        await tagManager.SaveTagAsync(_production, cancellationToken);
        await tagManager.SaveTagAsync(_otherTag, cancellationToken);

        // Applications
        _processor = new ApplicationModel { Name = "Processor" };
        _router = new ApplicationModel { Name = "Router" };
        _otherApplication = new ApplicationModel { Name = "Other" };
        await applicationManager.SaveApplicationAsync(_processor, cancellationToken);
        await applicationManager.SaveApplicationAsync(_router, cancellationToken);
        await applicationManager.SaveApplicationAsync(_otherApplication, cancellationToken);

        // Variables

        // Environment: acceptation or production
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "NamePrefix": "AcceptationProcessor"     
                       },
                       "LogLevel": "Debug"
                   }
                   """",
            TagId = _acceptation.Id,
            ApplicationId = _processor.Id
        }, cancellationToken);
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "NamePrefix": "AcceptationRouter"     
                       },
                       "LogLevel": "Debug"
                   }
                   """",
            TagId = _acceptation.Id,
            ApplicationId = _router.Id
        }, cancellationToken);
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "NamePrefix": "ProductionProcessor"     
                       },
                       "LogLevel": "Warning"
                   }
                   """",
            TagId = _production.Id,
            ApplicationId = _processor.Id
        }, cancellationToken);
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "NamePrefix": "ProductionRouter"     
                       },
                       "LogLevel": "Warning"
                   }
                   """",
            TagId = _production.Id,
            ApplicationId = _router.Id
        }, cancellationToken);

        // Region: benelux or france
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "Server": "rabbitmq_benelux",
                           "UserName": "rabbitmq_processor_benelux"
                       }
                   }
                   """",
            TagId = _benelux.Id,
            ApplicationId = _processor.Id
        }, cancellationToken);
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "Server": "rabbitmq_benelux",
                           "UserName": "rabbitmq_router_benelux"
                       }
                   }
                   """",
            TagId = _benelux.Id,
            ApplicationId = _router.Id
        }, cancellationToken);
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "Server": "rabbitmq_france",
                           "UserName": "rabbitmq_processor_france"
                       }
                   }
                   """",
            TagId = _france.Id,
            ApplicationId = _processor.Id
        }, cancellationToken);
        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "Server": "rabbitmq_france",
                           "UserName": "rabbitmq_router_france"
                       }
                   }
                   """",
            TagId = _france.Id,
            ApplicationId = _router.Id
        }, cancellationToken);
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

        var processorBeneluxAcceptationApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _acceptation.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBeneluxProductionApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _production.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorFranceProductionApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _production.Id, _france.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBeneluxAcceptationApiKey = new ApiKeyModel
        {
            ApplicationId = _router.Id,
            TagIds = new List<int> { _acceptation.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBeneluxProductionApiKey = new ApiKeyModel
        {
            ApplicationId = _router.Id,
            TagIds = new List<int> { _production.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerFranceProductionApiKey = new ApiKeyModel
        {
            ApplicationId = _router.Id,
            TagIds = new List<int> { _production.Id, _france.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        await apiKeyManager.SaveApiKeyAsync(processorBeneluxAcceptationApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(processorBeneluxProductionApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(processorFranceProductionApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(routerBeneluxAcceptationApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(routerBeneluxProductionApiKey, cancellationToken);
        await apiKeyManager.SaveApiKeyAsync(routerFranceProductionApiKey, cancellationToken);

        // Act
        var processorBeneluxAcceptationConfig = await variableManager.GetMergedConfigAsync(processorBeneluxAcceptationApiKey.Id, cancellationToken);
        var processorBeneluxProductionConfig = await variableManager.GetMergedConfigAsync(processorBeneluxProductionApiKey.Id, cancellationToken);
        var processorFranceProductionConfig = await variableManager.GetMergedConfigAsync(processorFranceProductionApiKey.Id, cancellationToken);
        var routerBeneluxAcceptationConfig = await variableManager.GetMergedConfigAsync(routerBeneluxAcceptationApiKey.Id, cancellationToken);
        var routerBeneluxProductionConfig = await variableManager.GetMergedConfigAsync(routerBeneluxProductionApiKey.Id, cancellationToken);
        var routerFranceProductionConfig = await variableManager.GetMergedConfigAsync(routerFranceProductionApiKey.Id, cancellationToken);

        // Assert
        var expectedProcessorBeneluxAcceptationConfig =
            """
            {
                "LogLevel": "Debug",
                "RabbitMq": {
                    "NamePrefix": "AcceptationProcessor",
                    "Server": "rabbitmq_benelux",
                    "UserName": "rabbitmq_processor_benelux"
                }
            }
            """;
        var expectedProcessorBeneluxProductionConfig =
            """
            {
                "LogLevel": "Warning",
                "RabbitMq": {
                    "NamePrefix": "ProductionProcessor",
                    "Server": "rabbitmq_benelux",
                    "UserName": "rabbitmq_processor_benelux"
                }
            }
            """;
        var expectedProcessorFranceProductionConfig =
            """
            {
                "LogLevel": "Warning",
                "RabbitMq": {
                    "NamePrefix": "ProductionProcessor",
                    "Server": "rabbitmq_france",
                    "UserName": "rabbitmq_processor_france"
                }
            }
            """;
        var expectedRouterBeneluxAcceptationConfig =
            """
            {
                "LogLevel": "Debug",
                "RabbitMq": {
                    "NamePrefix": "AcceptationRouter",
                    "Server": "rabbitmq_benelux",
                    "UserName": "rabbitmq_router_benelux"
                }
            }
            """;
        var expectedRouterBeneluxProductionConfig =
            """
            {
                "LogLevel": "Warning",
                "RabbitMq": {
                    "NamePrefix": "ProductionRouter",
                    "Server": "rabbitmq_benelux",
                    "UserName": "rabbitmq_router_benelux"
                }
            }
            """;
        var expectedRouterFranceProductionConfig =
            """
            {
                "LogLevel": "Warning",
                "RabbitMq": {
                    "NamePrefix": "ProductionRouter",
                    "Server": "rabbitmq_france",
                    "UserName": "rabbitmq_router_france"
                }
            }
            """;

        Assert.Equal(JsonNormalizer.Normalize(expectedProcessorBeneluxAcceptationConfig), JsonNormalizer.Normalize(processorBeneluxAcceptationConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedProcessorBeneluxProductionConfig), JsonNormalizer.Normalize(processorBeneluxProductionConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedProcessorFranceProductionConfig), JsonNormalizer.Normalize(processorFranceProductionConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedRouterBeneluxAcceptationConfig), JsonNormalizer.Normalize(routerBeneluxAcceptationConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedRouterBeneluxProductionConfig), JsonNormalizer.Normalize(routerBeneluxProductionConfig));
        Assert.Equal(JsonNormalizer.Normalize(expectedRouterFranceProductionConfig), JsonNormalizer.Normalize(routerFranceProductionConfig));
    }

    [Fact]
    public async Task UpdatingExistingConfig()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        await variableManager.SaveAsync(new VariablesEditModel
        {
            Json = """"
                   {
                       "RabbitMq": {
                           "NamePrefix": "ProductionProcessor_Modified"     
                       },
                       "LogLevel": "Error"
                   }
                   """",
            TagId = _production.Id,
            ApplicationId = _processor.Id
        }, cancellationToken);

        var expectedProcessorBeneluxProductionConfig =
            """
            {
                "LogLevel": "Error",
                "RabbitMq": {
                    "NamePrefix": "ProductionProcessor_Modified",
                    "Server": "rabbitmq_benelux",
                    "UserName": "rabbitmq_processor_benelux"
                }
            }
            """;

        var processorBeneluxProductionApiKey = new ApiKeyModel
        {
            ApplicationId = _processor.Id,
            TagIds = new List<int> { _production.Id, _benelux.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        await apiKeyManager.SaveApiKeyAsync(processorBeneluxProductionApiKey, cancellationToken);

        // Act
        var processorBeneluxProductionConfig = await variableManager.GetMergedConfigAsync(processorBeneluxProductionApiKey.Id, cancellationToken);

        // Assert
        Assert.Equal(
            JsonNormalizer.Normalize(expectedProcessorBeneluxProductionConfig),
            JsonNormalizer.Normalize(processorBeneluxProductionConfig)
        );
    }
}
