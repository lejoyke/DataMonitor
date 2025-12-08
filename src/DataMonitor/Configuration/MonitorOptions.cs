using Microsoft.Extensions.DependencyInjection;

namespace DataMonitor.Configuration;

/// <summary>
/// 监控器配置选项
/// </summary>
public sealed class MonitorOptions
{
    /// <summary>
    /// 是否启用滤波
    /// </summary>
    public bool EnableFiltering { get; set; }

    /// <summary>
    /// 滤波确认次数（默认2次）
    /// </summary>
    public int FilterConfirmationCount { get; set; } = 2;

    /// <summary>
    /// 并行执行回调
    /// </summary>
    public bool ParallelCallbackExecution { get; set; } = true;

    /// <summary>
    /// 首次接收数据时是否触发回调（无历史数据时）
    /// </summary>
    public bool TriggerOnFirstData { get; set; } = false;

    /// <summary>
    /// 监控器服务生命周期
    /// </summary>
    public ServiceLifetime MonitorLifetime { get; set; } = ServiceLifetime.Singleton;

    /// <summary>
    /// 锁等待超时
    /// </summary>
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// 默认配置实例 - 不启用滤波
    /// </summary>
    public static MonitorOptions Default => new();

    /// <summary>
    /// 启用滤波的配置实例
    /// </summary>
    public static MonitorOptions WithFiltering => new() { EnableFiltering = true };

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public void Validate()
    {
        if (FilterConfirmationCount < 1)
            throw new ArgumentOutOfRangeException(nameof(FilterConfirmationCount), "Must be at least 1");

        if (LockTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(LockTimeout), "Must be positive");
    }
}
