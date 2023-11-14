namespace Configo.Client.JsonSchemaGenerator;

/// <summary>
/// Tries to discover which configuration sections are bound to which options types
/// </summary>
internal sealed class BoundConfigurationSectionsDiscoverer
{
    private static readonly ISet<string> IgnoredSections = new HashSet<string>
    {
        "ConnectionStrings", "Kestrel", "AllowedHosts"
    };
    
    private static readonly string[] CommonSuffixes = {
        "Options", "Config", "Configuration", "Settings"
    };
    
    private readonly ILogger<BoundConfigurationSectionsDiscoverer> _logger;
    private readonly ServiceCollectionHolder _serviceCollectionHolder;
    private readonly IConfiguration _rootConfiguration;
    private readonly IServiceProvider _serviceProvider;

    public BoundConfigurationSectionsDiscoverer(
        ILogger<BoundConfigurationSectionsDiscoverer> logger,
        ServiceCollectionHolder serviceCollectionHolder,
        IConfiguration rootConfiguration, 
        IServiceProvider serviceProvider
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceCollectionHolder = serviceCollectionHolder ?? throw new ArgumentNullException(nameof(serviceCollectionHolder));
        _rootConfiguration = rootConfiguration ?? throw new ArgumentNullException(nameof(rootConfiguration));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
    
    /// <summary>
    /// Discovers bound configuration sections by creating a temporary new service collection
    /// and running all implementations of <see cref="IConfigureOptions{TOptions}"/> while recording which configuration sections are used
    /// </summary>
    public async Task<Dictionary<string, Type>> DiscoverAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var originalServices = _serviceCollectionHolder.Services;
        if (originalServices == null)
        {
            throw new ConfigoJsonSchemaGeneratorException("Original services are null, cannot proceed with automatic discovery of bound configurations");
        }

        var configureOptionsDescriptors = originalServices
            .Where(s => s.ServiceType.IsGenericType && s.ServiceType.GetGenericTypeDefinition() == typeof(IConfigureOptions<>))
            .ToList();
        
        var boundConfigurationSections = new Dictionary<string, Type>();
        var recordingConfiguration = new RecordingConfiguration(_rootConfiguration);
        var recordedBindings = new Dictionary<string, List<Type>>();
        var tempServices = new ServiceCollection();
        
        foreach (var serviceDescriptor in configureOptionsDescriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var optionsType = serviceDescriptor.ServiceType.GetGenericArguments()[0];

            var addOptionsMethod = typeof(OptionsServiceCollectionExtensions)
                .GetMethods()
                .Single(m => m is { Name: nameof(OptionsServiceCollectionExtensions.AddOptions), IsGenericMethod: true } && m.GetParameters().Length == 1)
                .MakeGenericMethod(optionsType);

            addOptionsMethod.Invoke(null, new object[] { tempServices });
        }

        tempServices.AddSingleton<IConfiguration>(recordingConfiguration);
        
        await using var tempServiceProvider = tempServices.BuildServiceProvider();

        foreach (var serviceDescriptor in configureOptionsDescriptors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var optionsType = serviceDescriptor.ServiceType.GetGenericArguments()[0];
            var wrapperType = typeof(IOptions<>).MakeGenericType(optionsType);
            var wrapperInstance = tempServiceProvider.GetRequiredService(wrapperType);
            var valueProperty = wrapperType.GetProperty(nameof(IOptions<ConfigoJsonSchemaGeneratorOptions>.Value));
            var options = valueProperty!.GetValue(wrapperInstance)!;
            
            _logger.LogTrace("Trying to discover which configuration section is bound to {OptionsType}", optionsType);
            
            recordingConfiguration.StartRecording();
            try
            {
                object instance;
                if (serviceDescriptor.ImplementationFactory != null)
                {
                    try
                    {
                        _logger.LogTrace("Invoking implementation factory for {OptionsType}", optionsType);
                        var factory = serviceDescriptor.ImplementationFactory!;
                        instance = factory.Invoke(tempServiceProvider);

                        _logger.LogTrace("Successfully invoked implementation factory for {OptionsType}", optionsType);
                    }
                    catch (Exception e)
                    {
                        _logger.LogTrace(e, "Failed to invoke implementation factory for options {OptionsType}", optionsType);
                        continue;
                    }
                }
                else if (serviceDescriptor.ImplementationInstance != null)
                {
                    instance = serviceDescriptor.ImplementationInstance!;

                    _logger.LogTrace("Using implementation instance for {OptionsType}: {ImplementationInstanceType}", optionsType, instance.GetType());
                }
                else
                {
                    try
                    {
                        _logger.LogTrace("Activating implementation type for {OptionsType}: {ImplementationType}", optionsType, serviceDescriptor.ImplementationType);

                        var creator = ActivatorUtilities.CreateFactory(serviceDescriptor.ImplementationType!, Array.Empty<Type>());

                        switch (serviceDescriptor.Lifetime)
                        {
                            case ServiceLifetime.Singleton:
                            case ServiceLifetime.Transient:
                                instance = creator(_serviceProvider, Array.Empty<object>());
                                break;
                            case ServiceLifetime.Scoped:
                            {
                                using var scope = _serviceProvider.CreateScope();
                                instance = creator(scope.ServiceProvider, Array.Empty<object>());
                                break;
                            }
                            default:
                                continue;
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogTrace(e, "Failed to activate implementation factory for options {OptionsType}", optionsType);
                        continue;
                    }

                    _logger.LogTrace("Successfully activated implementation type for {OptionsType}", optionsType);
                }

                var configureMethod = typeof(IConfigureOptions<>)
                    .MakeGenericType(optionsType)
                    .GetMethod(nameof(IConfigureOptions<ConfigoJsonSchemaGeneratorOptions>.Configure));

                var configureNamedMethod = typeof(IConfigureNamedOptions<>)
                    .MakeGenericType(optionsType)
                    .GetMethod(nameof(IConfigureNamedOptions<ConfigoJsonSchemaGeneratorOptions>.Configure), new[] { typeof(string), optionsType })!;

                try
                {
                    _logger.LogTrace("Invoking configure for {OptionsType}", optionsType);

                    if (instance.GetType().IsAssignableTo(typeof(IConfigureNamedOptions<>).MakeGenericType(optionsType)))
                    {
                        configureNamedMethod.Invoke(instance, new[] { Options.DefaultName, options });
                    }
                    else
                    {
                        configureMethod!.Invoke(instance, new[] { options });
                    }

                    _logger.LogTrace("Successfully invoked configure for {OptionsType}", optionsType);
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e, "Failed to invoke configure method for options {OptionsType}", optionsType);
                    continue;
                }

                var recordedSections = recordingConfiguration.RecordedSections;

                if (recordedSections.Count == 0)
                {
                    _logger.LogDebug("Did not record any configuration sections while configuring {OptionsType}", optionsType);
                    continue;
                }

                foreach (var recordedSection in recordedSections)
                {
                    if (!recordedBindings.TryGetValue(recordedSection, out var bindings))
                    {
                        bindings = new List<Type>();
                        recordedBindings.Add(recordedSection, bindings);
                    }

                    bindings.Add(optionsType);
                }
            }
            finally
            {
                recordingConfiguration.StopRecording();
            }
        }

        foreach (var recordedBinding in recordedBindings)
        {
            var section = recordedBinding.Key;
            var bindings = recordedBinding.Value;

            if (IgnoredSections.Contains(section))
            {
                continue;
            }

            if (bindings.Count == 1)
            {
                _logger.LogDebug("Section {Section} was only bound once, to {OptionsType}", section, bindings[0]);

                boundConfigurationSections.Add(section, bindings[0]);
                continue;
            }

            var exactMatch = bindings.FirstOrDefault(b => string.Equals(b.Name, section, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                _logger.LogDebug("Section {Section} was bound to multiple types, but {OptionsType} is an exact match", section, exactMatch);

                boundConfigurationSections.Add(section, exactMatch);
                continue;
            }

            var commonSuffix = bindings.FirstOrDefault(b => CommonSuffixes.Any(suffix => string.Equals(b.Name, section + suffix, StringComparison.OrdinalIgnoreCase)));
            if (commonSuffix != null)
            {
                _logger.LogDebug("Section {Section} was bound to multiple types, but {OptionsType} uses a common suffix", section, commonSuffix);
                boundConfigurationSections.Add(section, commonSuffix);
                continue;
            }

            var startsWith = bindings.Where(b => b.Name.StartsWith(section, StringComparison.OrdinalIgnoreCase)).ToList();
            if (startsWith.Count == 1)
            {
                _logger.LogDebug("Section {Section} was bound to multiple types, but {OptionsType} starts with the section name", section, startsWith[0]);
                boundConfigurationSections.Add(section, startsWith[0]);
                continue;
            }
            
            _logger.LogWarning("Configuration section {Section} was bound to multiple types, and none of them seem like a good match: {OptionsTypes}", section, 
                string.Join(", ", bindings.Select(b => b.Name)));
        }

        return boundConfigurationSections;
    }

}
