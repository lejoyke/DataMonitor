using DataMonitor.Abstractions;
using DataMonitor.Models;

namespace DataMonitor.Strategies;

/// <summary>
/// 无滤波策略，所有变化都立即触发回调
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public sealed class NoFilterStrategy<T> : IChangeDetectionStrategy<T> where T : struct
{
    public ChangeDetectionResult Evaluate(int address, T oldValue, T newValue)
    {
        bool hasChanged = !EqualityComparer<T>.Default.Equals(oldValue, newValue);

        return hasChanged
            ? ChangeDetectionResult.Trigger("Value changed")
            : ChangeDetectionResult.NoChange("No change detected");
    }

    public void Reset(int address)
    {
        // 无状态，无需重置
    }

    public void Clear()
    {
        // 无状态，无需清理
    }

    public int GetPendingChangesCount() => 0;

    public bool HasPendingChange(int address) => false;

    public int[] GetPendingAddresses() => [];
}
