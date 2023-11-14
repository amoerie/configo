namespace Configo.Client.JsonSchemaGenerator;

/// <summary>
/// Configures the <see cref="ConfigoJsonSchemaGenerator"/>
/// </summary>
public sealed record ConfigoJsonSchemaGeneratorOptions
{
    /// <summary>
    /// Gets or sets the environment in which the JSON schema will be generated
    /// Defaults to Development, so as not to introduce any startup performance overhead in production
    /// Set to null to always generate the JSON schema
    /// </summary>
    public string? OnlyInEnvironment { get; set; } = Environments.Development;

    /// <summary>
    /// Gets or sets whether bound configuration sections should be discovered automatically
    /// </summary>
    public bool AutoDiscoverBoundConfigurationSections { get; set; } = true;
    
    /// <summary>
    /// Gets or sets additional bound configuration sections.
    /// The key should be the configuration section name and the value should be the type parameter of <see cref="IConfigureOptions{TOptions}"/>
    /// Any matching configuration sections that were also discovered automatically will be overridden 
    /// </summary>
    public Dictionary<string, Type>? AdditionalBoundConfigurationSections { get; set; } = null;

    /// <summary>
    /// Gets or sets the location where the generated schema should be written to
    /// Paths are relative to the base directory of the current app domain
    /// If no output file is specified, an output file called "appsettings.schema.json" will be placed in the first directory that contains a .csproj file when going upward from the base directory
    /// </summary>
    public string? OutputFile { get; set; }
}
