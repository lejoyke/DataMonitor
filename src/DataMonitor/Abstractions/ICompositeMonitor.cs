using DataMonitor.Models;

namespace DataMonitor.Abstractions;

/// <summary>
/// 组合监控器接口，整合值监控和变化监控功能
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public interface ICompositeMonitor<T> : IDisposable where T : struct
{
    /// <summary>
    /// 异常发生事件
    /// </summary>
    event EventHandler<MonitorExceptionEventArgs>? OnException;

    /// <summary>
    /// 值监控器
    /// </summary>
    IValueMonitor<T> ValueMonitor { get; }

    /// <summary>
    /// 变化监控器
    /// </summary>
    IChangeMonitor<T> ChangeMonitor { get; }

    /// <summary>
    /// 注册值监控
    /// </summary>
    bool RegisterValueMonitor(int address, T targetValue, ValueMonitorCallback<T> callback);

    /// <summary>
    /// 注册变化监控
    /// </summary>
    bool RegisterChangeMonitor(int address, ChangeMonitorCallback<T> callback);

    /// <summary>
    /// 检查是否包含指定地址的监控
    /// </summary>
    bool ContainsAddress(int address);

    /// <summary>
    /// 移除指定地址的所有监控
    /// </summary>
    bool RemoveMonitor(int address);

    /// <summary>
    /// 清除所有监控
    /// </summary>
    void Clear();

    /// <summary>
    /// 检查数据（并行触发所有匹配的回调）
    /// </summary>
    /// <param name="data">泛型数据数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查指定地址的数据
    /// </summary>
    /// <param name="data">泛型数据数组</param>
    /// <param name="address">指定地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, int address, CancellationToken cancellationToken = default);
}
