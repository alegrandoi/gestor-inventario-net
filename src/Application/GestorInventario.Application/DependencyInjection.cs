using System.Reflection;
using FluentValidation;
using GestorInventario.Application.Analytics.Optimization;
using GestorInventario.Application.Analytics.Services;
using GestorInventario.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton<IDemandForecastService, DemandForecastService>();
        services.AddSingleton<IInventoryOptimizationService, InventoryOptimizationService>();
        return services;
    }
}
