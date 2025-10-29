using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingVariables : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private TagFormModel _global = null!;
    private TagFormModel _benelux = null!;
    private TagFormModel _nordics = null!;
    private TagFormModel _blade1 = null!;
    private TagFormModel _blade2 = null!;
    private ApplicationModel _processor = null!;
    private ApplicationModel _router = null!;
    private ApplicationModel _otherApplication = null!;
    private TagFormModel _otherTagForm = null!;
    private string _blade1Variables = null!;
    private string _blade2Variables = null!;
    private string _beneluxVariables = null!;
    private string _nordicsVariables = null!;
    private string _globalVariables = null!;

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

        var environments = new TagGroupModel { Name = "Environments" };
        var globalGroup = new TagGroupModel { Name = "Global" };
        await tagGroupManager.SaveTagGroupAsync(environments, cancellationToken);
        await tagGroupManager.SaveTagGroupAsync(globalGroup, cancellationToken);
        _global = new TagFormModel { Name = "Global", TagGroupId = globalGroup.Id };
        _benelux = new TagFormModel { Name = "Benelux", TagGroupId = environments.Id };
        _nordics = new TagFormModel { Name = "Nordics", TagGroupId = environments.Id };
        _blade1 = new TagFormModel { Name = "Blade 1", TagGroupId = environments.Id };
        _blade2 = new TagFormModel { Name = "Blade 2", TagGroupId = environments.Id };
        _otherTagForm = new TagFormModel { Name = "Other Tag", TagGroupId = environments.Id };
        await tagManager.SaveTagAsync(_benelux, cancellationToken);
        await tagManager.SaveTagAsync(_nordics, cancellationToken);
        await tagManager.SaveTagAsync(_blade1, cancellationToken);
        await tagManager.SaveTagAsync(_blade2, cancellationToken);
        await tagManager.SaveTagAsync(_otherTagForm, cancellationToken);
        await tagManager.SaveTagAsync(_global, cancellationToken);
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
                       "Common": "Common from other tag"
                   }
                   """",
            TagId = _otherTagForm.Id,
            ApplicationId = _processor.Id
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
            TagId = _blade1.Id,
            ApplicationId = _processor.Id
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
            TagId = _blade2.Id,
            ApplicationId = _processor.Id
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
            TagId = _benelux.Id,
            ApplicationId = _processor.Id
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
            TagId = _nordics.Id,
            ApplicationId = _processor.Id
        };
        await variableManager.SaveAsync(beneluxVariablesModel, cancellationToken);
        await variableManager.SaveAsync(nordicsVariablesModel, cancellationToken);

        // Global config
        _globalVariables = """"
                           {
                               "Common": "Common from Global"
                           }
                           """";
        var globalVariablesModel = new VariablesEditModel
        {
            Json = _globalVariables,
            TagId = _global.Id,
            ApplicationId = _processor.Id
        };
        await variableManager.SaveAsync(globalVariablesModel, cancellationToken);
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
            TagIds = new List<int> { _otherTagForm.Id },
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
                "Common": "Common from Blade1",
                "Environment": "Benelux",
                "Machine": "Blade1"
            }
            """;
        var expectedProcessorBlade2BeneluxConfig =
            """
            {
                "Common": "Common from Blade2",
                "Environment": "Benelux",
                "Machine": "Blade2"
            }
            """;
        var expectedProcessorBlade1NordicsConfig =
            """
            {
                "Common": "Common from Blade1",
                "Environment": "Nordics",
                "Machine": "Blade1"
            }
            """;
        var expectedProcessorBlade2NordicsConfig =
            """
            {
                "Common": "Common from Blade2",
                "Environment": "Nordics",
                "Machine": "Blade2"
            }
            """;
        var expectedRouterBlade1BeneluxConfig =
            """
            {
                "Common": "Common from Blade1",
                "Environment": "Benelux",
                "Machine": "Blade1"
            }
            """;
        var expectedRouterBlade2NordicsConfig =
            """
            {
                "Common": "Common from Blade2",
                "Environment": "Nordics",
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
            _blade1.Id,
            cancellationToken
        );

        var actualBlade2VariablesModel = await variableManager.GetConfigAsync(
            _blade2.Id,
            cancellationToken
        );

        var actualBeneluxVariablesModel = await variableManager.GetConfigAsync(
            _benelux.Id,
            cancellationToken
        );

        var actualNordicsVariablesModel = await variableManager.GetConfigAsync(
            _nordics.Id,
            cancellationToken
        );

        var actualGlobalVariables = await variableManager.GetConfigAsync(
            _global.Id,
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
            JsonNormalizer.Normalize(_globalVariables),
            JsonNormalizer.Normalize(actualGlobalVariables)
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
            TagId = _blade1.Id,
            ApplicationId = _processor.Id
        }, cancellationToken);

        var actualBlade1VariablesModel = await variableManager.GetConfigAsync(
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
