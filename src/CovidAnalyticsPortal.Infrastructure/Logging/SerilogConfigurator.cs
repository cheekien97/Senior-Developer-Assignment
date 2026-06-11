using Serilog;
using Serilog.Events;

namespace CovidAnalyticsPortal.Infrastructure.Logging;

/// <summary>
/// Centralises Serilog configuration so every host (API and Web) produces
/// consistent, structured logs. Reads any settings present in configuration and
/// applies sensible production defaults — console and rolling-file sinks plus
/// environment and thread enrichment — when explicit settings are absent.
/// </summary>
public static class SerilogConfigurator
{
    /// <summary>
    /// Applies the portal's standard Serilog configuration to the supplied
    /// logger configuration.
    /// </summary>
    /// <param name="loggerConfiguration">The Serilog logger configuration to populate.</param>
    /// <param name="configuration">The application configuration (for optional overrides).</param>
    /// <param name="applicationName">The application name to stamp onto every log event.</param>
    /// <returns>The same <paramref name="loggerConfiguration"/> for chaining.</returns>
    public static LoggerConfiguration Configure(
        this LoggerConfiguration loggerConfiguration,
        IConfigurationLike configuration,
        string applicationName)
    {
        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", applicationName)
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} " +
                "<s:{SourceContext}> {CorrelationId} {NewLine}{Exception}")
            .WriteTo.File(
                path: configuration.LogFilePath ?? "logs/covid-portal-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] " +
                "{Application} {CorrelationId} {Message:lj}{NewLine}{Exception}");

        return loggerConfiguration;
    }
}

/// <summary>
/// A minimal abstraction over the configuration values the
/// <see cref="SerilogConfigurator"/> needs, keeping the infrastructure project
/// free of a hard dependency on a specific configuration package.
/// </summary>
public interface IConfigurationLike
{
    /// <summary>
    /// Gets the configured rolling log-file path, or <c>null</c> to use the
    /// default.
    /// </summary>
    string? LogFilePath { get; }
}
