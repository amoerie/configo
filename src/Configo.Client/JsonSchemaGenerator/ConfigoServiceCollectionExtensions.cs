namespace Configo.Client.JsonSchemaGenerator;

/// <summary>
/// Extension methods for registering <see cref="ConfigoJsonSchemaGenerator"/> with <see cref="IServiceCollection"/>.
/// </summary>
public static class ConfigoServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Configo JSON schema generator
    /// This generator will run only for development environments, and produce an appsettings.schema.json file in the root of your project
    /// </summary>
    /// <param name="services">The services to add the generator to</param>
    /// <param name="configure">(Optional) a function that configures the <see cref="ConfigoJsonSchemaGeneratorOptions"/></param>
    /// <returns>The updated services</returns>
    public static IServiceCollection AddConfigoJsonSchemaGenerator(this IServiceCollection services, Action<ConfigoJsonSchemaGeneratorOptions>? configure = null)
    {
        var marker = ServiceDescriptor.Singleton<DefaultJsonSchemaProvider, DefaultJsonSchemaProvider>();
        if (services.Contains(marker))
        {
            return services;
        }
        
        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddLogging();
        services.Add(marker);
        services.AddSingleton(new ServiceCollectionHolder { Services = services });
        services.AddSingleton<BoundConfigurationSectionsDiscoverer>();
        services.AddSingleton<ConnectionStringsDiscoverer>();
        services.AddSingleton<ConfigoJsonSchemaGenerator>();
        
        // Startup
        services.AddHostedService<ConfigoJsonSchemaGeneratorService>();

        return services;
    }
}
