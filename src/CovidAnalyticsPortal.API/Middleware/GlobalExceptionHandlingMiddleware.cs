using System.Net;
using System.Text.Json;
using CovidAnalyticsPortal.API.Context;
using CovidAnalyticsPortal.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CovidAnalyticsPortal.API.Middleware;

/// <summary>
/// Centralised exception-handling middleware. Catches any unhandled exception
/// thrown downstream, logs it with the request's correlation identifier, and
/// translates it into a sanitised RFC 7807 <see cref="ProblemDetails"/>
/// response. Internal details are never leaked to clients, satisfying secure
/// error-handling requirements.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="GlobalExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger used to record failures.</param>
    /// <param name="environment">The host environment, used to decide detail verbosity.</param>
    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware, handling any exception raised downstream.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception).ConfigureAwait(false);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items.TryGetValue(HttpCurrentContext.CorrelationHeader, out var value)
            ? value?.ToString()
            : context.TraceIdentifier;

        var problem = MapToProblemDetails(exception, correlationId);

        if (problem.Status >= (int)HttpStatusCode.InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path} (correlation {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                "Request {Method} {Path} rejected: {Detail} (correlation {CorrelationId})",
                context.Request.Method,
                context.Request.Path,
                problem.Detail,
                correlationId);
        }

        context.Response.Clear();
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response
            .WriteAsync(JsonSerializer.Serialize(problem, problem.GetType(), SerializerOptions))
            .ConfigureAwait(false);
    }

    private ProblemDetails MapToProblemDetails(Exception exception, string? correlationId)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            DomainException domain => CreateProblem(
                StatusCodes.Status400BadRequest,
                "Domain Rule Violation",
                domain.Message),
            _ => CreateProblem(
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later."),
        };

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        return problem;
    }

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = "See the 'errors' property for details.",
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
        };
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail) => new()
    {
        Status = status,
        Title = title,
        Detail = detail,
        Type = $"https://datatracker.ietf.org/doc/html/rfc7231#section-6.{(status >= 500 ? 6 : 5)}",
    };
}
