using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class ManagingApplications : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;

    public ManagingApplications(IntegrationTestFixture fixture, ITestOutputHelper output)
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
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        CancellationToken cancellationToken = default;
        
        // Act + Assert
        var application = new ApplicationModel { Name = "Test 1",  };
        await applicationManager.SaveApplicationAsync(application, cancellationToken);
        var applications = await applicationManager.GetAllApplicationsAsync(cancellationToken);

        Assert.Single(applications);
        Assert.Equal("Test 1", applications.Single().Name);

        application.Name = "Test 2";
        await applicationManager.SaveApplicationAsync(application, cancellationToken);
        
        applications = await applicationManager.GetAllApplicationsAsync(cancellationToken);
        Assert.Single(applications);
        Assert.Equal("Test 2", applications.Single().Name);

        await applicationManager.DeleteApplicationAsync(application, cancellationToken);
        applications = await applicationManager.GetAllApplicationsAsync(cancellationToken);
        Assert.Empty(applications);
    }
}
