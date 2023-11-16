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
        var application = await applicationManager.SaveApplicationAsync(new ApplicationModel { Name = "Test 1" }, cancellationToken);
        var applications = await applicationManager.GetAllApplicationsAsync(cancellationToken);

        applications.Should().HaveCount(1);
        applications.Single().Name.Should().Be("Test 1");

        await applicationManager.SaveApplicationAsync(new ApplicationModel { Id = application.Id, Name = "Test 2" }, cancellationToken);
        
        applications = await applicationManager.GetAllApplicationsAsync(cancellationToken);
        applications.Should().HaveCount(1);
        applications.Single().Name.Should().Be("Test 2");

        await applicationManager.DeleteApplicationAsync(new ApplicationDeleteModel { Id = application.Id }, cancellationToken);
        applications = await applicationManager.GetAllApplicationsAsync(cancellationToken);
        applications.Should().HaveCount(0);
    }
}
