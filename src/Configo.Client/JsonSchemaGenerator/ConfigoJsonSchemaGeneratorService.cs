using System.Text;
using Newtonsoft.Json;

namespace Configo.Client.JsonSchemaGenerator;

internal sealed class ConfigoJsonSchemaGeneratorService : IHostedService
{
    private readonly ILogger<ConfigoJsonSchemaGeneratorService> _logger;
    private readonly IOptions<ConfigoJsonSchemaGeneratorOptions> _options;
    private readonly IHostEnvironment _environment;
    private readonly ConfigoJsonSchemaGenerator _generator;
    private readonly ServiceCollectionHolder _serviceCollectionHolder;

    public ConfigoJsonSchemaGeneratorService(
        ILogger<ConfigoJsonSchemaGeneratorService> logger,
        IOptions<ConfigoJsonSchemaGeneratorOptions> options,
        IHostEnvironment environment,
        ConfigoJsonSchemaGenerator generator,
        ServiceCollectionHolder serviceCollectionHolder)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _generator = generator ?? throw new ArgumentNullException(nameof(generator));
        _serviceCollectionHolder = serviceCollectionHolder ?? throw new ArgumentNullException(nameof(serviceCollectionHolder));
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = _options.Value;
            if (options.OnlyInEnvironment != null && !string.Equals(options.OnlyInEnvironment, _environment.EnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Not generating JSON schema because the current environment {CurrentEnvironment} is not {OnlyInEnvironment}",
                    _environment.EnvironmentName, options.OnlyInEnvironment);
                return;
            }

            var baseDirectory = new DirectoryInfo(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory));
            FileInfo? outputFile = null;
            if (options.OutputFile != null)
            {
                outputFile = new FileInfo(Path.GetFullPath(Path.Combine(baseDirectory.FullName, options.OutputFile)));
            }
            else
            {
                DirectoryInfo? directory = baseDirectory;
                var enumerationOptions = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false };
                while (directory != null && !directory.EnumerateFiles("*.csproj", enumerationOptions).Any())
                {
                    directory = directory.Parent;
                }

                if (directory != null)
                {
                    outputFile = new FileInfo(Path.GetFullPath(Path.Combine(directory.FullName, "appsettings.schema.json")));
                }
            }

            if (outputFile == null)
            {
                _logger.LogInformation("Not generating JSON schema because a .csproj file could not be found upwards from {BaseDirectory}" +
                                       " and the {OutputFile} option is not specified", baseDirectory, nameof(ConfigoJsonSchemaGeneratorOptions.OutputFile));
                return;
            }

            _logger.LogInformation("Generating JSON schema because the current environment is {CurrentEnvironment}", _environment.EnvironmentName);

            var jsonSchema = await _generator.GenerateAsync(cancellationToken);

            _logger.LogInformation("Saving JSON schema to {OutputFile}", outputFile);
            var jsonSchemaAsString = jsonSchema.ToJson(Formatting.Indented);
            await File.WriteAllTextAsync(outputFile.FullName, jsonSchemaAsString, Encoding.UTF8, cancellationToken);
        }
        finally
        {
            _serviceCollectionHolder.Services = null;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
