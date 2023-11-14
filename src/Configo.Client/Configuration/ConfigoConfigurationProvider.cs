using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Configuration.Json;

namespace Configo.Client.Configuration;

/// <summary>
/// A Configo based <see cref="ConfigurationProvider"/>.
/// </summary>
internal sealed class ConfigoConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly TimeSpan? _reloadInterval;
    private readonly ConfigoClient _client;
    private Task? _pollingTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;
    private readonly JsonWriterOptions _jsonWriterOptions;
    private readonly ConfigoJsonStreamConfigurationProvider _configoJsonStreamConfigurationProvider;
    private readonly string? _cacheFileName;

    internal ConfigoConfigurationProvider(ConfigoClient client, TimeSpan? reloadInterval, string? cacheFileName)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _pollingTask = null;
        _cancellationTokenSource = new CancellationTokenSource();
        _reloadInterval = reloadInterval;
        _cacheFileName = cacheFileName;
        _jsonWriterOptions = new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = true
        };
        _configoJsonStreamConfigurationProvider = new ConfigoJsonStreamConfigurationProvider(new JsonStreamConfigurationSource());
    }

    ~ConfigoConfigurationProvider() => Dispose(false);

    /// <summary>
    /// Load secrets into this provider.
    /// </summary>
    public override void Load() => LoadAsync(_cancellationTokenSource.Token).GetAwaiter().GetResult();

    private async Task PollForChangesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await WaitForReload(cancellationToken).ConfigureAwait(false);
            try
            {
                await LoadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Ignore
            }
        }
    }

    private Task WaitForReload(CancellationToken cancellationToken)
    {
        // WaitForReload is only called when the _reloadInterval has a value.
        return Task.Delay(_reloadInterval!.Value, cancellationToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        // Step 1: fetch JSON document from Configo
        var json = await _client.GetConfigAsync(cancellationToken);
        
        // Step 2: serialize to memory stream
        await using var memoryStream = new MemoryStream();
        await using (var jsonWriter = new Utf8JsonWriter(memoryStream, _jsonWriterOptions))
        {
            json.WriteTo(jsonWriter);
        }

        // Step 3: save to appsettings.configo.json as a caching layer for when configo goes offline
        if (_cacheFileName != null)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            await using var fileStream = File.OpenWrite(_cacheFileName);
            await memoryStream.CopyToAsync(fileStream, cancellationToken);
            fileStream.SetLength(memoryStream.Length);
        }

        // Step 4: Add configuration to Data
        memoryStream.Seek(0, SeekOrigin.Begin);
        _configoJsonStreamConfigurationProvider.Load(memoryStream);
        Data = _configoJsonStreamConfigurationProvider.Parsed;

        // Step 5: Schedule a polling task only if none exists and a valid delay is specified
        if (_pollingTask == null && _reloadInterval != null)
        {
            _pollingTask = PollForChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Frees resources held by the <see cref="ConfigoConfigurationProvider"/> object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Frees resources held by the <see cref="ConfigoConfigurationProvider"/> object.
    /// </summary>
    /// <param name="disposing">true if called from <see cref="Dispose()"/>, otherwise false.</param>
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_disposed)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _client.Dispose();
            }

            _disposed = true;
        }
    }
}
