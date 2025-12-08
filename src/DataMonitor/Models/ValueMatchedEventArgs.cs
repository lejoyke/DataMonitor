namespace DataMonitor.Models;

/// <summary>
/// 值匹配事件参数
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public sealed record ValueMatchedEventArgs<T> : MonitorEventArgsBase<T> where T : struct
{
    /// <summary>
    /// 匹配的值
    /// </summary>
    public required T Value { get; init; }
}
