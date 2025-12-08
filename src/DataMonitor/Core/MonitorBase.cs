using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Internal;
using DataMonitor.Models;

namespace DataMonitor.Core;

/// <summary>
/// 监控器基类，提供通用的监控功能
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public abstract class MonitorBase<T> : IMonitor<T> where T : struct
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    protected readonly MonitorOptions Options;
    protected readonly CallbackExecutor CallbackExecutor;
    private bool _disposed;

    public event EventHandler<MonitorExceptionEventArgs>? OnException;

    public abstract int Count { get; }

    protected MonitorBase(MonitorOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
        options.Validate();

        CallbackExecutor = new CallbackExecutor(options, RaiseException);
    }

    public abstract bool ContainsAddress(int address);

    public abstract bool RemoveMonitor(int address);

    public abstract void Clear();

    /// <summary>
    /// 获取读锁
    /// </summary>
    protected IDisposable AcquireReadLock()
    {
        ThrowIfDisposed();

        if (!_lock.TryEnterReadLock(Options.LockTimeout))
            throw new TimeoutException("Failed to acquire read lock");

        return new LockReleaser(() => _lock.ExitReadLock());
    }

    /// <summary>
    /// 获取写锁
    /// </summary>
    protected IDisposable AcquireWriteLock()
    {
        ThrowIfDisposed();

        if (!_lock.TryEnterWriteLock(Options.LockTimeout))
            throw new TimeoutException("Failed to acquire write lock");

        return new LockReleaser(() => _lock.ExitWriteLock());
    }

    /// <summary>
    /// 验证地址有效性
    /// </summary>
    protected static void ValidateAddress(int address)
    {
        if (address < 0)
            throw new ArgumentOutOfRangeException(nameof(address), "Address must be non-negative");
    }

    /// <summary>
    /// Attempts to read a value from the provided memory without exposing spans in async callers.
    /// </summary>
    protected static bool TryReadValue(ReadOnlyMemory<T> source, int address, out T value)
    {
        var span = source.Span;
        if (address < 0 || address >= span.Length)
        {
            value = default;
            return false;
        }

        value = span[address];
        return true;
    }

    /// <summary>
    /// 克隆数据
    /// </summary>
    protected static ReadOnlyMemory<T> CloneData(ReadOnlyMemory<T> source)
    {
        var copy = new T[source.Length];
        source.CopyTo(copy);
        return copy;
    }

    /// <summary>
    /// 触发异常事件
    /// </summary>
    protected void RaiseException(int address, Exception exception)
    {
        OnException?.Invoke(this, new MonitorExceptionEventArgs(address, exception));
    }

    /// <summary>
    /// 检查是否已释放
    /// </summary>
    protected void ThrowIfDisposed()
    {
        DisposableGuard.ThrowIfDisposed(_disposed, this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _lock.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
