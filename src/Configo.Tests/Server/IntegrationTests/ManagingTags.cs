using Configo.Server.Domain;
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
        var tagManager = _fixture.GetRequiredService<TagManager>();
        CancellationToken cancellationToken = CancellationToken.None;
        
        // Act + Assert
        var tag = new TagModel { Name = "Test 1" };
        await tagManager.SaveTagAsync(tag, cancellationToken);
        var allTags = await tagManager.GetAllTagsAsync(cancellationToken);

        allTags.Should().HaveCount(1);
        allTags.Single().Name.Should().Be("Test 1");

        tag.Name = "Test 2";
        await tagManager.SaveTagAsync(tag, cancellationToken);
        
        allTags = await tagManager.GetAllTagsAsync(cancellationToken);
        allTags.Should().HaveCount(1);
        allTags.Single().Name.Should().Be("Test 2");

        await tagManager.DeleteTagAsync(tag, cancellationToken);
        allTags = await tagManager.GetAllTagsAsync(cancellationToken);
        allTags.Should().HaveCount(0);
    }
}
