using Configo.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.IntegrationTests;

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
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        CancellationToken cancellationToken = default;
        
        // Act + Assert
        var tagGroup = await tagGroupManager.SaveTagGroupAsync(new TagGroupEditModel { Name = "Test 1" }, cancellationToken);
        var tagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);

        tagGroups.Should().HaveCount(1);
        tagGroups.Single().Name.Should().Be("Test 1");

        await tagGroupManager.SaveTagGroupAsync(new TagGroupEditModel { Id = tagGroup.Id, Name = "Test 2" }, cancellationToken);
        
        tagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);
        tagGroups.Should().HaveCount(1);
        tagGroups.Single().Name.Should().Be("Test 2");

        await tagGroupManager.DeleteTagGroupAsync(new TagGroupDeleteModel { Id = tagGroup.Id }, cancellationToken);
        tagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);
        tagGroups.Should().HaveCount(0);
    }
}
