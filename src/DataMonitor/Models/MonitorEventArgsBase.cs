namespace DataMonitor.Models;

/// <summary>
/// 监控事件参数基类
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public abstract record MonitorEventArgsBase<T> where T : struct
{
    /// <summary>
    /// 监控地址
    /// </summary>
    public required int Address { get; init; }

    /// <summary>
    /// 事件时间戳
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
