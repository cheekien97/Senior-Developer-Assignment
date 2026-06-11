using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CovidAnalyticsPortal.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that emits structured logs around every request,
/// recording the request name, successful completion, elapsed time, and any
/// unhandled exception. Provides consistent, cross-cutting observability
/// without polluting individual handlers (Single Responsibility Principle).
/// </summary>
/// <typeparam name="TRequest">The request type flowing through the pipeline.</typeparam>
/// <typeparam name="TResponse">The response type produced by the handler.</typeparam>
public sealed class LoggingBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehaviour<TRequest, TResponse>> _logger;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="LoggingBehaviour{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger used to emit structured log entries.</param>
    public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling request {RequestName}", requestName);

        try
        {
            var response = await next().ConfigureAwait(false);

            stopwatch.Stop();
            _logger.LogInformation(
                "Handled request {RequestName} in {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(
                exception,
                "Request {RequestName} failed after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
