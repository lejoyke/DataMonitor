namespace DataMonitor.Internal;

/// <summary>
/// 锁释放器，用于 using 模式释放锁
/// </summary>
internal sealed class LockReleaser : IDisposable
{
    private readonly Action _releaseAction;
    private bool _disposed;

    public LockReleaser(Action releaseAction)
    {
        _releaseAction = releaseAction ?? throw new ArgumentNullException(nameof(releaseAction));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _releaseAction();
    }
}
