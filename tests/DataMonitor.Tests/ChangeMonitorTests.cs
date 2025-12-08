using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Core;
using DataMonitor.Models;
using DataMonitor.Strategies;

namespace DataMonitor.Tests;

public class ChangeMonitorTests
{
    private readonly MonitorOptions _defaultOptions = MonitorOptions.Default;

    private ChangeMonitor<T> CreateMonitor<T>(MonitorOptions? options = null) where T : struct
    {
        var strategy = new NoFilterStrategy<T>();
        return new ChangeMonitor<T>(strategy, options ?? _defaultOptions);
    }

    [Fact]
    public void Register_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();

        // Act
        var result = monitor.Register(0, _ => ValueTask.CompletedTask);

        // Assert
        Assert.True(result);
        Assert.Equal(1, monitor.Count);
        Assert.True(monitor.ContainsAddress(0));
    }

    [Fact]
    public void Register_DuplicateAddress_ReturnsFalse()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.Register(0, _ => ValueTask.CompletedTask);

        // Act
        var result = monitor.Register(0, _ => ValueTask.CompletedTask);

        // Assert
        Assert.False(result);
        Assert.Equal(1, monitor.Count);
    }

    [Fact]
    public void RegisterOrUpdate_ExistingAddress_UpdatesMonitor()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.Register(0, _ => ValueTask.CompletedTask);

        // Act
        monitor.RegisterOrUpdate(0, _ => ValueTask.CompletedTask);

        // Assert
        Assert.Equal(1, monitor.Count);
    }

    [Fact]
    public void Register_NegativeAddress_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            monitor.Register(-1, _ => ValueTask.CompletedTask));
    }

    [Fact]
    public void Register_NullCallback_ThrowsArgumentNullException()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            monitor.Register(0, null!));
    }

    [Fact]
    public void RemoveMonitor_ExistingAddress_ReturnsTrue()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.Register(0, _ => ValueTask.CompletedTask);

        // Act
        var result = monitor.RemoveMonitor(0);

        // Assert
        Assert.True(result);
        Assert.Equal(0, monitor.Count);
    }

    [Fact]
    public void Clear_RemovesAllMonitors()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.Register(0, _ => ValueTask.CompletedTask);
        monitor.Register(1, _ => ValueTask.CompletedTask);

        // Act
        monitor.Clear();

        // Assert
        Assert.Equal(0, monitor.Count);
    }

    [Fact]
    public async Task CheckAsync_FirstCall_InitializesStateOnly()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var triggered = false;

        monitor.Register(0, _ =>
        {
            triggered = true;
            return ValueTask.CompletedTask;
        });

        var data = new int[] { 100 };

        // Act - 第一次调用只初始化状态
        var result = await monitor.CheckAsync(data);

        // Assert
        Assert.False(result);
        Assert.False(triggered);
    }

    [Fact]
    public async Task CheckAsync_ValueChanged_TriggersCallback()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var triggered = false;
        int? oldValue = null;
        int? newValue = null;

        monitor.Register(0, args =>
        {
            triggered = true;
            oldValue = args.OldValue;
            newValue = args.NewValue;
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 100 });

        // Act - 值变化
        var result = await monitor.CheckAsync(new int[] { 200 });

        // Assert
        await Task.Delay(50);
        Assert.True(result);
        Assert.True(triggered);
        Assert.Equal(100, oldValue);
        Assert.Equal(200, newValue);
    }

    [Fact]
    public async Task CheckAsync_ValueUnchanged_DoesNotTriggerCallback()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var triggerCount = 0;

        monitor.Register(0, _ =>
        {
            Interlocked.Increment(ref triggerCount);
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 100 });

        // Act - 值未变化
        await monitor.CheckAsync(new int[] { 100 });

        // Assert
        await Task.Delay(50);
        Assert.Equal(0, triggerCount);
    }

    [Fact]
    public async Task CheckAsync_MultipleChanges_TriggersAllCallbacks()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var triggeredAddresses = new List<int>();
        var lockObj = new object();

        monitor.Register(0, args =>
        {
            lock (lockObj) { triggeredAddresses.Add(args.Address); }
            return ValueTask.CompletedTask;
        });

        monitor.Register(1, args =>
        {
            lock (lockObj) { triggeredAddresses.Add(args.Address); }
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 10, 20 });

        // Act - 两个值都变化
        await monitor.CheckAsync(new int[] { 11, 21 });

        // Assert
        await Task.Delay(100);
        Assert.Equal(2, triggeredAddresses.Count);
        Assert.Contains(0, triggeredAddresses);
        Assert.Contains(1, triggeredAddresses);
    }

    [Fact]
    public async Task CheckAsync_PartialChange_TriggersOnlyChangedAddresses()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var triggeredAddresses = new List<int>();
        var lockObj = new object();

        monitor.Register(0, args =>
        {
            lock (lockObj) { triggeredAddresses.Add(args.Address); }
            return ValueTask.CompletedTask;
        });

        monitor.Register(1, args =>
        {
            lock (lockObj) { triggeredAddresses.Add(args.Address); }
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 10, 20 });

        // Act - 只有地址0变化
        await monitor.CheckAsync(new int[] { 11, 20 });

        // Assert
        await Task.Delay(100);
        Assert.Single(triggeredAddresses);
        Assert.Contains(0, triggeredAddresses);
    }

    [Fact]
    public async Task CheckAsync_WithSpecificAddress_OnlyChecksSpecifiedAddress()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var triggerCount = 0;

        monitor.Register(0, _ =>
        {
            Interlocked.Increment(ref triggerCount);
            return ValueTask.CompletedTask;
        });

        monitor.Register(1, _ =>
        {
            Interlocked.Increment(ref triggerCount);
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 10, 20 }, 0);
        await monitor.CheckAsync(new int[] { 10, 20 }, 1);

        // Act - 只检查地址0
        await monitor.CheckAsync(new int[] { 11, 21 }, 0);

        // Assert
        await Task.Delay(50);
        Assert.Equal(1, triggerCount);
    }

    [Fact]
    public async Task CheckAsync_BoolType_WorksCorrectly()
    {
        // Arrange
        using var monitor = CreateMonitor<bool>();
        var triggered = false;

        monitor.Register(0, _ =>
        {
            triggered = true;
            return ValueTask.CompletedTask;
        });

        // Act
        await monitor.CheckAsync(new bool[] { false });
        await monitor.CheckAsync(new bool[] { true });

        // Assert
        await Task.Delay(50);
        Assert.True(triggered);
    }

    [Fact]
    public async Task CheckAsync_WithFilteringStrategy_RequiresConfirmation()
    {
        // Arrange
        var options = new MonitorOptions
        {
            EnableFiltering = true,
            FilterConfirmationCount = 2
        };
        var strategy = new FilteringStrategy<int>(options.FilterConfirmationCount);
        using var monitor = new ChangeMonitor<int>(strategy, options);

        var triggerCount = 0;

        monitor.Register(0, _ =>
        {
            Interlocked.Increment(ref triggerCount);
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 100 });

        // Act - 第一次变化（待确认）
        await monitor.CheckAsync(new int[] { 200 });
        await Task.Delay(50);
        Assert.Equal(0, triggerCount);

        // Act - 第二次确认（触发）
        await monitor.CheckAsync(new int[] { 200 });
        await Task.Delay(50);
        Assert.Equal(1, triggerCount);
    }

    [Fact]
    public async Task CheckAsync_WithFilteringStrategy_ResetOnDifferentValue()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(2);
        using var monitor = new ChangeMonitor<int>(strategy, _defaultOptions);

        var triggerCount = 0;

        monitor.Register(0, _ =>
        {
            Interlocked.Increment(ref triggerCount);
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 100 });

        // Act - 第一次变化到200（待确认）
        await monitor.CheckAsync(new int[] { 200 });

        // Act - 变化到300（重置待确认）
        await monitor.CheckAsync(new int[] { 300 });

        // Assert - 没有触发
        await Task.Delay(50);
        Assert.Equal(0, triggerCount);
    }

    [Fact]
    public void GetPendingChangesCount_ReturnsCorrectCount()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(2);
        using var monitor = new ChangeMonitor<int>(strategy, _defaultOptions);

        // Assert
        Assert.Equal(0, monitor.GetPendingChangesCount());
    }

    [Fact]
    public async Task Dispose_PreventsSubsequentOperations()
    {
        // Arrange
        var monitor = CreateMonitor<int>();
        monitor.Dispose();

        // Act & Assert - CheckAsync 应该抛出异常
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await monitor.CheckAsync(new int[] { 100 }));
    }

    [Fact]
    public async Task CheckAsync_CallbackReceivesCorrectEventArgs()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        ValueChangedEventArgs<int>? receivedArgs = null;

        monitor.Register(1, args =>
        {
            receivedArgs = args;
            return ValueTask.CompletedTask;
        });

        // Act
        await monitor.CheckAsync(new int[] { 10, 20, 30 });
        await monitor.CheckAsync(new int[] { 10, 25, 30 });

        // Assert
        await Task.Delay(50);
        Assert.NotNull(receivedArgs);
        Assert.Equal(1, receivedArgs.Address);
        Assert.Equal(20, receivedArgs.OldValue);
        Assert.Equal(25, receivedArgs.NewValue);
    }
}
