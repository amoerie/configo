using System.Security.Claims;
using System.Text;
using Configo.Server.Caching;
using Configo.Server.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;

namespace Configo.Server.Endpoints;

public static class GetConfigEndpoint
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
    };

    public static async Task<ContentHttpResult> HandleAsync(
        [FromServices] VariableManager variableManager,
        [FromServices] VariablesJsonSerializer variablesJsonSerializer,
        [FromServices] HybridCache hybridCache,
        ClaimsPrincipal  claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var apiKeyId = int.Parse(claimsPrincipal.FindFirst(ApiKeyAuthenticationHandler.ApiKeyIdClaim)!.Value);
        var cacheKey = CacheKeys.GetConfigByApiKey(apiKeyId);
        var cacheState = new CacheState(variableManager, apiKeyId);
        var variables = await hybridCache.GetOrCreateAsync(
            cacheKey, 
            cacheState,
            static async (state, innerCancellationToken) => await state.VariableManager.GetMergedConfigAsync(state.ApiKeyId, innerCancellationToken),
            options: CacheOptions,
            tags: [ CacheTags.Config, CacheTags.ConfigByApiKey(apiKeyId) ],
            cancellationToken: cancellationToken);
        return TypedResults.Text(variables, "application/json", Encoding.UTF8);
    }

    private readonly record struct CacheState(VariableManager VariableManager, int ApiKeyId);
}
