namespace Configo.Client.JsonSchemaGenerator;

internal sealed class DefaultJsonSchemaProvider
{
    private const string ResourceName = "Configo.Client.JsonSchemaGenerator.default.schema.json";

    public async Task<JsonSchema> ProvideAsync(CancellationToken cancellationToken)
    {
        try
        {
            var assembly = typeof(DefaultJsonSchemaProvider).Assembly;
            await using var stream = assembly.GetManifestResourceStream(ResourceName)
                                     ?? throw new ConfigoJsonSchemaGeneratorException("Missing embedded resource");
            return await JsonSchema.FromJsonAsync(stream, cancellationToken);
        }
        catch (Exception e)
        {
            throw new ConfigoJsonSchemaGeneratorException("Failed to load the default schema", e);
        }
    }
}
