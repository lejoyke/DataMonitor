using DataMonitor.Core;
using DataMonitor.Models;

namespace DataMonitor.Abstractions;

/// <summary>
/// 变化监控回调委托
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
/// <param name="args">事件参数</param>
/// <returns>异步任务</returns>
public delegate ValueTask ChangeMonitorCallback<T>(ValueChangedEventArgs<T> args) where T : struct;

/// <summary>
/// 变化监控接口，用于监控值的变化
/// </summary>
/// <typeparam name="T">监控的数据类型</typeparam>
public interface IChangeMonitor<T> : IMonitor<T> where T : struct
{
    public DataStateManager<T> StateManager { get; }

    /// <summary>
    /// 注册变化监控
    /// </summary>
    /// <param name="address">监控地址</param>
    /// <param name="callback">回调函数</param>
    /// <returns>如果成功注册则返回true</returns>
    bool Register(int address, ChangeMonitorCallback<T> callback);

    /// <summary>
    /// 注册或更新变化监控
    /// </summary>
    /// <param name="address">监控地址</param>
    /// <param name="callback">回调函数</param>
    void RegisterOrUpdate(int address, ChangeMonitorCallback<T> callback);

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

    /// <summary>
    /// 获取待确认变化的数量
    /// </summary>
    int GetPendingChangesCount();

    /// <summary>
    /// 检查指定地址是否有待确认的变化
    /// </summary>
    /// <param name="address">地址</param>
    /// <returns>如果有待确认变化则返回true</returns>
    bool HasPendingChange(int address);

    /// <summary>
    /// 清除指定地址的待确认变化
    /// </summary>
    /// <param name="address">地址</param>
    void ClearPendingChange(int address);

    /// <summary>
    /// 清除所有待确认变化
    /// </summary>
    void ClearAllPendingChanges();
}
