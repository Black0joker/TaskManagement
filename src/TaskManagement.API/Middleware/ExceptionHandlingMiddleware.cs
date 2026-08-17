using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.Common.Exceptions;

namespace TaskManagement.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
            return;
        }

        await WriteEmptyErrorBodyAsync(context);
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning(
                exception,
                "Exception occurred after the response had started; unable to write an error response.");
            throw exception;
        }

        int statusCode;
        object responseBody;

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                responseBody = new ValidationProblemDetails(validationException.Errors)
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = StatusCodes.Status400BadRequest
                };
                break;

            case UnauthorizedException unauthorizedException:
                statusCode = StatusCodes.Status401Unauthorized;
                responseBody = CreateProblemDetails(statusCode, "Unauthorized", unauthorizedException.Message);
                break;

            case ForbiddenAccessException forbiddenException:
                statusCode = StatusCodes.Status403Forbidden;
                responseBody = CreateProblemDetails(statusCode, "Forbidden", forbiddenException.Message);
                break;

            case NotFoundException notFoundException:
                statusCode = StatusCodes.Status404NotFound;
                responseBody = CreateProblemDetails(statusCode, "Not Found", notFoundException.Message);
                break;

            case ConflictException conflictException:
                statusCode = StatusCodes.Status409Conflict;
                responseBody = CreateProblemDetails(statusCode, "Conflict", conflictException.Message);
                break;

            case BusinessRuleException businessRuleException:
                statusCode = StatusCodes.Status422UnprocessableEntity;
                responseBody = CreateProblemDetails(
                    statusCode,
                    "Unprocessable Entity",
                    businessRuleException.Message);
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                responseBody = CreateProblemDetails(
                    statusCode,
                    "Internal Server Error",
                    "An unexpected error occurred. Please try again later.");
                _logger.LogError(exception, "Unhandled exception occurred.");
                break;
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(responseBody, (JsonSerializerOptions?)null, "application/problem+json");
    }

    /// <summary>
    /// Ensures error responses produced outside MVC (for example the JWT
    /// challenge handler or unmatched routes) still carry a ProblemDetails body.
    /// </summary>
    private static async Task WriteEmptyErrorBodyAsync(HttpContext context)
    {
        var statusCode = context.Response.StatusCode;

        if (context.Response.HasStarted || context.Response.ContentLength > 0)
        {
            return;
        }

        var (title, detail) = statusCode switch
        {
            StatusCodes.Status401Unauthorized => ("Unauthorized", "Authentication is required to access this resource."),
            StatusCodes.Status403Forbidden => ("Forbidden", "You do not have permission to access this resource."),
            StatusCodes.Status404NotFound => ("Not Found", "The requested resource was not found."),
            StatusCodes.Status405MethodNotAllowed => ("Method Not Allowed", "The HTTP method is not allowed for this resource."),
            _ => (null!, null!)
        };

        if (title is null)
        {
            return;
        }

        await context.Response.WriteAsJsonAsync(
            CreateProblemDetails(statusCode, title, detail),
            (JsonSerializerOptions?)null,
            "application/problem+json");
    }

    private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail) => new()
    {
        Type = statusCode switch
        {
            StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            StatusCodes.Status401Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            StatusCodes.Status403Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            StatusCodes.Status405MethodNotAllowed => "https://tools.ietf.org/html/rfc9110#section-15.5.6",
            StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
            _ => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        },
        Status = statusCode,
        Title = title,
        Detail = detail
    };
}
