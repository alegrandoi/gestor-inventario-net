using GestorInventario.Api.Converters;
using GestorInventario.Api.Hubs;
using GestorInventario.Api.Middleware;
using GestorInventario.Application;
using GestorInventario.Application.Common.Interfaces;
using GestorInventario.Application.Common.Interfaces.Notifications;
using GestorInventario.Domain.Constants;
using GestorInventario.Infrastructure;
using GestorInventario.Infrastructure.Identity;
using GestorInventario.Infrastructure.Persistence;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IInventoryAlertNotifier, InventoryHubNotifier>();

var observabilitySection = builder.Configuration.GetSection("Observability");
var serviceName = observabilitySection.GetValue<string>("ServiceName") ?? builder.Environment.ApplicationName;
var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
var enableTracing = observabilitySection.GetValue("EnableTracing", true);
var enableMetrics = observabilitySection.GetValue("EnableMetrics", true);
var enableConsoleExporter = observabilitySection.GetSection("ConsoleExporter").GetValue("Enabled", builder.Environment.IsDevelopment());
var otlpSection = observabilitySection.GetSection("Otlp");
var otlpEnabled = otlpSection.GetValue("Enabled", false);
var otlpEndpoint = otlpSection.GetValue<string>("Endpoint");
var prometheusSection = observabilitySection.GetSection("Prometheus");
var enablePrometheus = prometheusSection.GetValue("Enabled", builder.Environment.IsDevelopment());
var prometheusEndpoint = prometheusSection.GetValue<string>("ScrapeEndpoint") ?? "/metrics";

var openTelemetryBuilder = builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: serviceName, serviceVersion: serviceVersion, serviceInstanceId: Environment.MachineName)
        .AddAttributes(new KeyValuePair<string, object>[]
        {
            new("deployment.environment", builder.Environment.EnvironmentName)
        }));

if (enableTracing)
{
    openTelemetryBuilder.WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.EnrichWithIDbCommand = (activity, command) =>
                {
                    if (activity is null || command is null)
                    {
                        return;
                    }

                    activity.SetTag("db.statement", command.CommandText);
                };
            })
            .AddSource("GestorInventario.Application")
            .AddSource("GestorInventario.Infrastructure");

        if (enableConsoleExporter)
        {
            tracing.AddConsoleExporter();
        }

        if (otlpEnabled && !string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });
}

if (enableMetrics)
{
    openTelemetryBuilder.WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation();

        if (enablePrometheus)
        {
            metrics.AddPrometheusExporter();
        }

        if (enableConsoleExporter)
        {
            metrics.AddConsoleExporter();
        }

        if (otlpEnabled && !string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });
}

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeScopes = true;
    logging.ParseStateValues = true;
    logging.IncludeFormattedMessage = true;

    if (enableConsoleExporter)
    {
        logging.AddConsoleExporter();
    }

    if (otlpEnabled && !string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        logging.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
    }
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new FlexibleEnumJsonConverterFactory());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Gestor de Inventario API",
        Version = "v1",
        Description = "API para gestionar productos, inventario, pedidos y analítica."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce el token JWT generado al iniciar sesión."
    };

    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});
builder.Services.AddHealthChecks();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
    new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOptions.Issuer),
            ValidateAudience = !string.IsNullOrWhiteSpace(jwtOptions.Audience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero
        };

        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministrator", policy => policy.RequireRole(RoleNames.Administrator));
    options.AddPolicy("RequirePlanner", policy => policy.RequireRole(RoleNames.Planner, RoleNames.Administrator));
    options.AddPolicy("RequireInventoryManager", policy => policy.RequireRole(RoleNames.InventoryManager, RoleNames.Administrator));
});

var httpsRedirectionEnabled = builder.Configuration.GetValue("HttpsRedirection:Enabled", !builder.Environment.IsDevelopment());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandling();

if (httpsRedirectionEnabled)
{
    app.UseHttpsRedirection();
}

app.UseCors("Frontend");

app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

if (enableMetrics && enablePrometheus)
{
    app.MapPrometheusScrapingEndpoint(prometheusEndpoint);
}

app.MapControllers();
app.MapHub<InventoryHub>("/hubs/inventory");
app.MapHealthChecks("/health");

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GestorInventarioDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("DatabaseInitialization");

    if (app.Environment.IsDevelopment())
    {
        await ResetSqliteDatabaseIfNeededAsync(dbContext, logger, CancellationToken.None).ConfigureAwait(false);
    }

    await dbContext.Database.MigrateAsync().ConfigureAwait(false);

    var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
    await identityService.EnsureSeedDataAsync(CancellationToken.None).ConfigureAwait(false);

}

static async Task ResetSqliteDatabaseIfNeededAsync(
    GestorInventarioDbContext dbContext,
    ILogger logger,
    CancellationToken cancellationToken)
{
    if (!dbContext.Database.IsSqlite())
    {
        return;
    }

    var databaseCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
    if (databaseCreator is null || !await databaseCreator.ExistsAsync(cancellationToken).ConfigureAwait(false))
    {
        return;
    }

    var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
    if (appliedMigrations.Any())
    {
        return;
    }

    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
    if (!pendingMigrations.Any())
    {
        return;
    }

    logger.LogWarning(
        "Se detectó una base de datos SQLite existente sin historial de migraciones. Se eliminará para aplicar migraciones limpiamente.");

    await dbContext.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
}

app.Run();

public partial class Program
{
}
