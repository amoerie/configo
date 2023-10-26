using Microsoft.AspNetCore.Components;

namespace Configo.Blazor;

/// <summary>
/// Custom base component to support cancellation tokens
/// </summary>
public class ConfigoComponentBase: ComponentBase, IDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;

    protected CancellationToken CancellationToken => (_cancellationTokenSource ??= new CancellationTokenSource()).Token;

    public virtual void Dispose()
    {
        if (_cancellationTokenSource == null) return;
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }
}
