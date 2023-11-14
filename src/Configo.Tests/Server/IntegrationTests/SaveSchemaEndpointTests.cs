using System.Net.Http.Json;
using Configo.Server.Domain;
using Xunit.Abstractions;

namespace Configo.Tests.Server.IntegrationTests;

[Collection(IntegrationTestFixture.Collection)]
public class SaveSchemaEndpointTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private ApplicationListModel _processor = default!;

    public SaveSchemaEndpointTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        _fixture.SetOutput(output);
    }

    public async Task InitializeAsync()
    {
        var applicationManager = _fixture.GetRequiredService<ApplicationManager>();
        var cancellationToken = CancellationToken.None;

        var processorModel = new ApplicationEditModel { Name = "Processor" };
        _processor = await applicationManager.SaveApplicationAsync(processorModel, cancellationToken);
    }

    public Task DisposeAsync()
    {
        return _fixture.ResetAsync();
    }

    [Fact]
    public async Task HappyCase()
    {
        // Arrange
        var schemaManager = _fixture.GetRequiredService<SchemaManager>();
        using var httpClient = _fixture.CreateClient();
        var cancellationToken = default(CancellationToken);

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
