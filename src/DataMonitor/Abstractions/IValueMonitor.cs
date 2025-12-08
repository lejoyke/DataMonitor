using DataMonitor.Models;

namespace DataMonitor.Abstractions;

/// <summary>
/// 值监控回调委托
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
/// <param name="args">事件参数</param>
/// <returns>异步任务</returns>
public delegate ValueTask ValueMonitorCallback<T>(ValueMatchedEventArgs<T> args) where T : struct;

/// <summary>
/// 值监控接口，用于监控特定值的出现
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public interface IValueMonitor<T> : IMonitor<T> where T : struct
{
    /// <summary>
    /// 注册值监控
    /// </summary>
    /// <param name="address">监控地址</param>
    /// <param name="targetValue">目标值</param>
    /// <param name="callback">回调函数</param>
    /// <returns>如果成功注册则返回true</returns>
    bool Register(int address, T targetValue, ValueMonitorCallback<T> callback);

    /// <summary>
    /// 注册或更新值监控
    /// </summary>
    /// <param name="address">监控地址</param>
    /// <param name="targetValue">目标值</param>
    /// <param name="callback">回调函数</param>
    void RegisterOrUpdate(int address, T targetValue, ValueMonitorCallback<T> callback);

    /// <summary>
    /// 检查数据（并行触发所有匹配的回调）
    /// </summary>
    /// <param name="data">泛型数据数组</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果触发了任何监控则返回true</returns>
    ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查指定地址的数据
    /// </summary>
    /// <param name="data">泛型数据数组</param>
    /// <param name="address">指定地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>如果触发了监控则返回true</returns>
    ValueTask<bool> CheckAsync(ReadOnlyMemory<T> data, int address, CancellationToken cancellationToken = default);
}
