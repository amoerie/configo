using Configo.Server.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Configo.Server.Endpoints;

public sealed record SaveSchemaRequest(string Schema);

public static class SaveSchemaEndpoint
{
    public static async Task<Ok> HandleAsync(
        [FromServices] SchemaManager schemaManager,
        [FromRoute] int applicationId,
        [FromBody] SaveSchemaRequest request,
        CancellationToken cancellationToken)
    {
        await schemaManager.SaveSchemaAsync(applicationId, request.Schema, cancellationToken);
        return TypedResults.Ok();
    }
}
