using System.Globalization;
using DayKeeper.Api.Middleware;
using DayKeeper.Api.Services;
using DayKeeper.Application;
using DayKeeper.Application.Interfaces;
using DayKeeper.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

// Bootstrap Serilog early so startup errors are captured
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting DayKeeper API");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ──────────────────────────────────────────────
    builder.Host.UseSerilog((context, loggerConfig) =>
        loggerConfig.ReadFrom.Configuration(context.Configuration));

    // ── Tenant Context ──────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

    // ── DI: Clean Architecture layers ────────────────────────
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // ── Controllers ──────────────────────────────────────────
    builder.Services.AddControllers();

    // ── OpenAPI / Scalar ─────────────────────────────────────
    builder.Services.AddOpenApi();

    // ── Health Checks ────────────────────────────────────────
    builder.Services.AddHealthChecks();

    // ── CORS ─────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    var app = builder.Build();

    // ── Exception handling (first in pipeline) ───────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // ── Serilog request logging ──────────────────────────────
    app.UseSerilogRequestLogging();

    // ── CORS ─────────────────────────────────────────────────
    app.UseCors("AllowAll");

    // ── OpenAPI + Scalar UI ──────────────────────────────────
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("DayKeeper API");
        });
    }

    // ── Health checks ────────────────────────────────────────
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Liveness: always healthy if process is running
    });

    app.MapHealthChecks("/health/ready");

    // ── Routing ──────────────────────────────────────────────
    app.UseHttpsRedirection();
    app.UseAuthorization();
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

// Make Program class accessible to WebApplicationFactory in integration tests
public partial class Program;
