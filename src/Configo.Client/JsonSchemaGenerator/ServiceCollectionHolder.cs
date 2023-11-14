namespace Configo.Client.JsonSchemaGenerator;

/// <summary>
/// Helper type to hold the original service descriptors
/// These are only used to automatically discover bound configuration sections
/// To avoid memory leaks, the services are cleared after discovery
/// </summary>
internal sealed record ServiceCollectionHolder
{
    public IServiceCollection? Services { get; set; }
}
