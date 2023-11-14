namespace Configo.Client.Configuration;

/// <summary>
/// Extension methods for registering <see cref="ConfigoConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class ConfigoConfigurationExtensions
{
    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from Configo
    /// This method will make an intermediate build of the configuration so far, and look for the "Configo" configuration section
    /// If there is at least a Configo:Url and Configo:ApiKey that is not empty, Configo will be used
    /// To avoid the intermediate build, use one of the overloads that accepts an URL and API key directly
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to</param>
    /// <returns>The <see cref="IConfigurationBuilder"/></returns>
    public static IConfigurationBuilder AddConfigo(this IConfigurationBuilder configurationBuilder)
    {
        // Intermediate build of the configuration to retrieve the URL and API key
        var configuration = configurationBuilder.Build();
        var configoSection = configuration.GetSection("Configo");
        var configoOptions = configoSection.Get<ConfigoOptions>();
        if (configoOptions == null)
        {
            return configurationBuilder;
        }
        var url = configoOptions.Url;
        var apiKey = configoOptions.ApiKey;
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(apiKey))
        {
            return configurationBuilder;
        }
        return AddConfigo(configurationBuilder, configoOptions);
    }
    
    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from Configo
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to</param>
    /// <param name="options">The options that configure using Configo as a configuration source</param>
    /// <returns>The <see cref="IConfigurationBuilder"/></returns>
    public static IConfigurationBuilder AddConfigo(this IConfigurationBuilder configurationBuilder, ConfigoOptions options)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Url == null)
        {
            throw new ArgumentException($"{nameof(ConfigoOptions.Url)} is not configured", nameof(ConfigoOptions.Url));
        }

        if (options.ApiKey == null)
        {
            throw new ArgumentException($"{nameof(ConfigoOptions.ApiKey)} is not configured", nameof(ConfigoOptions.ApiKey));
        }

        if (!Uri.TryCreate(options.Url, UriKind.Absolute, out var url))
        {
            throw new ArgumentException($"{options.Url} is not a valid, absolute URI", nameof(options.Url));
        }
        
        return configurationBuilder.AddConfigo(new ConfigoClient(url, options.ApiKey), options.ReloadInterval, options.CacheFileName);
    }

    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from Configo
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to</param>
    /// <param name="httpClient">The HTTP client that will be used to make network requests</param>
    /// <param name="options">The Configo configuration options</param>
    /// <returns>The <see cref="IConfigurationBuilder"/></returns>
    public static IConfigurationBuilder AddConfigo(
        this IConfigurationBuilder configurationBuilder,
        HttpClient httpClient,
        ConfigoOptions options)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ApiKey == null)
        {
            throw new ArgumentException($"{nameof(ConfigoOptions.ApiKey)} is not configured", nameof(ConfigoOptions.ApiKey));
        }
        
        return configurationBuilder.AddConfigo(new ConfigoClient(httpClient, options.ApiKey), options.ReloadInterval, options.CacheFileName);
    }

    private static IConfigurationBuilder AddConfigo(
        this IConfigurationBuilder configurationBuilder,
        ConfigoClient client,
        TimeSpan? reloadInterval,
        string? cacheFileName)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        ArgumentNullException.ThrowIfNull(client);

        if (cacheFileName != null && File.Exists(cacheFileName))
        {
            configurationBuilder.AddJsonFile(cacheFileName, optional: false, reloadOnChange: false);
        }
        
        if(reloadInterval != null && reloadInterval.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(reloadInterval), reloadInterval, nameof(reloadInterval) + " must be positive.");
        }
        
        configurationBuilder.Add(new ConfigoConfigurationSource(client, reloadInterval, cacheFileName));

        return configurationBuilder;
    }
}
