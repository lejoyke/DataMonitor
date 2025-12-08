namespace DataMonitor.Models;

/// <summary>
/// 变化检测结果
/// </summary>
public readonly record struct ChangeDetectionResult
{
    /// <summary>
    /// 是否应该触发回调
    /// </summary>
    public bool ShouldTrigger { get; init; }

    /// <summary>
    /// 是否处于待确认状态
    /// </summary>
    public bool IsPending { get; init; }

    /// <summary>
    /// 结果描述
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 创建一个触发回调的结果
    /// </summary>
    public static ChangeDetectionResult Trigger(string? reason = null)
        => new() { ShouldTrigger = true, Reason = reason };

    /// <summary>
    /// 创建一个待确认状态的结果
    /// </summary>
    public static ChangeDetectionResult Pending(string? reason = null)
        => new() { IsPending = true, Reason = reason };

    /// <summary>
    /// 创建一个无变化的结果
    /// </summary>
    public static ChangeDetectionResult NoChange(string? reason = null)
        => new() { Reason = reason };
}
