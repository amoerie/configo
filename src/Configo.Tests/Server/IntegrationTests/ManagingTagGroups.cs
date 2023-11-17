using Configo.Server.Blazor;
using Configo.Server.Domain;
using MudBlazor;
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
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        CancellationToken cancellationToken = default;
        
        // Act + Assert
        var tagGroup = new TagGroupModel
        {
            Name = "Test 1",
            Icon = TagGroupIcon.GetByName(Icons.Material.Filled.Factory)
        };
        await tagGroupManager.SaveTagGroupAsync(tagGroup, cancellationToken);
        var tagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);

        tagGroups.Should().HaveCount(1);
        tagGroups.Single().Name.Should().Be("Test 1");
        tagGroups.Single().Icon.Should().Be(TagGroupIcon.GetByName(Icons.Material.Filled.Factory));

        await tagGroupManager.SaveTagGroupAsync(
            new TagGroupModel { Id = tagGroup.Id, Name = "Test 2", Icon = TagGroupIcon.GetByName(Icons.Material.Filled.Face2) }, cancellationToken);
        
        tagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);
        tagGroups.Should().HaveCount(1);
        tagGroups.Single().Name.Should().Be("Test 2");
        tagGroups.Single().Icon.Should().Be(TagGroupIcon.GetByName(Icons.Material.Filled.Face2));

        await tagGroupManager.DeleteTagGroupAsync(tagGroup, cancellationToken);
        tagGroups = await tagGroupManager.GetAllTagGroupsAsync(cancellationToken);
        tagGroups.Should().HaveCount(0);
    }
}
