using System.Net.Http.Headers;
using System.Text.Json;

namespace Configo.Client.Configuration;

internal sealed class ConfigoClient: IDisposable
{
    private readonly string _apiKey;
    private readonly bool _leaveOpen;
    private readonly HttpClient _httpClient;

    internal ConfigoClient(Uri uri, string apiKey)
    {
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient
        {
            BaseAddress = uri ?? throw new ArgumentNullException(nameof(uri))
        };
        _leaveOpen = false;
    }

    internal ConfigoClient(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _leaveOpen = true;
    }

    public async Task<JsonDocument> GetConfigAsync(CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "api/config")
        {
            Headers =
            {
                Authorization = new AuthenticationHeaderValue("Bearer", _apiKey)
            }
        };

        Stream body;
        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            body = await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (HttpRequestException e)
        {
            throw new ConfigoConfigurationException("Sending HTTP request to Configo failed", e);
        }

        try
        {
            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };
            var json = await JsonDocument.ParseAsync(body, options, cancellationToken);
            return json ?? throw new ConfigoConfigurationException("Parsing HTTP response from Configo to JSON failed");
        }
        catch (JsonException e)
        {
            throw new ConfigoConfigurationException("Parsing HTTP response from Configo to JSON failed", e);
        }
    }

    public void Dispose()
    {
        if (!_leaveOpen)
        {
            _httpClient.Dispose();
        }
    }
}
