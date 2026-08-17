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
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
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
        await context.Response.WriteAsJsonAsync(responseBody);
    }

    private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail) => new()
    {
        Status = statusCode,
        Title = title,
        Detail = detail
    };
}
