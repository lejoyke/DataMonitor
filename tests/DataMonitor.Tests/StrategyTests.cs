using DataMonitor.Models;
using DataMonitor.Strategies;

namespace DataMonitor.Tests;

public class StrategyTests
{
    #region NoFilterStrategy Tests

    [Fact]
    public void NoFilterStrategy_ValueChanged_ShouldTrigger()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();

        // Act
        var result = strategy.Evaluate(0, 100, 200);

        // Assert
        Assert.True(result.ShouldTrigger);
        Assert.False(result.IsPending);
    }

    [Fact]
    public void NoFilterStrategy_ValueUnchanged_ShouldNotTrigger()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();

        // Act
        var result = strategy.Evaluate(0, 100, 100);

        // Assert
        Assert.False(result.ShouldTrigger);
        Assert.False(result.IsPending);
    }

    [Fact]
    public void NoFilterStrategy_BoolChange_ShouldTrigger()
    {
        // Arrange
        var strategy = new NoFilterStrategy<bool>();

        // Act
        var result = strategy.Evaluate(0, false, true);

        // Assert
        Assert.True(result.ShouldTrigger);
    }

    [Fact]
    public void NoFilterStrategy_GetPendingChangesCount_ReturnsZero()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();

        // Act & Assert
        Assert.Equal(0, strategy.GetPendingChangesCount());
    }

    [Fact]
    public void NoFilterStrategy_HasPendingChange_ReturnsFalse()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();

        // Act & Assert
        Assert.False(strategy.HasPendingChange(0));
    }

    [Fact]
    public void NoFilterStrategy_GetPendingAddresses_ReturnsEmptyArray()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();

        // Act
        var addresses = strategy.GetPendingAddresses();

        // Assert
        Assert.Empty(addresses);
    }

    [Fact]
    public void NoFilterStrategy_Reset_DoesNotThrow()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();

        // Act & Assert - 不应抛出异常
        strategy.Reset(0);
    }

    [Fact]
    public void NoFilterStrategy_Clear_DoesNotThrow()
    {
        // Arrange
        var strategy = new NoFilterStrategy<int>();

        // Act & Assert - 不应抛出异常
        strategy.Clear();
    }

    #endregion

    #region FilteringStrategy Tests

    [Fact]
    public void FilteringStrategy_FirstChange_IsPending()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act
        var result = strategy.Evaluate(0, 100, 200);

        // Assert
        Assert.False(result.ShouldTrigger);
        Assert.True(result.IsPending);
    }

    [Fact]
    public void FilteringStrategy_ConfirmedChange_ShouldTrigger()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act - 第一次变化
        var result1 = strategy.Evaluate(0, 100, 200);

        // Act - 第二次确认
        var result2 = strategy.Evaluate(0, 100, 200);

        // Assert
        Assert.False(result1.ShouldTrigger);
        Assert.True(result1.IsPending);
        Assert.True(result2.ShouldTrigger);
        Assert.False(result2.IsPending);
    }

    [Fact]
    public void FilteringStrategy_ChangeBackToOriginal_CancelsPending()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act - 第一次变化
        strategy.Evaluate(0, 100, 200);

        // Act - 回到原值
        var result = strategy.Evaluate(0, 100, 100);

        // Assert
        Assert.False(result.ShouldTrigger);
        Assert.False(result.IsPending);
        Assert.False(strategy.HasPendingChange(0));
    }

    [Fact]
    public void FilteringStrategy_ChangeToDifferentValue_ResetsPending()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act - 第一次变化到200
        strategy.Evaluate(0, 100, 200);

        // Act - 变化到300（不是200）
        var result = strategy.Evaluate(0, 100, 300);

        // Assert
        Assert.False(result.ShouldTrigger);
        Assert.True(result.IsPending);
        Assert.True(strategy.HasPendingChange(0));
    }

    [Fact]
    public void FilteringStrategy_ThreeConfirmations_Required()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 3);

        // Act
        var result1 = strategy.Evaluate(0, 100, 200);
        var result2 = strategy.Evaluate(0, 100, 200);
        var result3 = strategy.Evaluate(0, 100, 200);

        // Assert
        Assert.False(result1.ShouldTrigger);
        Assert.False(result2.ShouldTrigger);
        Assert.True(result3.ShouldTrigger);
    }

    [Fact]
    public void FilteringStrategy_GetPendingChangesCount_ReturnsCorrectCount()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act
        strategy.Evaluate(0, 100, 200);
        strategy.Evaluate(1, 300, 400);

        // Assert
        Assert.Equal(2, strategy.GetPendingChangesCount());
    }

    [Fact]
    public void FilteringStrategy_HasPendingChange_ReturnsTrueForPendingAddress()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act
        strategy.Evaluate(0, 100, 200);

        // Assert
        Assert.True(strategy.HasPendingChange(0));
        Assert.False(strategy.HasPendingChange(1));
    }

    [Fact]
    public void FilteringStrategy_GetPendingAddresses_ReturnsCorrectAddresses()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act
        strategy.Evaluate(0, 100, 200);
        strategy.Evaluate(5, 300, 400);

        var addresses = strategy.GetPendingAddresses();

        // Assert
        Assert.Equal(2, addresses.Length);
        Assert.Contains(0, addresses);
        Assert.Contains(5, addresses);
    }

    [Fact]
    public void FilteringStrategy_Reset_ClearsPendingForAddress()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);
        strategy.Evaluate(0, 100, 200);
        strategy.Evaluate(1, 300, 400);

        // Act
        strategy.Reset(0);

        // Assert
        Assert.False(strategy.HasPendingChange(0));
        Assert.True(strategy.HasPendingChange(1));
    }

    [Fact]
    public void FilteringStrategy_Clear_ClearsAllPending()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);
        strategy.Evaluate(0, 100, 200);
        strategy.Evaluate(1, 300, 400);

        // Act
        strategy.Clear();

        // Assert
        Assert.Equal(0, strategy.GetPendingChangesCount());
        Assert.False(strategy.HasPendingChange(0));
        Assert.False(strategy.HasPendingChange(1));
    }

    [Fact]
    public void FilteringStrategy_BoolType_WorksCorrectly()
    {
        // Arrange
        var strategy = new FilteringStrategy<bool>(confirmationCount: 2);

        // Act
        var result1 = strategy.Evaluate(0, false, true);
        var result2 = strategy.Evaluate(0, false, true);

        // Assert
        Assert.False(result1.ShouldTrigger);
        Assert.True(result2.ShouldTrigger);
    }

    [Fact]
    public void FilteringStrategy_MultipleAddresses_Independent()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act - 地址0开始变化
        strategy.Evaluate(0, 100, 200);

        // Act - 地址1开始变化
        strategy.Evaluate(1, 300, 400);

        // Act - 地址0确认
        var result0 = strategy.Evaluate(0, 100, 200);

        // Assert - 地址0应该触发，地址1还在等待
        Assert.True(result0.ShouldTrigger);
        Assert.True(strategy.HasPendingChange(1));
    }

    [Fact]
    public void FilteringStrategy_AfterTrigger_RequiresNewConfirmation()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 2);

        // Act - 完成一次触发
        strategy.Evaluate(0, 100, 200);
        var result1 = strategy.Evaluate(0, 100, 200);
        Assert.True(result1.ShouldTrigger);

        // Act - 新的变化需要重新确认
        var result2 = strategy.Evaluate(0, 200, 300);

        // Assert
        Assert.False(result2.ShouldTrigger);
        Assert.True(result2.IsPending);
    }

    [Fact]
    public void FilteringStrategy_ConfirmationCountOne_ImmediatelyTriggers()
    {
        // Arrange
        var strategy = new FilteringStrategy<int>(confirmationCount: 1);

        // Act
        var result = strategy.Evaluate(0, 100, 200);

        // Assert - 确认次数为1时，第一次变化就触发
        Assert.True(result.ShouldTrigger);
    }

    #endregion

    #region ChangeDetectionResult Tests

    [Fact]
    public void ChangeDetectionResult_Trigger_ReturnsCorrectResult()
    {
        // Act
        var result = ChangeDetectionResult.Trigger("Test reason");

        // Assert
        Assert.True(result.ShouldTrigger);
        Assert.False(result.IsPending);
        Assert.Equal("Test reason", result.Reason);
    }

    [Fact]
    public void ChangeDetectionResult_Pending_ReturnsCorrectResult()
    {
        // Act
        var result = ChangeDetectionResult.Pending("Test reason");

        // Assert
        Assert.False(result.ShouldTrigger);
        Assert.True(result.IsPending);
        Assert.Equal("Test reason", result.Reason);
    }

    [Fact]
    public void ChangeDetectionResult_NoChange_ReturnsCorrectResult()
    {
        // Act
        var result = ChangeDetectionResult.NoChange("Test reason");

        // Assert
        Assert.False(result.ShouldTrigger);
        Assert.False(result.IsPending);
        Assert.Equal("Test reason", result.Reason);
    }

    #endregion
}
