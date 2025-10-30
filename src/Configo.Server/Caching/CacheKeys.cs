namespace Configo.Server.Caching;

public static class CacheKeys
{
    public static string GetConfigByApiKey(int apiKeyId) => $"configo_get_config_by_apikey:{apiKeyId}";

}
