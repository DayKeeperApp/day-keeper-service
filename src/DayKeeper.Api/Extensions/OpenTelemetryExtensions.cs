using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DayKeeper.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection AddDayKeeperOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var otelSection = configuration.GetSection("OpenTelemetry");

        if (!otelSection.GetValue("Enabled", true))
        {
            return services;
        }

        var serviceName = otelSection.GetValue("ServiceName", "daykeeper-api")!;
        var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        var otel = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["deployment.environment.name"] =
                        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                }));

        ConfigureTracing(otel, otelSection.GetSection("Tracing"));
        ConfigureMetrics(otel, otelSection.GetSection("Metrics"));

        return services;
    }

    private static void ConfigureTracing(
        OpenTelemetry.OpenTelemetryBuilder otel,
        IConfigurationSection tracingSection)
    {
        if (!tracingSection.GetValue("Enabled", true))
        {
            return;
        }

        var samplingRatio = tracingSection.GetValue("SamplingRatio", 1.0);
        var exportInterval = tracingSection.GetValue("ExportIntervalMilliseconds", 5000);

        otel.WithTracing(tracing =>
        {
            if (samplingRatio < 1.0)
            {
                tracing.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));
            }

            tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.Filter = context =>
                        !context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
                })
                .AddHttpClientInstrumentation()
                .AddQuartzInstrumentation()
                .AddNpgsql()
                .AddSource("HotChocolate.Execution")
                .AddOtlpExporter(opts =>
                {
                    opts.BatchExportProcessorOptions.ScheduledDelayMilliseconds = exportInterval;
                });
        });
    }

    private static void ConfigureMetrics(
        OpenTelemetry.OpenTelemetryBuilder otel,
        IConfigurationSection metricsSection)
    {
        if (!metricsSection.GetValue("Enabled", true))
        {
            return;
        }

        var exportInterval = metricsSection.GetValue("ExportIntervalMilliseconds", 15000);

        otel.WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddNpgsqlInstrumentation()
            .AddOtlpExporter((exporterOpts, readerOpts) =>
            {
                readerOpts.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = exportInterval;
            }));
    }
}
