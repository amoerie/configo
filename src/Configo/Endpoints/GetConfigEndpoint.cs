using System.Security.Claims;
using System.Text;
using Configo.Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Configo.Endpoints;

public static class GetConfigEndpoint
{
    public static async Task<ContentHttpResult> HandleAsync(
        VariableManager variableManager,
        VariablesJsonSerializer variablesJsonSerializer,
        ClaimsIdentity claimsIdentity,
        CancellationToken cancellationToken)
    {
        var apiKeyId = int.Parse(claimsIdentity.FindFirst(ApiKeyAuthenticationHandler.ApiKeyIdClaim)!.Value);
        var variables = await variableManager.GetConfigAsync(apiKeyId, cancellationToken);
        return TypedResults.Text(variables, "application/json", Encoding.UTF8);
    }
}
