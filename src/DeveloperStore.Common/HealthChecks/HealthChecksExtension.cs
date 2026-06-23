using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Net.Mime;

namespace DeveloperStore.Common.HealthChecks;

public static class HealthChecksExtension
{
    public static void AddBasicHealthChecks(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            .AddCheck("Liveness", () => HealthCheckResult.Healthy(), tags: ["liveness"]);
    }

    public static void UseBasicHealthChecks(this WebApplication app)
    {
        app.UseHealthChecks("/health/live", WriteHealthCheckResponse(app, "liveness"));
        app.UseHealthChecks("/health/ready", WriteHealthCheckResponse(app, "readiness"));
        app.UseHealthChecks("/health", WriteHealthCheckResponse(app, null));

        var logger = app.Services.GetRequiredService<ILogger<HealthCheckService>>();
        logger.LogInformation("Health checks enabled at /health");
    }

    private static HealthCheckOptions WriteHealthCheckResponse(WebApplication app, string? tag)
    {
        return new HealthCheckOptions
        {
            Predicate = check => string.IsNullOrWhiteSpace(tag) || check.Tags.Contains(tag),
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = async (context, report) =>
            {
                var result = new
                {
                    status = report.Status.ToString(),
                    healthChecks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        errorMessage = entry.Value.Exception?.Message,
                        hostEnvironment = app.Environment.EnvironmentName.ToLowerInvariant()
                    })
                };

                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsJsonAsync(result);
            }
        };
    }
}
