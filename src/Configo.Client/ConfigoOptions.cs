namespace Configo.Client;

/// <summary>
/// Configures how to use Configo as a configuration source
/// </summary>
public sealed record ConfigoOptions
{
    /// <summary>
    /// The Configo url
    /// </summary>
    public required string? Url { get; set; }

    /// <summary>
    /// The Configo API key to use for authentication
    /// </summary>
    public required string? ApiKey { get; set; }
    
    /// <summary>
    /// Gets or sets the timespan to wait between attempts at polling Configo for changes. <code>null</code> to disable reloading.
    /// </summary>
    public TimeSpan? ReloadInterval { get; set; }

    /// <summary>
    /// Gets or sets the file name where configuration retrieved from Configo will be cached.
    /// This allows Configo to be offline from time to time, without hindering application startup.
    /// </summary>
    public string? CacheFileName { get; set; } = "appsettings.configo.json";
}
