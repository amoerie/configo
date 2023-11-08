using System.Security.Claims;
using System.Text;
using Configo.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Configo.Endpoints;

public static class GetConfigEndpoint
{
    public static async Task<ContentHttpResult> HandleAsync(
        [FromServices] VariableManager variableManager,
        [FromServices] VariablesJsonSerializer variablesJsonSerializer,
        ClaimsPrincipal  claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var apiKeyId = int.Parse(claimsPrincipal.FindFirst(ApiKeyAuthenticationHandler.ApiKeyIdClaim)!.Value);
        var variables = await variableManager.GetConfigAsync(apiKeyId, cancellationToken);
        return TypedResults.Text(variables, "application/json", Encoding.UTF8);
    }
}
