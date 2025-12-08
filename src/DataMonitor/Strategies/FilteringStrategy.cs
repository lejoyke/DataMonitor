using DataMonitor.Abstractions;
using DataMonitor.Models;
using System.Collections.Concurrent;

namespace DataMonitor.Strategies;

/// <summary>
/// 滤波策略，需要连续检测到相同变化才触发回调
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public sealed class FilteringStrategy<T> : IChangeDetectionStrategy<T> where T : struct
{
    private readonly ConcurrentDictionary<int, PendingChangeInfo> _pendingChanges = new();
    private readonly int _confirmationCount;

    /// <summary>
    /// 待确认变化信息
    /// </summary>
    private sealed class PendingChangeInfo
    {
        public T PendingNewValue { get; set; }
        public T OriginalOldValue { get; set; }
        public int ConfirmCount { get; set; }
    }

    /// <summary>
    /// 创建滤波策略
    /// </summary>
    /// <param name="confirmationCount">确认次数（默认2次）</param>
    public FilteringStrategy(int confirmationCount = 2)
    {
        if (confirmationCount < 1)
            throw new ArgumentOutOfRangeException(nameof(confirmationCount), "Must be at least 1");

        _confirmationCount = confirmationCount;
    }

    public ChangeDetectionResult Evaluate(int address, T oldValue, T newValue)
    {
        // 检查是否有待确认的变化
        if (_pendingChanges.TryGetValue(address, out var pendingChange))
        {
            if (EqualityComparer<T>.Default.Equals(newValue, pendingChange.PendingNewValue))
            {
                // 确认变化：当前值等于待定的新值
                pendingChange.ConfirmCount++;

                if (pendingChange.ConfirmCount >= _confirmationCount)
                {
                    _pendingChanges.TryRemove(address, out _);
                    return ChangeDetectionResult.Trigger($"Change confirmed after {_confirmationCount} consecutive detections");
                }

                return ChangeDetectionResult.Pending($"Pending confirmation ({pendingChange.ConfirmCount}/{_confirmationCount})");
            }
            else if (EqualityComparer<T>.Default.Equals(newValue, pendingChange.OriginalOldValue))
            {
                // 变化被撤销：当前值回到了原始值
                _pendingChanges.TryRemove(address, out _);
                return ChangeDetectionResult.NoChange("Change reverted to original value");
            }
            else
            {
                // 变化到了第三个值，更新待定变化
                pendingChange.PendingNewValue = newValue;
                pendingChange.ConfirmCount = 1;
                return ChangeDetectionResult.Pending("New pending value detected");
            }
        }
        else if (!EqualityComparer<T>.Default.Equals(newValue, oldValue))
        {
            // 检测到新的变化，添加到待定列表
            var newPending = new PendingChangeInfo
            {
                PendingNewValue = newValue,
                OriginalOldValue = oldValue,
                ConfirmCount = 1
            };

            _pendingChanges.TryAdd(address, newPending);

            if (_confirmationCount == 1)
            {
                _pendingChanges.TryRemove(address, out _);
                return ChangeDetectionResult.Trigger("Change detected (single confirmation mode)");
            }

            return ChangeDetectionResult.Pending($"Initial change detected, awaiting confirmation (1/{_confirmationCount})");
        }

        return ChangeDetectionResult.NoChange("No change detected");
    }

    public void Reset(int address)
    {
        _pendingChanges.TryRemove(address, out _);
    }

    public void Clear()
    {
        _pendingChanges.Clear();
    }

    public int GetPendingChangesCount() => _pendingChanges.Count;

    public bool HasPendingChange(int address) => _pendingChanges.ContainsKey(address);

    public int[] GetPendingAddresses() => [.. _pendingChanges.Keys];
}
