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
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        var tagManager = _fixture.GetRequiredService<TagManager>();
        CancellationToken cancellationToken = default;
        
        // Act + Assert
        var application = await applicationManager.SaveApplicationAsync(new ApplicationModel { Name = "App" }, cancellationToken);
        var tagGroup1 = await tagGroupManager.SaveTagGroupAsync(new TagGroupModel { Name = "Group 1" }, cancellationToken);
        var tagGroup2 = await tagGroupManager.SaveTagGroupAsync(new TagGroupModel { Name = "Group 2" }, cancellationToken);
        var tag1 = await tagManager.SaveTagAsync(new TagModel { Name = "Tag 1", TagGroupId = tagGroup1.Id }, cancellationToken);
        var tag2 = await tagManager.SaveTagAsync(new TagModel { Name = "Tag 2", TagGroupId = tagGroup2.Id }, cancellationToken);
        var apiKeyEditModel = new ApiKeyEditModel
        {
            ApplicationId = application.Id,
            TagIds = new List<int> { tag1.Id, tag2.Id },
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1)
        };
        var apiKey = await apiKeyManager.SaveApiKeyAsync(apiKeyEditModel, cancellationToken);
        var apiKeys = await apiKeyManager.GetAllApiKeysAsync(cancellationToken);

        apiKeys.Should().HaveCount(1);
        var apiKeyListModel = apiKeys.Single();
        apiKeyListModel.ApplicationId.Should().Be(application.Id);
        apiKeyListModel.TagIds.Should().BeEquivalentTo(apiKeyEditModel.TagIds);

        apiKeyEditModel = apiKeyEditModel with
        {
            Id = apiKey.Id,
            TagIds = new List<int> { tag2.Id }
        };
        
        await apiKeyManager.SaveApiKeyAsync(apiKeyEditModel, cancellationToken);
        
        apiKeys = await apiKeyManager.GetAllApiKeysAsync(cancellationToken);
        apiKeys.Should().HaveCount(1);
        apiKeyListModel = apiKeys.Single();
        apiKeyListModel.ApplicationId.Should().Be(application.Id);
        apiKeyListModel.TagIds.Should().BeEquivalentTo(apiKeyEditModel.TagIds);

        await apiKeyManager.DeleteApiKeyAsync(new ApiKeyDeleteModel { Id = apiKey.Id }, cancellationToken);
        apiKeys = await apiKeyManager.GetAllApiKeysAsync(cancellationToken);
        apiKeys.Should().HaveCount(0);
    }
}
