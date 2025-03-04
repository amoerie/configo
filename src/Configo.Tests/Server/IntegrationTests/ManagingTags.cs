using Configo.Server.Blazor;
using Configo.Server.Domain;
using MudBlazor;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingTags : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    public ManagingTags(IntegrationTestFixture fixture, ITestOutputHelper output)
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
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        var tagManager = _fixture.GetRequiredService<TagManager>();
        CancellationToken cancellationToken = default;
        
        // Act + Assert
        var group1 = new TagGroupModel { Name = "Group 1", Icon = TagGroupIcon.GetByName(Icons.Material.Filled.Factory) };
        await tagGroupManager.SaveTagGroupAsync(group1, cancellationToken);
        var group2 = new TagGroupModel { Name = "Group 2", Icon = TagGroupIcon.GetByName(Icons.Material.Filled._1k) };
        await tagGroupManager.SaveTagGroupAsync(group2, cancellationToken);
        var tag = new TagModel { Name = "Test 1", GroupId = group1.Id, GroupIcon = group1.Icon };
        await tagManager.SaveTagAsync(tag, cancellationToken);
        var tagsOfGroup1 = await tagManager.GetAllTagsAsync(group1.Id, cancellationToken);
        var tagsOfGroup2 = await tagManager.GetAllTagsAsync(group2.Id, cancellationToken);

        tagsOfGroup1.Should().HaveCount(1);
        tagsOfGroup1.Single().Name.Should().Be("Test 1");
        tagsOfGroup2.Should().BeEmpty();

        tag.Name = "Test 2";
        await tagManager.SaveTagAsync(tag, cancellationToken);
        
        tagsOfGroup1 = await tagManager.GetAllTagsAsync(group1.Id, cancellationToken);
        tagsOfGroup1.Should().HaveCount(1);
        tagsOfGroup1.Single().Name.Should().Be("Test 2");

        await tagManager.DeleteTagAsync(tag, cancellationToken);
        tagsOfGroup1 = await tagManager.GetAllTagsAsync(group1.Id, cancellationToken);
        tagsOfGroup1.Should().HaveCount(0);
    }
}
