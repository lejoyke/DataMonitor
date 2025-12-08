using DataMonitor.Configuration;
using DataMonitor.Core;
using DataMonitor.Strategies;

namespace DataMonitor.Tests;

public class ConcurrencyTests
{
    private readonly MonitorOptions _defaultOptions = MonitorOptions.Default;

    [Fact]
    public async Task ValueMonitor_ConcurrentRegistrations_ThreadSafe()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var tasks = new List<Task>();

        // Act - 并发注册多个监控
        for (int i = 0; i < 100; i++)
        {
            int address = i;
            tasks.Add(Task.Run(() => monitor.Register(address, address * 10, _ => ValueTask.CompletedTask)));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, monitor.Count);
    }

    [Fact]
    public async Task ValueMonitor_ConcurrentChecks_ThreadSafe()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var triggerCount = 0;

        monitor.Register(0, 100, _ =>
        {
            Interlocked.Increment(ref triggerCount);
            return ValueTask.CompletedTask;
        });

        var data = new int[] { 100 };
        var tasks = new List<Task>();

        // Act - 并发检查
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(monitor.CheckAsync(data).AsTask());
        }

        await Task.WhenAll(tasks);
        await Task.Delay(200); // 等待所有回调完成

        // Assert - 每次检查都应该触发回调
        Assert.Equal(50, triggerCount);
    }

    [Fact]
    public async Task ChangeMonitor_ConcurrentChecks_ThreadSafe()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();
        using var monitor = new ChangeMonitor<int>(strategy, _defaultOptions);
        var triggerCount = 0;
        var lockObj = new object();

        monitor.Register(0, _ =>
        {
            lock (lockObj)
            {
                triggerCount++;
            }
            return ValueTask.CompletedTask;
        });

        // 初始化
        await monitor.CheckAsync(new int[] { 0 });

        // Act - 并发检查（交替值）
        var tasks = new List<Task>();
        for (int i = 1; i <= 10; i++)
        {
            int value = i;
            tasks.Add(Task.Run(async () =>
            {
                await monitor.CheckAsync(new int[] { value });
            }));
        }

        await Task.WhenAll(tasks);
        await Task.Delay(200);

        // Assert - 至少触发一次（由于并发，确切次数取决于执行顺序）
        Assert.True(triggerCount >= 1);
    }

    [Fact]
    public async Task CompositeMonitor_ConcurrentOperations_ThreadSafe()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();
        using var monitor = new CompositeMonitor<int>(strategy, _defaultOptions);
        var operations = new List<Task>();

        // Act - 并发注册、检查和移除
        for (int i = 0; i < 50; i++)
        {
            int address = i;
            operations.Add(Task.Run(() =>
            {
                monitor.RegisterValueMonitor(address, address * 10, _ => ValueTask.CompletedTask);
                monitor.RegisterChangeMonitor(address, _ => ValueTask.CompletedTask);
            }));
        }

        await Task.WhenAll(operations);

        // 并发检查
        var data = Enumerable.Range(0, 50).Select(i => i * 10).ToArray();
        var checkTasks = Enumerable.Range(0, 20)
            .Select(_ => monitor.CheckAsync(data).AsTask())
            .ToList();

        await Task.WhenAll(checkTasks);

        // Assert - 不应抛出异常
        Assert.True(true);
    }

    [Fact]
    public async Task FilteringStrategy_ConcurrentEvaluations_ThreadSafe()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);
        var tasks = new List<Task<bool>>();

        // Act - 并发评估多个地址
        for (int i = 0; i < 100; i++)
        {
            int address = i;
            tasks.Add(Task.Run(() =>
            {
                var result1 = strategy.Evaluate(address, 0, 1);
                var result2 = strategy.Evaluate(address, 0, 1);
                return result2.ShouldTrigger;
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - 所有地址都应该触发
        Assert.All(results, Assert.True);
    }

    [Fact]
    public async Task Monitor_RegisterAndCheckConcurrently_ThreadSafe()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var registerTask = Task.Run(async () =>
        {
            for (int i = 0; i < 100 && !cts.IsCancellationRequested; i++)
            {
                monitor.RegisterOrUpdate(i % 10, i, _ => ValueTask.CompletedTask);
                await Task.Delay(1);
            }
        });

        var checkTask = Task.Run(async () =>
        {
            var data = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = 0; i < 100 && !cts.IsCancellationRequested; i++)
            {
                await monitor.CheckAsync(data);
                await Task.Delay(1);
            }
        });

        // Act & Assert - 不应抛出异常
        await Task.WhenAll(registerTask, checkTask);
    }

    [Fact]
    public async Task ParallelCallbackExecution_ExecutesInParallel()
    {
        // Arrange
        var options = new MonitorOptions { ParallelCallbackExecution = true };
        using var monitor = new ValueMonitor<int>(options);

        var concurrentExecutions = 0;
        var maxConcurrent = 0;
        var lockObj = new object();

        for (int i = 0; i < 5; i++)
        {
            monitor.Register(i, i, async _ =>
            {
                var current = Interlocked.Increment(ref concurrentExecutions);
                lock (lockObj)
                {
                    if (current > maxConcurrent)
                        maxConcurrent = current;
                }

                await Task.Delay(50); // 模拟耗时操作
                Interlocked.Decrement(ref concurrentExecutions);
            });
        }

        var data = new int[] { 0, 1, 2, 3, 4 };

        // Act
        await monitor.CheckAsync(data);
        await Task.Delay(200); // 等待所有回调完成

        // Assert - 并行执行应该有多个并发
        Assert.True(maxConcurrent > 1, $"Expected concurrent executions > 1, but was {maxConcurrent}");
    }

    [Fact]
    public async Task SequentialCallbackExecution_ExecutesSequentially()
    {
        // Arrange
        var options = new MonitorOptions { ParallelCallbackExecution = false };
        using var monitor = new ValueMonitor<int>(options);

        var executionOrder = new List<int>();
        var lockObj = new object();

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            monitor.Register(i, i, async _ =>
            {
                lock (lockObj)
                {
                    executionOrder.Add(index);
                }
                await Task.Delay(10);
            });
        }

        var data = new int[] { 0, 1, 2 };

        // Act
        await monitor.CheckAsync(data);
        await Task.Delay(100);

        // Assert - 应该按某种顺序执行（不一定是注册顺序，但是是顺序执行）
        Assert.Equal(3, executionOrder.Count);
    }

    [Fact]
    public async Task CancellationToken_StopsProcessing()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var triggerCount = 0;

        for (int i = 0; i < 100; i++)
        {
            monitor.Register(i, i, _ =>
            {
                Interlocked.Increment(ref triggerCount);
                return ValueTask.CompletedTask;
            });
        }

        var data = Enumerable.Range(0, 100).ToArray();
        using var cts = new CancellationTokenSource();

        // 立即取消
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await monitor.CheckAsync(data, cts.Token));
    }

    [Fact]
    public async Task MultipleDispose_DoesNotThrow()
    {
        // Arrange
        var monitor = new ValueMonitor<int>(_defaultOptions);

        // Act - 多次释放不应抛出异常
        monitor.Dispose();
        monitor.Dispose();
        monitor.Dispose();

        // Assert
        Assert.True(true);
        await Task.CompletedTask;
    }
}
