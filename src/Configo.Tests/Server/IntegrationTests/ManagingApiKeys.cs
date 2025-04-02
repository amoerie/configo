using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingApiKeys : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    public ManagingApiKeys(IntegrationTestFixture fixture, ITestOutputHelper output)
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
    public async Task CrudShouldWorkCorrectly()
    {
        // Arrange
        var apiKeyManager = _fixture.GetRequiredService<ApiKeyManager>();
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        var tagManager = _fixture.GetRequiredService<TagManager>();
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        CancellationToken cancellationToken = default;
        
        // Act + Assert
        var tagGroup = new TagGroupModel { Name = "Group 1" };
        await tagGroupManager.SaveTagGroupAsync(tagGroup, cancellationToken);
        var application = new ApplicationModel { Name = "App" };
        await applicationManager.SaveApplicationAsync(application, cancellationToken);
        var tag1 = new TagModel { Name = "Tag 1", TagGroupId = tagGroup.Id };
        var tag2 = new TagModel { Name = "Tag 2", TagGroupId = tagGroup.Id };
        await tagManager.SaveTagAsync(tag1, cancellationToken);
        await tagManager.SaveTagAsync(tag2, cancellationToken);
        var apiKey = new ApiKeyModel
        {
            ApplicationId = application.Id,
            TagIds = new List<int> { tag1.Id, tag2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
            Key = "",
        };
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);
        var apiKeys = await apiKeyManager.GetAllApiKeysAsync(cancellationToken);

        Assert.Single(apiKeys);
        var apiKeyListModel = apiKeys.Single();
        Assert.Equal(application.Id, apiKeyListModel.ApplicationId);
        Assert.Equivalent(apiKey.TagIds, apiKeyListModel.TagIds);

        apiKey = apiKey with
        {
            Id = apiKey.Id,
            TagIds = new List<int> { tag2.Id }
        };
        
        await apiKeyManager.SaveApiKeyAsync(apiKey, cancellationToken);
        
        apiKeys = await apiKeyManager.GetAllApiKeysAsync(cancellationToken);
        Assert.Single(apiKeys);
        apiKeyListModel = apiKeys.Single();
        Assert.Equal(application.Id, apiKeyListModel.ApplicationId);
        Assert.Equivalent(apiKey.TagIds, apiKeyListModel.TagIds);

        await apiKeyManager.DeleteApiKeyAsync(apiKey, cancellationToken);
        apiKeys = await apiKeyManager.GetAllApiKeysAsync(cancellationToken);
        Assert.Empty(apiKeys);
    }
}
