using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Models;
using System.Collections.Concurrent;

namespace DataMonitor.Core;

/// <summary>
/// 值监控器实现
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public sealed class ValueMonitor<T> : MonitorBase<T>, IValueMonitor<T> where T : struct
{
    private readonly ConcurrentDictionary<int, (T TargetValue, ValueMonitorCallback<T> Callback)> _monitors = new();

    public ValueMonitor(MonitorOptions options)
        : base(options)
    {
    }

    public override int Count => _monitors.Count;

    public override bool ContainsAddress(int address)
    {
        return _monitors.ContainsKey(address);
    }

    public bool Register(int address, T targetValue, ValueMonitorCallback<T> callback)
    {
        ValidateAddress(address);
        ArgumentNullException.ThrowIfNull(callback);

        return _monitors.TryAdd(address, (targetValue, callback));
    }

    public void RegisterOrUpdate(int address, T targetValue, ValueMonitorCallback<T> callback)
    {
        ValidateAddress(address);
        ArgumentNullException.ThrowIfNull(callback);

        _monitors[address] = (targetValue, callback);
    }

    public override bool RemoveMonitor(int address)
    {
        return _monitors.TryRemove(address, out _);
    }

    public override void Clear()
    {
        _monitors.Clear();
    }

    public async ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_monitors.IsEmpty)
            return false;

        var matchedCallbacks = new List<(ValueMonitorCallback<T> Callback, ValueMatchedEventArgs<T> Args)>();

        // 第一阶段：收集所有匹配的回调
        foreach (var kvp in _monitors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int address = kvp.Key;
            var (targetValue, callback) = kvp.Value;

            // 检查地址有效性
            if (!TryReadValue(data, address, out T currentValue))
                continue;

            if (EqualityComparer<T>.Default.Equals(currentValue, targetValue))
            {
                var args = new ValueMatchedEventArgs<T>
                {
                    Address = address,
                    Value = currentValue,
                };

                matchedCallbacks.Add((callback, args));
            }
        }

        if (matchedCallbacks.Count == 0)
            return false;

        // 第二阶段：并行执行所有回调
        await CallbackExecutor.ExecuteBatchAsync(
            matchedCallbacks.Select(m => (
                (Func<ValueMatchedEventArgs<T>, ValueTask>)(a => m.Callback(a)),
                m.Args,
                m.Args.Address
            ))
        ).ConfigureAwait(false);

        return true;
    }

    public async ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, int address, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        if (!_monitors.TryGetValue(address, out var monitor))
            return false;

        // 检查地址有效性
        if (!TryReadValue(data, address, out T currentValue))
            return false;

        if (!EqualityComparer<T>.Default.Equals(currentValue, monitor.TargetValue))
            return false;

        var args = new ValueMatchedEventArgs<T>
        {
            Address = address,
            Value = currentValue,
        };

        await CallbackExecutor.ExecuteAsync(
            a => monitor.Callback(a),
            args,
            address
        ).ConfigureAwait(false);

        return true;
    }
}
