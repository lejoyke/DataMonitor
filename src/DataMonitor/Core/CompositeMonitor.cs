using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Internal;
using DataMonitor.Models;

namespace DataMonitor.Core;

/// <summary>
/// 组合监控器，整合值监控和变化监控
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public sealed class CompositeMonitor<T> : ICompositeMonitor<T> where T : struct
{
    private readonly ValueMonitor<T> _valueMonitor;
    private readonly ChangeMonitor<T> _changeMonitor;
    private bool _disposed;

    public event EventHandler<MonitorExceptionEventArgs>? OnException;

    public IValueMonitor<T> ValueMonitor => _valueMonitor;
    public IChangeMonitor<T> ChangeMonitor => _changeMonitor;

    public CompositeMonitor(
        IChangeDetectionStrategy<T> strategy,
        MonitorOptions options)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        ArgumentNullException.ThrowIfNull(options);

        _valueMonitor = new ValueMonitor<T>(options);
        _changeMonitor = new ChangeMonitor<T>(strategy, options);

        // 转发异常事件
        _valueMonitor.OnException += (sender, args) => OnException?.Invoke(sender, args);
        _changeMonitor.OnException += (sender, args) => OnException?.Invoke(sender, args);
    }

    public bool RegisterValueMonitor(int address, T targetValue, ValueMonitorCallback<T> callback)
    {
        ThrowIfDisposed();
        return _valueMonitor.Register(address, targetValue, callback);
    }

    public bool RegisterChangeMonitor(int address, ChangeMonitorCallback<T> callback)
    {
        ThrowIfDisposed();
        return _changeMonitor.Register(address, callback);
    }

    public bool ContainsAddress(int address)
    {
        ThrowIfDisposed();
        return _valueMonitor.ContainsAddress(address) || _changeMonitor.ContainsAddress(address);
    }

    public bool RemoveMonitor(int address)
    {
        ThrowIfDisposed();
        var valueRemoved = _valueMonitor.RemoveMonitor(address);
        var changeRemoved = _changeMonitor.RemoveMonitor(address);
        return valueRemoved || changeRemoved;
    }

    public void Clear()
    {
        ThrowIfDisposed();
        _valueMonitor.Clear();
        _changeMonitor.Clear();
    }

    public async ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var valueTask = _valueMonitor.CheckAsync(data, cancellationToken);
        var changeTask = _changeMonitor.CheckAsync(data, cancellationToken);

        var valueResult = await valueTask.ConfigureAwait(false);
        var changeResult = await changeTask.ConfigureAwait(false);

        return valueResult || changeResult;
    }

    public async ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, int address, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var valueTask = _valueMonitor.CheckAsync(data, address, cancellationToken);
        var changeTask = _changeMonitor.CheckAsync(data, address, cancellationToken);

        var valueResult = await valueTask.ConfigureAwait(false);
        var changeResult = await changeTask.ConfigureAwait(false);

        return valueResult || changeResult;
    }

    private void ThrowIfDisposed()
    {
        DisposableGuard.ThrowIfDisposed(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _valueMonitor.Dispose();
        _changeMonitor.Dispose();
    }
}
