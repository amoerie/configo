namespace Configo.Server.Caching;

public static class CacheTags
{
    public const string Config = "config";
    public static string ConfigByApiKey(int apiKeyId) => $"config:{apiKeyId}";
}
