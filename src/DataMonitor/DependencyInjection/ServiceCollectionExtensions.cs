using DataMonitor.Abstractions;
using DataMonitor.Configuration;
using DataMonitor.Core;
using DataMonitor.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataMonitor.DependencyInjection;

/// <summary>
/// 依赖注入扩展方法
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加 DataMonitor 服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDataMonitor(
        this IServiceCollection services,
        Action<MonitorOptions>? configure = null)
    {
        var options = new MonitorOptions();
        configure?.Invoke(options);
        options.Validate();

        // 注册配置
        services.AddSingleton(options);

        // 根据配置注册策略
        if (options.EnableFiltering)
        {
            services.TryAddSingleton(typeof(IChangeDetectionStrategy<>), typeof(FilteringStrategy<>));
        }
        else
        {
            services.TryAddSingleton(typeof(IChangeDetectionStrategy<>), typeof(NoFilterStrategy<>));
        }

        // 注册监控器
        services.Add([
        new ServiceDescriptor(typeof(IValueMonitor<>), typeof(ValueMonitor<>), options.MonitorLifetime),
        new ServiceDescriptor(typeof(IChangeMonitor<>), typeof(ChangeMonitor<>), options.MonitorLifetime),
        new ServiceDescriptor(typeof(ICompositeMonitor<>), typeof(CompositeMonitor<>), options.MonitorLifetime)
        ]);

        return services;
    }

    /// <summary>
    /// 添加指定类型的监控器
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddDataMonitor<T>(
        this IServiceCollection services,
        Action<MonitorOptions>? configure = null)
        where T : struct
    {
        var options = new MonitorOptions();
        configure?.Invoke(options);
        options.Validate();

        // 注册配置
        services.AddSingleton(options);

        // 根据配置注册策略
        if (options.EnableFiltering)
        {
            services.TryAddSingleton<IChangeDetectionStrategy<T>>(sp =>
                new FilteringStrategy<T>(options.FilterConfirmationCount));
        }
        else
        {
            services.TryAddSingleton<IChangeDetectionStrategy<T>, NoFilterStrategy<T>>();
        }

        // 注册监控器
        services.Add([
        new ServiceDescriptor(typeof(IValueMonitor<T>), typeof(ValueMonitor<T>), options.MonitorLifetime),
        new ServiceDescriptor(typeof(IChangeMonitor<T>), typeof(ChangeMonitor<T>), options.MonitorLifetime),
        new ServiceDescriptor(typeof(ICompositeMonitor<T>), typeof(CompositeMonitor<T>), options.MonitorLifetime)
        ]);

        return services;
    }

    /// <summary>
    /// 添加自定义策略
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    /// <typeparam name="TStrategy">策略类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddChangeDetectionStrategy<T, TStrategy>(this IServiceCollection services)
        where T : struct
        where TStrategy : class, IChangeDetectionStrategy<T>
    {
        services.AddSingleton<IChangeDetectionStrategy<T>, TStrategy>();
        return services;
    }
}
