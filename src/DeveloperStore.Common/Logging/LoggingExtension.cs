using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Sinks.SystemConsole.Themes;
using System.Diagnostics;

namespace DeveloperStore.Common.Logging;

public static class LoggingExtension
{
    private static readonly DestructuringOptionsBuilder DestructuringOptionsBuilder = new DestructuringOptionsBuilder()
        .WithDefaultDestructurers();

    private static readonly Func<LogEvent, bool> FilterPredicate = logEvent =>
    {
        if (logEvent.Level != LogEventLevel.Information)
        {
            return true;
        }

        logEvent.Properties.TryGetValue("StatusCode", out var statusCode);
        logEvent.Properties.TryGetValue("Path", out var path);

        var excludeByStatusCode = statusCode is null || statusCode.ToString().Equals("200");
        var excludeByPath = path?.ToString().Contains("/health", StringComparison.OrdinalIgnoreCase) ?? false;

        return excludeByStatusCode && excludeByPath;
    };

    public static WebApplicationBuilder AddDefaultLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();

        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails(DestructuringOptionsBuilder)
                .Filter.ByExcluding(FilterPredicate);

            if (Debugger.IsAttached)
            {
                loggerConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    theme: SystemConsoleTheme.Colored);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.Console(
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
                    .WriteTo.File(
                        "logs/log-.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}");
            }
        });

        builder.Services.AddLogging();
        return builder;
    }

    public static WebApplication UseDefaultLogging(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Logger>>();
        logger.LogInformation(
            "Logging enabled for '{Application}' on '{Environment}'",
            app.Environment.ApplicationName,
            app.Environment.EnvironmentName);

        return app;
    }
}
