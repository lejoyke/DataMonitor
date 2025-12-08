# DataMonitor

高性能 .NET 数据监控类库，用于监控数据中的数值匹配和变化检测。

## 功能特性

| 功能 | 描述 |
|------|------|
| **值监控** | 监控特定地址的值是否等于预设目标值 |
| **变化监控** | 监控特定地址的值是否发生变化 |
| **滤波策略** | 支持变化检测的滤波机制，避免信号抖动误触发 |
| **多类型支持** | 支持 `bool`、`short`、`int`、`float`、`ushort` 等多种数据类型 |
| **并行回调** | 所有匹配的回调并行执行，提高处理效率 |
| **异步支持** | 使用 `ValueTask` 统一处理同步和异步回调 |

## 快速开始

### 安装

```bash
dotnet add package DataMonitor
```

### 基本使用

```csharp
using DataMonitor.Core;
using DataMonitor.Configuration;

// 创建 Bool 监控器
var monitor = new BoolMonitorManager(MonitorOptions.Default);

// 注册值监控：当地址 10 的值为 true 时触发
monitor.RegisterValueMonitor(10, true, async args =>
{
    Console.WriteLine($"[{args.Timestamp}] Address {args.Address} matched: {args.Value}");
    await ValueTask.CompletedTask;
});

// 注册变化监控：当地址 10 的值发生变化时触发
monitor.RegisterChangeMonitor(10, async args =>
{
    Console.WriteLine($"[{args.Timestamp}] Address {args.Address} changed: {args.OldValue} → {args.NewValue}");
    await ValueTask.CompletedTask;
});

// 检查数据（直接传入泛型数组）
bool[] data = GetDataFromDevice();
await monitor.CheckAsync(data);

// 释放资源
monitor.Dispose();
```

### 使用滤波策略

```csharp
// 启用滤波，需要连续 2 次确认才触发
var options = new MonitorOptions
{
    EnableFiltering = true,
    FilterConfirmationCount = 2
};

var monitor = new BoolMonitorManager(options);

monitor.RegisterChangeMonitor(5, async args =>
{
    // 只有连续 2 次检测到相同变化才会触发
    Console.WriteLine($"Confirmed change at {args.Address}");
    await ValueTask.CompletedTask;
});
```

### 使用其他数据类型

```csharp
using DataMonitor.Core;
using DataMonitor.Strategies;

// 创建 Int16 监控器
var strategy = new NoFilterStrategy<short>();
var options = MonitorOptions.Default;

var monitor = new CompositeMonitor<short>(strategy, options);

// 监控地址 0 的 Int16 值变化
monitor.RegisterChangeMonitor(0, async args =>
{
    Console.WriteLine($"Int16 value changed: {args.OldValue} → {args.NewValue}");
    await ValueTask.CompletedTask;
});

// 检查数据
short[] data = new short[] { 1, 100, 32767 };
await monitor.CheckAsync(data);
```

### 依赖注入

```csharp
// Program.cs
services.AddDataMonitor(options =>
{
    options.EnableFiltering = true;
    options.FilterConfirmationCount = 2;
    options.CallbackTimeout = TimeSpan.FromSeconds(30);
});

// 使用
public class DeviceMonitorService
{
    private readonly ICompositeMonitor<bool> _monitor;

    public DeviceMonitorService(ICompositeMonitor<bool> monitor)
    {
        _monitor = monitor;
        _monitor.RegisterChangeMonitor(0, OnValueChanged);
    }

    private async ValueTask OnValueChanged(ValueChangedEventArgs<bool> args)
    {
        await ProcessChangeAsync(args);
    }
}
```

## 架构设计

```
┌─────────────────────────────────────────────────────────────┐
│                        API Layer                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │ IValueMonitor<T>│  │ IChangeMonitor<T>│  │IComposite   │  │
│  │                 │  │                  │  │ Monitor<T>  │  │
│  └────────┬────────┘  └────────┬────────┘  └──────┬──────┘  │
└───────────┼────────────────────┼─────────────────┼──────────┘
            │                    │                 │
            ▼                    ▼                 ▼
┌─────────────────────────────────────────────────────────────┐
│                    Core Implementation                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │
│  │ ValueMonitor<T> │  │ ChangeMonitor<T>│  │Composite    │  │
│  │                 │  │                  │  │ Monitor<T>  │  │
│  └────────┬────────┘  └────────┬────────┘  └──────┬──────┘  │
│           └────────────────────┼─────────────────┘          │
│                                ▼                             │
│                    ┌─────────────────────┐                   │
│                    │   MonitorBase<T>    │                   │
│                    └─────────────────────┘                   │
└─────────────────────────────────────────────────────────────┘
            │                    │
            ▼                    ▼
┌─────────────────────────────────────────────────────────────┐
│                      Support Layer                           │
│  ┌────────────────────────┐  ┌────────────────┐             │
│  │IChangeDetectionStrategy│  │ MonitorOptions │             │
│  │         <T>            │  │                │             │
│  └──────────┬─────────────┘  └────────────────┘             │
│             │                                                │
│  ┌──────────┴─────────────┐                                 │
│  │ • NoFilterStrategy<T>  │                                 │
│  │ • FilteringStrategy<T> │                                 │
│  └────────────────────────┘                                 │
└─────────────────────────────────────────────────────────────┘
```

## 目录结构

```
DataMonitor/
├── Abstractions/              # 接口定义层
│   ├── IMonitor.cs            # 监控器基础接口
│   ├── IValueMonitor.cs       # 值监控接口
│   ├── IChangeMonitor.cs      # 变化监控接口
│   ├── ICompositeMonitor.cs   # 组合监控器接口
│   └── IChangeDetectionStrategy.cs  # 变化检测策略接口
│
├── Configuration/             # 配置层
│   └── MonitorOptions.cs      # 监控器配置选项
│
├── Models/                    # 数据模型层
│   ├── MonitorEventArgsBase.cs      # 事件参数基类
│   ├── ValueMatchedEventArgs.cs     # 值匹配事件参数
│   ├── ValueChangedEventArgs.cs     # 值变化事件参数
│   ├── MonitorExceptionEventArgs.cs # 异常事件参数
│   └── ChangeDetectionResult.cs     # 变化检测结果
│
├── Strategies/                # 策略层
│   ├── NoFilterStrategy.cs    # 无滤波策略
│   └── FilteringStrategy.cs   # 滤波策略
│
├── Core/                      # 核心实现层
│   ├── MonitorBase.cs         # 监控器基类
│   ├── ValueMonitor.cs        # 值监控器实现
│   ├── ChangeMonitor.cs       # 变化监控器实现
│   ├── CompositeMonitor.cs    # 组合监控器实现
│   └── BoolMonitorManager.cs  # Bool 监控便捷类
│
├── Internal/                  # 内部工具层
│   ├── LockReleaser.cs        # 锁释放器
│   ├── CallbackExecutor.cs    # 回调执行器
│   └── DataStateManager.cs    # 数据状态管理器
│
└── DependencyInjection/       # 依赖注入层
    └── ServiceCollectionExtensions.cs  # DI 扩展方法
```

## 配置选项

```csharp
public sealed class MonitorOptions
{
    // 是否启用滤波（默认 false）
    public bool EnableFiltering { get; set; } = false;

    // 滤波确认次数（默认 2）
    public int FilterConfirmationCount { get; set; } = 2;

    // 回调超时时间（默认 30 秒）
    public TimeSpan CallbackTimeout { get; set; } = TimeSpan.FromSeconds(30);

    // 是否并行执行回调（默认 true）
    public bool ParallelCallbackExecution { get; set; } = true;

    // 锁超时时间（默认 3 秒）
    public TimeSpan LockTimeout { get; set; } = TimeSpan.FromSeconds(3);
}
```

## 变化检测策略

### NoFilterStrategy - 无滤波策略

立即触发所有变化，无需确认。

```
时序: T1 → T2 → T3
值:   0  → 1  → 0

T2: 检测到 0→1，立即触发回调
T3: 检测到 1→0，立即触发回调
```

### FilteringStrategy - 滤波策略

需要连续多次检测到相同变化才触发，可配置确认次数。

```
确认次数: 2

时序: T1 → T2 → T3 → T4 → T5
值:   0  → 1  → 1  → 0  → 1

T2: 检测到 0→1，进入待确认状态 (1/2)
T3: 确认 0→1，触发回调 ✓
T4: 检测到 1→0，进入待确认状态 (1/2)
T5: 检测到 0→1（与待确认值不同），重置为新的待确认 (1/2)
```

## 错误处理

```csharp
monitor.OnException += (sender, args) =>
{
    Console.WriteLine($"Error at address {args.Address}: {args.Exception.Message}");
};
```

## 性能特性

| 特性 | 说明 |
|------|------|
| `ReadOnlyMemory<T>` | 避免不必要的数组复制 |
| `ValueTask` | 减少异步状态机分配 |
| `record struct` | 值类型事件结果，栈分配 |
| 并行回调 | 所有匹配的回调并行执行 |

## 许可证

MIT License
