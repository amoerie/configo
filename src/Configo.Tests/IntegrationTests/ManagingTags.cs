using Configo.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.IntegrationTests;

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
        var group1 = await tagGroupManager.SaveTagGroupAsync(new TagGroupEditModel { Name = "Group 1" }, cancellationToken);
        var group2 = await tagGroupManager.SaveTagGroupAsync(new TagGroupEditModel { Name = "Group 2" }, cancellationToken);
        var tag = await tagManager.SaveTagAsync(new TagEditModel { Name = "Test 1", TagGroupId = group1.Id }, cancellationToken);
        var tagsOfGroup1 = await tagManager.GetTagsOfGroupAsync(group1.Id, cancellationToken);
        var tagsOfGroup2 = await tagManager.GetTagsOfGroupAsync(group2.Id, cancellationToken);

        tagsOfGroup1.Should().HaveCount(1);
        tagsOfGroup1.Single().Name.Should().Be("Test 1");
        tagsOfGroup2.Should().BeEmpty();

        await tagManager.SaveTagAsync(new TagEditModel { Id = tag.Id, TagGroupId = group1.Id, Name = "Test 2" }, cancellationToken);
        
        tagsOfGroup1 = await tagManager.GetTagsOfGroupAsync(group1.Id, cancellationToken);
        tagsOfGroup1.Should().HaveCount(1);
        tagsOfGroup1.Single().Name.Should().Be("Test 2");

        await tagManager.DeleteTagAsync(new TagDeleteModel { Id = tag.Id }, cancellationToken);
        tagsOfGroup1 = await tagManager.GetTagsOfGroupAsync(group1.Id, cancellationToken);
        tagsOfGroup1.Should().HaveCount(0);
    }
}
