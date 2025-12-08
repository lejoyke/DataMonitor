using DataMonitor.Internal;

namespace DataMonitor.Core;

/// <summary>
/// 数据状态管理器，线程安全地管理历史数据
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public sealed class DataStateManager<T> : IDisposable where T : struct
{
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
    private T[]? _previousData;
    private bool _disposed;

    /// <summary>
    /// 获取上一次的数据副本
    /// </summary>
    public T[]? GetPreviousData()
    {
        ThrowIfDisposed();

        _lock.EnterReadLock();
        try
        {
            return _previousData?.ToArray();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 尝试获取上一次数据的指定位置值
    /// </summary>
    public bool TryGetPreviousValue(int address, out T value)
    {
        ThrowIfDisposed();
        value = default;

        _lock.EnterReadLock();
        try
        {
            if (_previousData is null || address < 0 || address >= _previousData.Length)
                return false;

            value = _previousData[address];
            return true;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// 更新数据状态
    /// </summary>
    public void UpdateState(ReadOnlyMemory<T> newData)
    {
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            if (_previousData is null || _previousData.Length != newData.Length)
            {
                _previousData = new T[newData.Length];
            }

            newData.Span.CopyTo(_previousData);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 清除状态
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();

        _lock.EnterWriteLock();
        try
        {
            _previousData = null;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// 检查是否有历史数据
    /// </summary>
    public bool HasPreviousData
    {
        get
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                return _previousData is not null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        DisposableGuard.ThrowIfDisposed(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lock.Dispose();
    }
}
