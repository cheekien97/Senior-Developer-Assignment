using Asp.Versioning.ApiExplorer;
using CovidAnalyticsPortal.API;
using CovidAnalyticsPortal.API.Logging;
using CovidAnalyticsPortal.API.Middleware;
using CovidAnalyticsPortal.Application;
using CovidAnalyticsPortal.Infrastructure;
using CovidAnalyticsPortal.Infrastructure.Logging;
using CovidAnalyticsPortal.Infrastructure.Persistence;
using Serilog;

// Bootstrap a minimal logger so failures during start-up are captured.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting COVID-19 Analytics Portal API");

    var builder = WebApplication.CreateBuilder(args);

    // Structured logging via Serilog, configured from the shared configurator.
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.Configure(
            new SerilogFileConfiguration(context.Configuration),
            "CovidAnalyticsPortal.API"));

    // Layered service registration (inner to outer).
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApiServices();

    builder.Services.AddProblemDetails();

    var app = builder.Build();

    // Bring the database schema up to date by applying any pending migrations,
    // so the portal works on a fresh machine without manual database setup.
    await app.Services.MigrateDatabaseAsync();

    // ---- HTTP request pipeline ----

    // Correlation id first so every subsequent log line and error carries it.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Baseline security response headers on every response (including errors).
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Structured request logging (method, path, status, elapsed). Sits outside
    // the exception handler so it records the final, translated status code.
    app.UseSerilogRequestLogging();

    // Global exception handling translates unhandled exceptions to ProblemDetails.
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        });
    }

    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthorization();
    app.MapControllers().RequireRateLimiting(CovidAnalyticsPortal.API.DependencyInjection.GlobalRateLimitPolicy);

    app.Run();
}
catch (Exception exception)
    when (exception is not HostAbortedException
          && !exception.GetType().Name.Contains("StopTheHostException", StringComparison.Ordinal))
{
    Log.Fatal(exception, "COVID-19 Analytics Portal API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposed so the functional test project can reference the API host via
/// <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program
{
}
