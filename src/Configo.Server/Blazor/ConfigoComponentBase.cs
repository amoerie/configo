using Microsoft.AspNetCore.Components;

namespace Configo.Server.Blazor;

/// <summary>
/// Custom base component to support cancellation tokens
/// </summary>
public class ConfigoComponentBase: ComponentBase, IAsyncDisposable
{
    private CancellationTokenSource? _cancellationTokenSource;

    protected CancellationToken CancellationToken => (_cancellationTokenSource ??= new CancellationTokenSource()).Token;

    protected virtual ValueTask OnDisposeAsync()
    {
        return default;
    }

    public async ValueTask DisposeAsync()
    {
        if (_cancellationTokenSource == null) return;
        await OnDisposeAsync();
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }
}
