namespace DataMonitor.Models;

/// <summary>
/// 监控异常事件参数
/// </summary>
public sealed class MonitorExceptionEventArgs : EventArgs
{
    /// <summary>
    /// 发生异常的地址
    /// </summary>
    public int Address { get; }

    /// <summary>
    /// 发生的异常
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// 异常发生时间
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    public MonitorExceptionEventArgs(int address, Exception exception)
    {
        Address = address;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        Timestamp = DateTimeOffset.UtcNow;
    }
}
