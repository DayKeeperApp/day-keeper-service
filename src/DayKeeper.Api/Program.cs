using System.Globalization;
using Asp.Versioning;
using DayKeeper.Api.GraphQL;
using DayKeeper.Api.GraphQL.Mutations;
using DayKeeper.Api.GraphQL.Queries;
using DayKeeper.Api.GraphQL.Validation;
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

    // ── API Versioning ─────────────────────────────────────
    builder.Services
        .AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    // ── GraphQL ──────────────────────────────────────────────
    builder.Services
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .AddMutationType<Mutation>()
        .AddTypeExtension<TenantQueries>()
        .AddTypeExtension<UserQueries>()
        .AddTypeExtension<SpaceQueries>()
        .AddTypeExtension<SpaceMembershipQueries>()
        .AddTypeExtension<TenantMutations>()
        .AddTypeExtension<UserMutations>()
        .AddTypeExtension<SpaceMutations>()
        .AddTypeExtension<SpaceMembershipMutations>()
        .AddTypeExtension<ProjectQueries>()
        .AddTypeExtension<ProjectMutations>()
        .AddTypeExtension<TaskItemQueries>()
        .AddTypeExtension<TaskItemMutations>()
        .AddTypeExtension<CalendarQueries>()
        .AddTypeExtension<CalendarMutations>()
        .AddTypeExtension<CalendarEventQueries>()
        .AddTypeExtension<CalendarEventMutations>()
        .AddTypeExtension<PersonQueries>()
        .AddTypeExtension<PersonMutations>()
        .AddTypeExtension<ContactMethodMutations>()
        .AddTypeExtension<AddressMutations>()
        .AddTypeExtension<ImportantDateMutations>()
        .AddMutationConventions(new MutationConventionOptions
        {
            ApplyToAllMutations = true,
        })
        .AddErrorFilter<DomainErrorFilter>()
        .TryAddTypeInterceptor<ValidationTypeInterceptor>()
        .AddFiltering()
        .AddSorting()
        .AddProjections()
        .AddDbContextCursorPagingProvider()
        .ModifyPagingOptions(opt =>
        {
            opt.DefaultPageSize = 25;
            opt.MaxPageSize = 100;
            opt.IncludeTotalCount = true;
        });

    // ── OpenAPI / Scalar ─────────────────────────────────────
    builder.Services.AddOpenApi("v1");

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
    app.MapGraphQL();

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
