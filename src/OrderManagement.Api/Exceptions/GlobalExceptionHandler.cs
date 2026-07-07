using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OrderManagement.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = MapException(exception);

        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["path"] = httpContext.Request.Path.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception) =>
        exception switch
        {
            ArgumentException argumentException => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                argumentException.Message),
            KeyNotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                "The requested resource was not found."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Server error",
                "An unexpected error occurred.")
        };
}
