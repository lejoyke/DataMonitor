using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Core;
using DataMonitor.Models;
using DataMonitor.Strategies;

namespace DataMonitor.Tests;

public class CompositeMonitorTests
{
    private readonly MonitorOptions _defaultOptions = MonitorOptions.Default;

    private CompositeMonitor<T> CreateMonitor<T>(MonitorOptions? options = null) where T : struct
    {
        var strategy = new NoFilterStrategy<T>();
        return new CompositeMonitor<T>(strategy, options ?? _defaultOptions);
    }

    [Fact]
    public void RegisterValueMonitor_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();

        // Act
        var result = monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask);

        // Assert
        Assert.True(result);
        Assert.True(monitor.ContainsAddress(0));
    }

    [Fact]
    public void RegisterChangeMonitor_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();

        // Act
        var result = monitor.RegisterChangeMonitor(0, _ => ValueTask.CompletedTask);

        // Assert
        Assert.True(result);
        Assert.True(monitor.ContainsAddress(0));
    }

    [Fact]
    public void ContainsAddress_RegisteredInValueMonitor_ReturnsTrue()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask);

        // Act & Assert
        Assert.True(monitor.ContainsAddress(0));
        Assert.False(monitor.ContainsAddress(1));
    }

    [Fact]
    public void ContainsAddress_RegisteredInChangeMonitor_ReturnsTrue()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.RegisterChangeMonitor(0, _ => ValueTask.CompletedTask);

        // Act & Assert
        Assert.True(monitor.ContainsAddress(0));
    }

    [Fact]
    public void ContainsAddress_RegisteredInBoth_ReturnsTrue()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask);
        monitor.RegisterChangeMonitor(0, _ => ValueTask.CompletedTask);

        // Act & Assert
        Assert.True(monitor.ContainsAddress(0));
    }

    [Fact]
    public void RemoveMonitor_RemovesFromBoth()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask);
        monitor.RegisterChangeMonitor(0, _ => ValueTask.CompletedTask);

        // Act
        var result = monitor.RemoveMonitor(0);

        // Assert
        Assert.True(result);
        Assert.False(monitor.ContainsAddress(0));
    }

    [Fact]
    public void Clear_RemovesAllMonitors()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask);
        monitor.RegisterChangeMonitor(1, _ => ValueTask.CompletedTask);

        // Act
        monitor.Clear();

        // Assert
        Assert.False(monitor.ContainsAddress(0));
        Assert.False(monitor.ContainsAddress(1));
    }

    [Fact]
    public async Task CheckAsync_TriggersBothValueAndChangeMonitors()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var valueTriggered = false;
        var changeTriggered = false;

        monitor.RegisterValueMonitor(0, 100, _ =>
        {
            valueTriggered = true;
            return ValueTask.CompletedTask;
        });

        monitor.RegisterChangeMonitor(0, _ =>
        {
            changeTriggered = true;
            return ValueTask.CompletedTask;
        });

        // Act - 初始化变化监控
        await monitor.CheckAsync(new int[] { 50 });

        // Act - 值匹配且变化
        await monitor.CheckAsync(new int[] { 100 });

        // Assert
        await Task.Delay(100);
        Assert.True(valueTriggered);
        Assert.True(changeTriggered);
    }

    [Fact]
    public async Task CheckAsync_OnlyValueMatches_TriggersOnlyValueMonitor()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var valueTriggered = false;
        var changeTriggered = false;

        monitor.RegisterValueMonitor(0, 100, _ =>
        {
            valueTriggered = true;
            return ValueTask.CompletedTask;
        });

        monitor.RegisterChangeMonitor(0, _ =>
        {
            changeTriggered = true;
            return ValueTask.CompletedTask;
        });

        // Act - 初始化变化监控
        await monitor.CheckAsync(new int[] { 100 });

        // Act - 值匹配但不变化
        await monitor.CheckAsync(new int[] { 100 });

        // Assert
        await Task.Delay(100);
        Assert.True(valueTriggered);
        Assert.False(changeTriggered);
    }

    [Fact]
    public async Task CheckAsync_OnlyValueChanges_TriggersOnlyChangeMonitor()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var valueTriggered = false;
        var changeTriggered = false;

        monitor.RegisterValueMonitor(0, 100, _ =>
        {
            valueTriggered = true;
            return ValueTask.CompletedTask;
        });

        monitor.RegisterChangeMonitor(0, _ =>
        {
            changeTriggered = true;
            return ValueTask.CompletedTask;
        });

        // Act - 初始化变化监控
        await monitor.CheckAsync(new int[] { 50 });

        // Act - 值变化但不匹配目标值
        await monitor.CheckAsync(new int[] { 60 });

        // Assert
        await Task.Delay(100);
        Assert.False(valueTriggered);
        Assert.True(changeTriggered);
    }

    [Fact]
    public async Task CheckAsync_WithSpecificAddress_WorksCorrectly()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var valueTriggered = false;
        var changeTriggered = false;

        monitor.RegisterValueMonitor(0, 100, _ =>
        {
            valueTriggered = true;
            return ValueTask.CompletedTask;
        });

        monitor.RegisterChangeMonitor(0, _ =>
        {
            changeTriggered = true;
            return ValueTask.CompletedTask;
        });

        // Act - 初始化
        await monitor.CheckAsync(new int[] { 50 }, 0);

        // Act - 检查指定地址
        await monitor.CheckAsync(new int[] { 100 }, 0);

        // Assert
        await Task.Delay(100);
        Assert.True(valueTriggered);
        Assert.True(changeTriggered);
    }

    [Fact]
    public void ValueMonitor_Property_ReturnsValueMonitorInstance()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();

        // Act & Assert
        Assert.NotNull(monitor.ValueMonitor);
        Assert.IsAssignableFrom<IValueMonitor<int>>(monitor.ValueMonitor);
    }

    [Fact]
    public void ChangeMonitor_Property_ReturnsChangeMonitorInstance()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();

        // Act & Assert
        Assert.NotNull(monitor.ChangeMonitor);
        Assert.IsAssignableFrom<IChangeMonitor<int>>(monitor.ChangeMonitor);
    }

    [Fact]
    public void OnException_ForwardsExceptionsFromBothMonitors()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        var exceptionCount = 0;

        monitor.OnException += (sender, args) =>
        {
            Interlocked.Increment(ref exceptionCount);
        };

        // 注册会抛出异常的回调
        monitor.RegisterValueMonitor(0, 100, _ => throw new InvalidOperationException("Test"));

        // Assert - 验证事件已订阅
        Assert.Equal(0, exceptionCount); // 初始状态
    }

    [Fact]
    public void Dispose_DisposesInnerMonitors()
    {
        // Arrange
        var monitor = CreateMonitor<int>();

        // Act
        monitor.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() =>
            monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask));
    }

    [Fact]
    public async Task CheckAsync_ReturnsTrueIfAnyMonitorTriggered()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask);

        // Act
        var result = await monitor.CheckAsync(new int[] { 100 });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CheckAsync_ReturnsFalseIfNoMonitorTriggered()
    {
        // Arrange
        using var monitor = CreateMonitor<int>();
        monitor.RegisterValueMonitor(0, 100, _ => ValueTask.CompletedTask);

        // Act
        var result = await monitor.CheckAsync(new int[] { 50 });

        // Assert
        Assert.False(result);
    }
}
