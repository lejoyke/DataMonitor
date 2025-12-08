using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Core;
using DataMonitor.Models;

namespace DataMonitor.Tests;

public class ValueMonitorTests
{
    private readonly MonitorOptions _defaultOptions = MonitorOptions.Default;

    [Fact]
    public void Register_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);

        // Act
        var result = monitor.Register(0, 100, _ => ValueTask.CompletedTask);

        // Assert
        Assert.True(result);
        Assert.Equal(1, monitor.Count);
        Assert.True(monitor.ContainsAddress(0));
    }

    [Fact]
    public void Register_DuplicateAddress_ReturnsFalse()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        monitor.Register(0, 100, _ => ValueTask.CompletedTask);

        // Act
        var result = monitor.Register(0, 200, _ => ValueTask.CompletedTask);

        // Assert
        Assert.False(result);
        Assert.Equal(1, monitor.Count);
    }

    [Fact]
    public void RegisterOrUpdate_ExistingAddress_UpdatesMonitor()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        monitor.Register(0, 100, _ => ValueTask.CompletedTask);

        // Act
        monitor.RegisterOrUpdate(0, 200, _ => ValueTask.CompletedTask);

        // Assert
        Assert.Equal(1, monitor.Count);
    }

    [Fact]
    public void Register_NegativeAddress_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            monitor.Register(-1, 100, _ => ValueTask.CompletedTask));
    }

    [Fact]
    public void Register_NullCallback_ThrowsArgumentNullException()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            monitor.Register(0, 100, null!));
    }

    [Fact]
    public void RemoveMonitor_ExistingAddress_ReturnsTrue()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        monitor.Register(0, 100, _ => ValueTask.CompletedTask);

        // Act
        var result = monitor.RemoveMonitor(0);

        // Assert
        Assert.True(result);
        Assert.Equal(0, monitor.Count);
        Assert.False(monitor.ContainsAddress(0));
    }

    [Fact]
    public void RemoveMonitor_NonExistingAddress_ReturnsFalse()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);

        // Act
        var result = monitor.RemoveMonitor(0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Clear_RemovesAllMonitors()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        monitor.Register(0, 100, _ => ValueTask.CompletedTask);
        monitor.Register(1, 200, _ => ValueTask.CompletedTask);
        monitor.Register(2, 300, _ => ValueTask.CompletedTask);

        // Act
        monitor.Clear();

        // Assert
        Assert.Equal(0, monitor.Count);
    }

    [Fact]
    public async Task CheckAsync_MatchingValue_TriggersCallback()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var triggered = false;
        int? triggeredAddress = null;
        int? triggeredValue = null;

        monitor.Register(1, 100, args =>
        {
            triggered = true;
            triggeredAddress = args.Address;
            triggeredValue = args.Value;
            return ValueTask.CompletedTask;
        });

        var data = new int[] { 0, 100, 200 };

        // Act
        var result = await monitor.CheckAsync(data);

        // Assert
        await Task.Delay(50); // 等待并行回调完成
        Assert.True(result);
        Assert.True(triggered);
        Assert.Equal(1, triggeredAddress);
        Assert.Equal(100, triggeredValue);
    }

    [Fact]
    public async Task CheckAsync_NonMatchingValue_DoesNotTriggerCallback()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var triggered = false;

        monitor.Register(1, 100, _ =>
        {
            triggered = true;
            return ValueTask.CompletedTask;
        });

        var data = new int[] { 0, 99, 200 };

        // Act
        var result = await monitor.CheckAsync(data);

        // Assert
        await Task.Delay(50);
        Assert.False(result);
        Assert.False(triggered);
    }

    [Fact]
    public async Task CheckAsync_MultipleMatches_TriggersAllCallbacks()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var triggeredAddresses = new List<int>();
        var lockObj = new object();

        monitor.Register(0, 10, args =>
        {
            lock (lockObj) { triggeredAddresses.Add(args.Address); }
            return ValueTask.CompletedTask;
        });

        monitor.Register(2, 30, args =>
        {
            lock (lockObj) { triggeredAddresses.Add(args.Address); }
            return ValueTask.CompletedTask;
        });

        var data = new int[] { 10, 20, 30 };

        // Act
        var result = await monitor.CheckAsync(data);

        // Assert
        await Task.Delay(100); // 等待并行回调完成
        Assert.True(result);
        Assert.Equal(2, triggeredAddresses.Count);
        Assert.Contains(0, triggeredAddresses);
        Assert.Contains(2, triggeredAddresses);
    }

    [Fact]
    public async Task CheckAsync_WithSpecificAddress_OnlyChecksSpecifiedAddress()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var triggeredCount = 0;

        monitor.Register(0, 10, _ =>
        {
            Interlocked.Increment(ref triggeredCount);
            return ValueTask.CompletedTask;
        });

        monitor.Register(1, 20, _ =>
        {
            Interlocked.Increment(ref triggeredCount);
            return ValueTask.CompletedTask;
        });

        var data = new int[] { 10, 20, 30 };

        // Act
        var result = await monitor.CheckAsync(data, 0);

        // Assert
        await Task.Delay(50);
        Assert.True(result);
        Assert.Equal(1, triggeredCount);
    }

    [Fact]
    public async Task CheckAsync_AddressOutOfRange_ReturnsFalse()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        monitor.Register(10, 100, _ => ValueTask.CompletedTask);

        var data = new int[] { 1, 2, 3 }; // 只有3个元素，地址10超出范围

        // Act
        var result = await monitor.CheckAsync(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckAsync_EmptyMonitors_ReturnsFalse()
    {
        // Arrange
        using var monitor = new ValueMonitor<int>(_defaultOptions);
        var data = new int[] { 1, 2, 3 };

        // Act
        var result = await monitor.CheckAsync(data);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckAsync_BoolType_WorksCorrectly()
    {
        // Arrange
        using var monitor = new ValueMonitor<bool>(_defaultOptions);
        var triggered = false;

        monitor.Register(1, true, _ =>
        {
            triggered = true;
            return ValueTask.CompletedTask;
        });

        var data = new bool[] { false, true, false };

        // Act
        var result = await monitor.CheckAsync(data);

        // Assert
        await Task.Delay(50);
        Assert.True(result);
        Assert.True(triggered);
    }

    [Fact]
    public async Task CheckAsync_FloatType_WorksCorrectly()
    {
        // Arrange
        using var monitor = new ValueMonitor<float>(_defaultOptions);
        var triggered = false;

        monitor.Register(0, 3.14f, _ =>
        {
            triggered = true;
            return ValueTask.CompletedTask;
        });

        var data = new float[] { 3.14f, 2.71f };

        // Act
        var result = await monitor.CheckAsync(data);

        // Assert
        await Task.Delay(50);
        Assert.True(result);
        Assert.True(triggered);
    }

    [Fact]
    public async Task Dispose_PreventsSubsequentOperations()
    {
        // Arrange
        var monitor = new ValueMonitor<int>(_defaultOptions);
        monitor.Dispose();

        // Act & Assert - CheckAsync 应该抛出异常
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await monitor.CheckAsync(new int[] { 100 }));
    }
}
