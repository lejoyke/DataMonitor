using DataMonitor.Models;

namespace DataMonitor.Abstractions;

/// <summary>
/// 变化检测策略接口
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public interface IChangeDetectionStrategy<T> where T : struct
{
    /// <summary>
    /// 评估是否应该触发变化通知
    /// </summary>
    /// <param name="address">地址</param>
    /// <param name="oldValue">旧值</param>
    /// <param name="newValue">新值</param>
    /// <returns>变化检测结果</returns>
    ChangeDetectionResult Evaluate(int address, T oldValue, T newValue);

    /// <summary>
    /// 重置指定地址的策略状态
    /// </summary>
    /// <param name="address">地址</param>
    void Reset(int address);

    /// <summary>
    /// 清除所有策略状态
    /// </summary>
    void Clear();

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
    /// 获取所有待确认变化的地址
    /// </summary>
    /// <returns>地址数组</returns>
    int[] GetPendingAddresses();
}
