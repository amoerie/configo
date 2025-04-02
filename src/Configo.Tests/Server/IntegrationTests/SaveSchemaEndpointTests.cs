using System.Net.Http.Headers;
using System.Net.Http.Json;
using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class SaveSchemaEndpointTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private ApplicationModel _processor = null!;

    public SaveSchemaEndpointTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _fixture.SetOutput(output);
    }

    public async Task InitializeAsync()
    {
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        var cancellationToken = CancellationToken.None;

        _processor = new ApplicationModel { Name = "Processor" };
        await applicationManager.SaveApplicationAsync(_processor, cancellationToken);
    }

    public Task DisposeAsync()
    {
        return _fixture.ResetAsync();
    }

    [Fact]
    public async Task HappyCase()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var schemaManager = _fixture.GetRequiredService<SchemaManager>();
        
        using var httpClient = _fixture.CreateClient();

        // Act
        var schema = await File.ReadAllTextAsync("./Server/IntegrationTests/SaveSchemaEndpointTests.schema.json", cancellationToken);
        var body = new { Schema = schema };
        var response = await httpClient.PostAsJsonAsync($"/api/applications/{_processor.Id}/schema", body, cancellationToken);
        response.EnsureSuccessStatusCode();

        // Assert
        var schemaInDatabase = await schemaManager.GetSchemaAsync(_processor.Id, cancellationToken);
        Assert.Equal(
            JsonNormalizer.Normalize(schemaInDatabase),
            JsonNormalizer.Normalize(schema)
        );
    }
}
