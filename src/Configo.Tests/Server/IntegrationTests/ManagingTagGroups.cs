using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingTagGroups : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    public ManagingTagGroups(IntegrationTestFixture fixture, ITestOutputHelper output)
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
        var tagManager = _fixture.GetRequiredService<TagManager>();
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        CancellationToken cancellationToken = CancellationToken.None;
        
        // Act + Assert
        var tagGroup = new TagGroupModel { Name = "Group 1" };
        await tagGroupManager.SaveTagGroupAsync(tagGroup, cancellationToken);
        var tag = new TagFormModel { Name = "Test 1", TagGroupId = tagGroup.Id };
        await tagManager.SaveTagAsync(tag, cancellationToken);
        var allTagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);

        Assert.Single(allTagGroups);
        var firstTagGroup = allTagGroups.Single();
        Assert.Equal("Group 1", firstTagGroup.Name);

        tagGroup = new TagGroupModel { Id = firstTagGroup.Id, Name = "Group 2" };
        await tagGroupManager.SaveTagGroupAsync(tagGroup, cancellationToken);
        
        allTagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);
        Assert.Single(allTagGroups);
        firstTagGroup = allTagGroups.Single();
        Assert.Equal("Group 2", firstTagGroup.Name);

        await tagGroupManager.DeleteTagGroupAsync(firstTagGroup, cancellationToken);
        allTagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);
        Assert.Empty(allTagGroups);
    }
}
