﻿using Configo.Server.Domain;
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
        var tagGroupManager = _fixture.GetRequiredService<TagGroupManager>();
        CancellationToken cancellationToken = CancellationToken.None;
        
        // Act + Assert
        var tagGroup = new TagGroupModel { Name = "Group 1" };
        await tagGroupManager.SaveTagGroupAsync(tagGroup, cancellationToken);
        var tag = new TagFormModel { Name = "Test 1", TagGroupId = tagGroup.Id };
        await tagManager.SaveTagAsync(tag, cancellationToken);
        var allTags = await tagManager.GetAllTagsAsync(cancellationToken);

        Assert.Single(allTags);
        var firstTag = allTags.Single();
        Assert.Equal("Test 1", firstTag.Name);

        tag = new TagFormModel { Id = firstTag.Id, Name = "Test 2", TagGroupId = tagGroup.Id };
        await tagManager.SaveTagAsync(tag, cancellationToken);
        
        allTags = await tagManager.GetAllTagsAsync(cancellationToken);
        Assert.Single(allTags);
        firstTag = allTags.Single();
        Assert.Equal("Test 2", firstTag.Name);

        await tagManager.DeleteTagAsync(firstTag, cancellationToken);
        allTags = await tagManager.GetAllTagsAsync(cancellationToken);
        Assert.Empty(allTags);
    }
}
