namespace DataMonitor.Models;

/// <summary>
/// 值变化事件参数
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public sealed record ValueChangedEventArgs<T> : MonitorEventArgsBase<T> where T : struct
{
    /// <summary>
    /// 旧值
    /// </summary>
    public required T OldValue { get; init; }

    /// <summary>
    /// 新值
    /// </summary>
    public required T NewValue { get; init; }
}
