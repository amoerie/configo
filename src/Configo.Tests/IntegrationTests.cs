using Configo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Configo.Tests;

public class IntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    internal IntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    [Fact]
    public async Task ShouldReturnCorrectVariables()
    {
        // Arrange
        var tagGroupManager = _factory.Services.GetRequiredService<TagGroupManager>();
        var tagManager = _factory.Services.GetRequiredService<TagManager>();
        var applicationManager = _factory.Services.GetRequiredService<ApplicationManager>();
        var apiKeyManager = _factory.Services.GetRequiredService<ApiKeyManager>();
        var variableManager = _factory.Services.GetRequiredService<VariableManager>();
        var cancellationToken = CancellationToken.None;

        // Act
        var group1 = new TagGroupEditModel { Name = "Group 1" };
        var group2 = new TagGroupEditModel { Name = "Group 2" };
        var group3 = new TagGroupEditModel { Name = "Group 3" };
        var group4 = new TagGroupEditModel { Name = "Group 4" };
        var savedGroup1 = await tagGroupManager.SaveTagGroupAsync(group1, cancellationToken);
        var savedGroup2 = await tagGroupManager.SaveTagGroupAsync(group2, cancellationToken);
        var savedGroup3 = await tagGroupManager.SaveTagGroupAsync(group3, cancellationToken);
        var savedGroup4 = await tagGroupManager.SaveTagGroupAsync(group4, cancellationToken);
        var tag1 = new TagEditModel { Name = "Tag 1", TagGroupId = savedGroup1.Id };
        var tag2 = new TagEditModel { Name = "Tag 2", TagGroupId = savedGroup2.Id };
        var tag3 = new TagEditModel { Name = "Tag 3", TagGroupId = savedGroup3.Id };
        var tag4 = new TagEditModel { Name = "Tag 4", TagGroupId = savedGroup4.Id };
        var savedTag1 = await tagManager.SaveTagAsync(tag1, cancellationToken);
        var savedTag2 = await tagManager.SaveTagAsync(tag2, cancellationToken);
        var savedTag3 = await tagManager.SaveTagAsync(tag3, cancellationToken);
        var savedTag4 = await tagManager.SaveTagAsync(tag4, cancellationToken);
        var app1 = new ApplicationEditModel { Name = "App 1" };
        var app2 = new ApplicationEditModel { Name = "App 2" };
        var savedApp1 = await applicationManager.SaveApplicationAsync(app1, cancellationToken);
        var savedApp2 = await applicationManager.SaveApplicationAsync(app2, cancellationToken);
        var apiKey1 = new ApiKeyEditModel
        {
            ApplicationId = savedApp1.Id,
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
            TagIds = new List<int> { savedTag1.Id, savedTag2.Id }
        };
        var apiKey2 = new ApiKeyEditModel
        {
            ApplicationId = savedApp2.Id,
            ActiveSinceUtc = DateTime.UtcNow,
            ActiveUntilUtc = DateTime.UtcNow.AddMonths(1),
            TagIds = new List<int> { savedTag2.Id, savedTag3.Id }
        };
        var savedApiKey1 = await apiKeyManager.SaveApiKeyAsync(apiKey1, cancellationToken);
        var savedApiKey2 = await apiKeyManager.SaveApiKeyAsync(apiKey2, cancellationToken);

        var tag1Config = """"
                         {
                             "Foo":
                             {
                                 "Bar": "Test"
                             }
                         }
                         """";
        var tag2Config = """"
                         {
                             "Foo":
                             {
                                 "Bar2": "Test2"
                             }
                         }
                         """";
        
        await variableManager.SaveVariablesAsync()


        // Assert
    }
}
