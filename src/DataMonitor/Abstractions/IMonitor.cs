using DataMonitor.Models;

namespace DataMonitor.Abstractions;

/// <summary>
/// 监控器基础接口
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public interface IMonitor<T> : IDisposable where T : struct
{
    /// <summary>
    /// 异常发生事件
    /// </summary>
    event EventHandler<MonitorExceptionEventArgs>? OnException;

    /// <summary>
    /// 检查是否包含指定地址的监控
    /// </summary>
    /// <param name="address">地址</param>
    /// <returns>如果包含则返回true</returns>
    bool ContainsAddress(int address);

    /// <summary>
    /// 移除指定地址的监控
    /// </summary>
    /// <param name="address">地址</param>
    /// <returns>如果成功移除则返回true</returns>
    bool RemoveMonitor(int address);

    /// <summary>
    /// 清除所有监控
    /// </summary>
    void Clear();

    /// <summary>
    /// 获取已注册的地址数量
    /// </summary>
    int Count { get; }
}
