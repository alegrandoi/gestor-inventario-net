using GestorInventario.Application.Common.Auditing;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Caching;
using GestorInventario.Application.Common.Interfaces.Compliance;
using GestorInventario.Application.Common.Interfaces.Messaging;
using GestorInventario.Domain.Entities;
using GestorInventario.Infrastructure.Auditing;
using GestorInventario.Infrastructure.Caching;
using GestorInventario.Infrastructure.Configuration;
using GestorInventario.Infrastructure.DataGovernance;
using GestorInventario.Infrastructure.Identity;
using GestorInventario.Infrastructure.Messaging;
using GestorInventario.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace GestorInventario.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseOptions = configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>() ?? new DatabaseOptions();

        var connectionString = string.IsNullOrWhiteSpace(databaseOptions.ConnectionString)
            ? configuration.GetConnectionString("GestorInventario")
            : databaseOptions.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("The connection string 'GestorInventario' was not found in the configuration.");
        }

        services.AddSingleton<IDataGovernancePolicyRegistry, DataGovernancePolicyRegistry>();
        services.AddScoped<IDataGovernancePolicyEnforcer, DataGovernancePolicyEnforcer>();

        services.AddDbContext<GestorInventarioDbContext>(options =>
        {
            if (string.Equals(databaseOptions.Provider, DatabaseProviders.Sqlite, StringComparison.OrdinalIgnoreCase))
            {
                var sqliteConnectionString = PrepareSqliteConnectionString(connectionString);
                options.UseSqlite(
                    sqliteConnectionString,
                    builder => builder
                        .MigrationsAssembly(typeof(GestorInventarioDbContext).Assembly.FullName)
                        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
                return;
            }

            options.UseSqlServer(
                connectionString,
                builder => builder
                    .MigrationsAssembly(typeof(GestorInventarioDbContext).Assembly.FullName)
                    .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        services.AddScoped<IGestorInventarioDbContext>(provider =>
            provider.GetRequiredService<GestorInventarioDbContext>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<IAuditTrail, AuditTrailInterceptor>();

        var redisOptions = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();

        if (redisOptions.Enabled && !string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisOptions.ConnectionString;
                options.InstanceName = redisOptions.InstanceName;
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheKeyRegistry, DistributedCacheKeyRegistry>();
        services.AddScoped<ICacheInvalidationService, DistributedCacheInvalidationService>();

        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));

        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>() ?? new RabbitMqOptions();

        if (rabbitMqOptions.Enabled)
        {
            services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        }
        else
        {
            services.AddSingleton<IMessageBus, NullMessageBus>();
        }

        services.AddSingleton<IIntegrationEventPublisher, IntegrationEventPublisher>();

        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
            })
            .AddRoles<Role>()
            .AddRoleManager<RoleManager<Role>>()
            .AddUserStore<UserStore>()
            .AddRoleStore<RoleStore>();

        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }

    private static string PrepareSqliteConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new InvalidOperationException("The SQLite connection string must define a Data Source.");
        }

        var dataSourcePath = builder.DataSource;
        if (!Path.IsPathRooted(dataSourcePath))
        {
            dataSourcePath = Path.Combine(AppContext.BaseDirectory, dataSourcePath);
        }

        var directory = Path.GetDirectoryName(dataSourcePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        builder.DataSource = dataSourcePath;
        return builder.ToString();
    }
}
