using Microsoft.Extensions.Caching.Hybrid;

namespace Configo.Server.Caching;

public sealed class CacheManager(HybridCache hybridCache)
{
    public async Task ExpireAllConfigAsync(CancellationToken cancellationToken) 
        => await hybridCache.RemoveByTagAsync(CacheTags.Config, cancellationToken);
    
    public async Task ExpireConfigByApiKeyIdAsync(int apiKeyId, CancellationToken cancellationToken) 
        => await hybridCache.RemoveByTagAsync(CacheTags.ConfigByApiKey(apiKeyId), cancellationToken);
}
