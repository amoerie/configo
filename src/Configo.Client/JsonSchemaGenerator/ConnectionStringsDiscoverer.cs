namespace Configo.Client.JsonSchemaGenerator;

/// <summary>
/// Tries to discover which configuration sections are bound to which options types
/// </summary>
internal sealed class ConnectionStringsDiscoverer
{
    private readonly ILogger<ConnectionStringsDiscoverer> _logger;
    private readonly IConfiguration _rootConfiguration;

    public ConnectionStringsDiscoverer(
        ILogger<ConnectionStringsDiscoverer> logger,
        IConfiguration rootConfiguration)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rootConfiguration = rootConfiguration ?? throw new ArgumentNullException(nameof(rootConfiguration));
    }
    
    /// <summary>
    /// Discovers connection strings
    /// </summary>
    public Task<List<string>> DiscoverAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_rootConfiguration.GetSection("ConnectionStrings")
            .GetChildren()
            .Where(c => c.Value != null)
            .Select(c => c.Key)
            .Where(key => !string.IsNullOrEmpty(key))
            .ToList());
    }

}
