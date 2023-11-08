using Configo.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class GettingConfigWithApiKeys : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    public GettingConfigWithApiKeys(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _fixture.SetOutput(output);
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return _fixture.ResetAsync();
    }

    [Fact]
    public async Task ShouldReturnCorrectVariables()
    {
        // Arrange
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        var tagManager = _fixture.GetRequiredService<TagManager>();
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        var variableManager = _fixture.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        var environmentsModel = new TagGroupEditModel { Name = "Environments" };
        var machinesModel = new TagGroupEditModel { Name = "Machines" };
        var otherTagGroupModel = new TagGroupEditModel { Name = "Other" };
        var environments = await tagGroupManager.SaveTagGroupAsync(environmentsModel, cancellationToken);
        var machines = await tagGroupManager.SaveTagGroupAsync(machinesModel, cancellationToken);
        var otherTagGroup = await tagGroupManager.SaveTagGroupAsync(otherTagGroupModel, cancellationToken);
        var beneluxModel = new TagEditModel { Name = "Benelux", TagGroupId = environments.Id };
        var nordicsModel = new TagEditModel { Name = "Nordics", TagGroupId = environments.Id };
        var blade1Model = new TagEditModel { Name = "Blade 1", TagGroupId = machines.Id };
        var blade2Model = new TagEditModel { Name = "Blade 2", TagGroupId = machines.Id };
        var otherTagModel = new TagEditModel { Name = "Other Tag", TagGroupId = otherTagGroup.Id };
        var benelux = await tagManager.SaveTagAsync(beneluxModel, cancellationToken);
        var nordics = await tagManager.SaveTagAsync(nordicsModel, cancellationToken);
        var blade1 = await tagManager.SaveTagAsync(blade1Model, cancellationToken);
        var blade2 = await tagManager.SaveTagAsync(blade2Model, cancellationToken);
        var otherTag = await tagManager.SaveTagAsync(otherTagModel, cancellationToken);
        var processorModel = new ApplicationEditModel { Name = "Processor" };
        var routerModel = new ApplicationEditModel { Name = "Router" };
        var otherApplicationModel = new ApplicationEditModel { Name = "Other" };
        var processor = await applicationManager.SaveApplicationAsync(processorModel, cancellationToken);
        var router = await applicationManager.SaveApplicationAsync(routerModel, cancellationToken);
        var otherApplication = await applicationManager.SaveApplicationAsync(otherApplicationModel, cancellationToken);

        // Other config
        var otherVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "Some": "Other"
                   }
                   """",
            ApplicationIds = new List<int> { otherApplication.Id },
            TagIds = new List<int> { otherTag.Id }
        };
        await variableManager.SaveAsync(otherVariablesModel, cancellationToken);

        // Machine specific config
        var blade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "Machine": "Blade1",
                        "Common": "Common from Blade1",
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { blade1.Id }
        };
        var blade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "Machine": "Blade2",
                        "Common": "Common from Blade2",
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { blade2.Id }
        };
        await variableManager.SaveAsync(blade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(blade2VariablesModel, cancellationToken);

        // Environment specific config
        var beneluxVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "Environment": "Benelux",
                        "Common": "Common from Benelux"
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { benelux.Id }
        };
        var nordicsVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "Environment": "Nordics",
                        "Common": "Common from Nordics"
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { nordics.Id }
        };
        await variableManager.SaveAsync(beneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsVariablesModel, cancellationToken);

        // Environment + Machine specific config
        var beneluxBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "EnvironmentMachine": "Benelux+Blade1",
                        "Common": "Common from Benelux+Blade1"
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { benelux.Id, blade1.Id }
        };
        var beneluxBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "EnvironmentMachine": "Benelux+Blade2",
                        "Common": "Common from Benelux+Blade2"
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { benelux.Id, blade2.Id }
        };
        var nordicsBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "EnvironmentMachine": "Nordics+Blade1",
                        "Common": "Common from Nordics+Blade1"
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { nordics.Id, blade1.Id }
        };
        var nordicsBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "EnvironmentMachine": "Nordics+Blade2",
                        "Common": "Common from Nordics+Blade2"
                   }
                   """",
            ApplicationIds = new List<int>(),
            TagIds = new List<int> { nordics.Id, blade2.Id }
        };
        await variableManager.SaveAsync(beneluxBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(beneluxBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsBlade2VariablesModel, cancellationToken);

        // Application specific config
        var processorVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "Application": "Processor",
                       "Common": "Common from Processor"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int>()
        };
        var routerVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "Application": "Router",
                       "Common": "Common from Router"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int>()
        };
        await variableManager.SaveAsync(processorVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerVariablesModel, cancellationToken);

        // Application + Machine specific config
        var processorBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "ApplicationMachine": "Processor+Blade1",
                       "Common": "Common from Processor+Blade1"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { blade1.Id }
        };
        var processorBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationMachine": "Processor+Blade2",
                        "Common": "Common from Processor+Blade2"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { blade2.Id }
        };
        var routerBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationMachine": "Router+Blade1",
                        "Common": "Common from Router+Blade1"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { blade1.Id }
        };
        var routerBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationMachine": "Router+Blade2",
                        "Common": "Common from Router+Blade2"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { blade2.Id }
        };
        await variableManager.SaveAsync(processorBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBlade2VariablesModel, cancellationToken);

        // Application + Environment specific config
        var processorBeneluxVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "ApplicationEnvironment": "Processor+Benelux",
                       "Common": "Common from Processor+Benelux"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { benelux.Id }
        };
        var processorNordicsVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironment": "Processor+Nordics",
                        "Common": "Common from Processor+Nordics"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { nordics.Id }
        };
        var routerBeneluxVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironment": "Router+Benelux",
                        "Common": "Common from Router+Benelux"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { benelux.Id }
        };
        var routerNordicsVariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironment": "Router+Nordics",
                        "Common": "Common from Router+Nordics"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { nordics.Id }
        };
        await variableManager.SaveAsync(processorBeneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorNordicsVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBeneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerNordicsVariablesModel, cancellationToken);

        // Application + Environment + Machine specific config
        var processorBeneluxBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "ApplicationEnvironmentMachine": "Processor+Benelux+Blade1",
                       "Common": "Common from Processor+Benelux+Blade1"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { benelux.Id, blade1.Id }
        };
        var processorBeneluxBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironmentMachine": "Processor+Benelux+Blade2",
                        "Common": "Common from Processor+Benelux+Blade2"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { benelux.Id, blade2.Id }
        };
        var processorNordicsBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironmentMachine": "Processor+Nordics+Blade1",
                        "Common": "Common from Processor+Nordics+Blade1"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { nordics.Id, blade1.Id }
        };
        var processorNordicsBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironmentMachine": "Processor+Nordics+Blade2",
                        "Common": "Common from Processor+Nordics+Blade2"
                   }
                   """",
            ApplicationIds = new List<int> { processor.Id },
            TagIds = new List<int> { nordics.Id, blade2.Id }
        };
        var routerBeneluxBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironmentMachine": "Router+Benelux+Blade1",
                        "Common": "Common from Router+Benelux+Blade1"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { benelux.Id, blade1.Id }
        };
        var routerBeneluxBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "ApplicationEnvironmentMachine": "Router+Benelux+Blade2",
                       "Common": "Common from Router+Benelux+Blade2"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { benelux.Id, blade2.Id }
        };
        var routerNordicsBlade1VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                       "ApplicationEnvironmentMachine": "Router+Nordics+Blade1",
                       "Common": "Common from Router+Nordics+Blade1"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { nordics.Id, blade1.Id }
        };
        var routerNordicsBlade2VariablesModel = new VariablesEditModel
        {
            Json = """"
                   {
                        "ApplicationEnvironmentMachine": "Router+Nordics+Blade2",
                        "Common": "Common from Router+Nordics+Blade2"
                   }
                   """",
            ApplicationIds = new List<int> { router.Id },
            TagIds = new List<int> { nordics.Id, blade2.Id }
        };
        await variableManager.SaveAsync(processorBeneluxBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorBeneluxBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorNordicsBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(processorNordicsBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBeneluxBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerBeneluxBlade2VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerNordicsBlade1VariablesModel, cancellationToken);
        await variableManager.SaveAsync(routerNordicsBlade2VariablesModel, cancellationToken);

        // Processor runs on both blades for both environments
        // Router runs on blade 1 for benelux and on blade 2 for nordics
        var processorBlade1BeneluxApiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = processor.Id,
            TagIds = new List<int> { benelux.Id, blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade2BeneluxApiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = processor.Id,
            TagIds = new List<int> { benelux.Id, blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade1NordicsApiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = processor.Id,
            TagIds = new List<int> { nordics.Id, blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var processorBlade2NordicsApiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = processor.Id,
            TagIds = new List<int> { nordics.Id, blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBlade1BeneluxApiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = router.Id,
            TagIds = new List<int> { benelux.Id, blade1.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var routerBlade2NordicsApiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = router.Id,
            TagIds = new List<int> { nordics.Id, blade2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
        };
        var otherApiKeyModel = new ApiKeyEditModel
        {
            ApplicationId = otherApplication.Id,
            TagIds = new List<int> { otherTag.Id },
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
        var processorBlade1BeneluxConfig = await variableManager.GetConfigAsync(processorBlade1BeneluxApiKey.Id, cancellationToken);
        var processorBlade2BeneluxConfig = await variableManager.GetConfigAsync(processorBlade2BeneluxApiKey.Id, cancellationToken);
        var processorBlade1NordicsConfig = await variableManager.GetConfigAsync(processorBlade1NordicsApiKey.Id, cancellationToken);
        var processorBlade2NordicsConfig = await variableManager.GetConfigAsync(processorBlade2NordicsApiKey.Id, cancellationToken);
        var routerBlade1BeneluxConfig = await variableManager.GetConfigAsync(routerBlade1BeneluxApiKey.Id, cancellationToken);
        var routerBlade2NordicsConfig = await variableManager.GetConfigAsync(routerBlade2NordicsApiKey.Id, cancellationToken);

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
}
