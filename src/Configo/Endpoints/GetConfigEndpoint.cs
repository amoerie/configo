using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Configo.Database.Migrations;
using Configo.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Configo.Endpoints;

public static class GetConfigEndpoint
{
    public static async Task<Results<Ok<JsonDocument>, NotFound>> HandleAsync(
        VariableManager variableManager,
        ClaimsIdentity claimsIdentity,
        CancellationToken cancellationToken)
    {
        var apiKeyId = int.Parse(claimsIdentity.FindFirst(ApiKeyAuthenticationHandler.ApiKeyIdClaim)!.Value);
        var variables = await variableManager.GetConfigAsync(apiKeyId, cancellationToken);
        var root = new JsonObject();

        
    }
}
