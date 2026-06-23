using DeveloperStore.Application;
using DeveloperStore.Common.HealthChecks;
using DeveloperStore.Common.Logging;
using DeveloperStore.Common.Validation;
using DeveloperStore.IoC;
using DeveloperStore.ORM;
using DeveloperStore.WebApi.Middleware;
using DeveloperStore.WebApi.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Text.Json.Serialization;
using System.Net;

namespace DeveloperStore.WebApi;

public partial class Program
{
    public static void Main(string[] args)
    {
        try
        {
            Log.Information("Starting web application");

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            builder.AddDefaultLogging();

            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    return new ObjectResult(ApiErrorFactory.FromModelState(context.HttpContext, context.ModelState))
                    {
                        StatusCode = StatusCodes.Status422UnprocessableEntity
                    };
                };
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new() { Title = "DeveloperStore Sales API", Version = "v1" });
            });

            builder.AddBasicHealthChecks();

            builder.Services.AddDbContext<DefaultContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    databaseOptions =>
                    {
                        databaseOptions.MigrationsAssembly("DeveloperStore.ORM");
                        databaseOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorCodesToAdd: null);
                    }));
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<DefaultContext>("postgresql", tags: ["readiness"]);

            builder.RegisterDependencies();

            builder.Services.AddMediatR(configuration =>
            {
                configuration.RegisterServicesFromAssemblies(typeof(ApplicationLayer).Assembly, typeof(Program).Assembly);
            });
            builder.Services.AddValidatorsFromAssembly(typeof(ApplicationLayer).Assembly);
            builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
            builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            var app = builder.Build();

            if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<DefaultContext>();
                if (dbContext.Database.IsRelational())
                {
                    dbContext.Database.Migrate();
                }
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseDefaultLogging();
            app.UseStatusCodePages(async statusCodeContext =>
            {
                var response = statusCodeContext.HttpContext.Response;
                if (response.HasStarted || response.StatusCode < 400)
                {
                    return;
                }

                response.ContentType = "application/json";

                var error = response.StatusCode switch
                {
                    StatusCodes.Status404NotFound => ApiErrorFactory.Create(
                        statusCodeContext.HttpContext,
                        HttpStatusCode.NotFound,
                        "resource_not_found",
                        "Resource not found",
                        "The requested resource could not be found."),
                    StatusCodes.Status405MethodNotAllowed => ApiErrorFactory.Create(
                        statusCodeContext.HttpContext,
                        HttpStatusCode.MethodNotAllowed,
                        "method_not_allowed",
                        "Method not allowed",
                        "The requested HTTP method is not allowed for this resource."),
                    _ => ApiErrorFactory.Create(
                        statusCodeContext.HttpContext,
                        (HttpStatusCode)response.StatusCode,
                        "http_error",
                        "HTTP error",
                        "The request could not be completed.")
                };

                await response.WriteAsJsonAsync(error);
            });
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();
            app.UseBasicHealthChecks();
            app.MapControllers();
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}

public partial class Program;
