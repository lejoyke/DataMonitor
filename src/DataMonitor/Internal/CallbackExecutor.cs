using DataMonitor.Configuration;
using DataMonitor.Models;

namespace DataMonitor.Internal;

/// <summary>
/// 回调执行器，统一处理回调的执行和异常处理
/// </summary>
public sealed class CallbackExecutor
{
    private readonly MonitorOptions _options;
    private readonly Action<int, Exception>? _exceptionHandler;

    public CallbackExecutor(MonitorOptions options, Action<int, Exception>? exceptionHandler = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _exceptionHandler = exceptionHandler;
    }

    /// <summary>
    /// 执行回调
    /// </summary>
    /// <typeparam name="TArgs">参数类型</typeparam>
    /// <param name="callback">回调函数</param>
    /// <param name="args">参数</param>
    /// <param name="address">地址（用于异常报告）</param>
    public async ValueTask ExecuteAsync<TArgs>(Func<TArgs, ValueTask> callback, TArgs args, int address)
    {
        try
        {
            // 同步执行
            await callback(args).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _exceptionHandler?.Invoke(address, ex);
        }
    }

    /// <summary>
    /// 批量执行回调
    /// </summary>
    public async ValueTask ExecuteBatchAsync<TArgs>(
        IEnumerable<(Func<TArgs, ValueTask> Callback, TArgs Args, int Address)> items)
    {
        if (_options.ParallelCallbackExecution)
        {
            // 并行执行所有回调
            await Task.WhenAll(
                items.Select(item => ExecuteAsync(item.Callback, item.Args, item.Address).AsTask())
            ).ConfigureAwait(false);
        }
        else
        {
            // 顺序执行
            foreach (var item in items)
            {
                await ExecuteAsync(item.Callback, item.Args, item.Address).ConfigureAwait(false);
            }
        }
    }
}
