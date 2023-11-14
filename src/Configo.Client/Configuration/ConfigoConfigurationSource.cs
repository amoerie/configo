// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Configo.Client.Configuration;

/// <summary>
/// Represents Configo configuration as an <see cref="IConfigurationSource"/>.
/// </summary>
internal sealed class ConfigoConfigurationSource : IConfigurationSource
{
    private readonly ConfigoClient _client;
    private readonly TimeSpan? _reloadInterval;
    private readonly string? _cacheFileName;

    public ConfigoConfigurationSource(ConfigoClient client, TimeSpan? reloadInterval, string? cacheFileName)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _reloadInterval = reloadInterval;
        _cacheFileName = cacheFileName;
    }

    /// <inheritdoc />
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ConfigoConfigurationProvider(_client, _reloadInterval, _cacheFileName);
    }
}
