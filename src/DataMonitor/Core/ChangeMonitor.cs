using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Internal;
using DataMonitor.Models;
using System.Collections.Concurrent;

namespace DataMonitor.Core;

/// <summary>
/// 变化监控器实现
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public sealed class ChangeMonitor<T> : MonitorBase<T>, IChangeMonitor<T> where T : struct
{
    private readonly ConcurrentDictionary<int, ChangeMonitorCallback<T>> _monitors = new();
    private readonly IChangeDetectionStrategy<T> _strategy;
    private readonly DataStateManager<T> _stateManager = new();

    public DataStateManager<T> StateManager => _stateManager;

    public ChangeMonitor(
        IChangeDetectionStrategy<T> strategy,
        MonitorOptions options)
        : base(options)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public override int Count => _monitors.Count;

    public override bool ContainsAddress(int address)
    {
        return _monitors.ContainsKey(address);
    }

    public bool Register(int address, ChangeMonitorCallback<T> callback)
    {
        ValidateAddress(address);
        ArgumentNullException.ThrowIfNull(callback);

        return _monitors.TryAdd(address, callback);
    }

    public void RegisterOrUpdate(int address, ChangeMonitorCallback<T> callback)
    {
        ValidateAddress(address);
        ArgumentNullException.ThrowIfNull(callback);

        _monitors[address] = callback;
    }

    public override bool RemoveMonitor(int address)
    {
        var removed = _monitors.TryRemove(address, out _);
        if (removed)
        {
            _strategy.Reset(address);
        }
        return removed;
    }

    public override void Clear()
    {
        _monitors.Clear();
        _strategy.Clear();
        _stateManager.Clear();
    }

    public async ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_monitors.IsEmpty)
        {
            _stateManager.UpdateState(data);
            return false;
        }

        // 如果没有历史数据且配置为不在首次触发，直接更新状态并返回
        if (!_stateManager.HasPreviousData && !Options.TriggerOnFirstData)
        {
            _stateManager.UpdateState(data);
            return false;
        }

        var changedCallbacks = new List<(ChangeMonitorCallback<T> Callback, ValueChangedEventArgs<T> Args)>();
        bool isFirstData = !_stateManager.HasPreviousData;

        // 第一阶段：收集所有需要触发的回调
        foreach (var kvp in _monitors)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int address = kvp.Key;
            var callback = kvp.Value;

            // 检查地址有效性
            if (!TryReadValue(data, address, out T currentValue))
                continue;

            T oldValue;
            bool shouldTrigger;

            if (isFirstData)
            {
                // 首次数据，使用默认值作为旧值，不经过策略评估
                oldValue = default;
                shouldTrigger = true;
            }
            else
            {
                // 获取旧值
                if (!_stateManager.TryGetPreviousValue(address, out oldValue))
                    continue;

                // 使用策略评估是否应该触发
                var result = _strategy.Evaluate(address, oldValue, currentValue);
                shouldTrigger = result.ShouldTrigger;
            }

            if (shouldTrigger)
            {
                var args = new ValueChangedEventArgs<T>
                {
                    Address = address,
                    OldValue = oldValue,
                    NewValue = currentValue,
                };

                changedCallbacks.Add((callback, args));
            }
        }

        _stateManager.UpdateState(data);

        if (changedCallbacks.Count == 0)
            return false;

        // 第二阶段：批量执行所有回调
        await CallbackExecutor.ExecuteBatchAsync(
            changedCallbacks.Select(m => (
                (Func<ValueChangedEventArgs<T>, ValueTask>)(a => m.Callback(a)),
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

        if (!_monitors.TryGetValue(address, out var callback))
        {
            _stateManager.UpdateState(data);
            return false;
        }

        // 检查地址有效性
        if (!TryReadValue(data, address, out T currentValue))
        {
            _stateManager.UpdateState(data);
            return false;
        }

        bool isFirstData = !_stateManager.HasPreviousData;
        T oldValue;
        bool shouldTrigger;

        if (isFirstData)
        {
            // 如果没有历史数据且配置为不在首次触发，直接更新状态并返回
            if (!Options.TriggerOnFirstData)
            {
                _stateManager.UpdateState(data);
                return false;
            }

            // 首次数据，使用默认值作为旧值
            oldValue = default;
            shouldTrigger = true;
        }
        else
        {
            // 获取旧值
            if (!_stateManager.TryGetPreviousValue(address, out oldValue))
            {
                _stateManager.UpdateState(data);
                return false;
            }

            // 使用策略评估是否应该触发
            var result = _strategy.Evaluate(address, oldValue, currentValue);
            shouldTrigger = result.ShouldTrigger;
        }

        _stateManager.UpdateState(data);

        if (!shouldTrigger)
            return false;

        var args = new ValueChangedEventArgs<T>
        {
            Address = address,
            OldValue = oldValue,
            NewValue = currentValue,
        };

        await CallbackExecutor.ExecuteAsync(
            a => callback(a),
            args,
            address
        ).ConfigureAwait(false);

        return true;
    }

    public int GetPendingChangesCount() => _strategy.GetPendingChangesCount();

    public bool HasPendingChange(int address) => _strategy.HasPendingChange(address);

    public void ClearPendingChange(int address) => _strategy.Reset(address);

    public void ClearAllPendingChanges() => _strategy.Clear();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stateManager.Dispose();
        }
        base.Dispose(disposing);
    }
}
