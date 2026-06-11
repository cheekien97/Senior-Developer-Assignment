using CovidAnalyticsPortal.Infrastructure.Logging;

namespace CovidAnalyticsPortal.API.Logging;

/// <summary>
/// Adapts the host's <see cref="IConfiguration"/> to the infrastructure
/// layer's <see cref="IConfigurationLike"/> abstraction, supplying the
/// configured rolling log-file path to the shared Serilog configurator.
/// </summary>
public sealed class SerilogFileConfiguration : IConfigurationLike
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SerilogFileConfiguration"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    public SerilogFileConfiguration(IConfiguration configuration)
    {
        LogFilePath = configuration.GetValue<string>("Serilog:LogFilePath");
    }

    /// <inheritdoc />
    public string? LogFilePath { get; }
}
