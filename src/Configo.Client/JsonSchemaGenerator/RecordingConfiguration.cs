using Microsoft.Extensions.Primitives;

namespace Configo.Client.JsonSchemaGenerator;

/// <summary>
/// Helper class that reveals when a configuration section was retrieved
/// </summary>
internal sealed class RecordingConfiguration : IConfiguration
{
    private readonly IConfiguration _inner;

    private readonly ISet<string> _recordedSections;

    public RecordingConfiguration(IConfiguration rootConfiguration)
    {
        _inner = rootConfiguration ?? throw new ArgumentNullException(nameof(rootConfiguration));
        _recordedSections = new HashSet<string>();
    }

    public void StartRecording()
    {
        _recordedSections.Clear();
    }

    public ISet<string> RecordedSections => _recordedSections;

    public void StopRecording()
    {
        _recordedSections.Clear();
    }
    
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        return _inner.GetChildren();
    }

    public IChangeToken GetReloadToken()
    {
        return _inner.GetReloadToken();
    }

    public IConfigurationSection GetSection(string key)
    {
        _recordedSections.Add(key);
        return _inner.GetSection(key);
    }

    public string? this[string key]
    {
        get => _inner[key];
        set => _inner[key] = value;
    }
}
