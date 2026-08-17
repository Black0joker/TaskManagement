using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TaskManagement.API.Middleware;
using TaskManagement.API.Services;
using TaskManagement.Application;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Structured logging: single-line JSON events with UTC timestamps.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";
    options.IncludeScopes = true;
});

// Secret management: the JWT signing key is never stored in source code.
// Production requires it from a secure source; development generates a
// random ephemeral key when none is supplied (user-secrets / env vars).
if (builder.Environment.IsProduction() &&
    string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:Secret"]))
{
    throw new InvalidOperationException(
        "JwtSettings:Secret must be provided through a secure configuration source " +
        "(environment variable, user secrets or a key vault).");
}

if (builder.Environment.IsDevelopment() &&
    string.IsNullOrWhiteSpace(builder.Configuration["JwtSettings:Secret"]))
{
    builder.Configuration["JwtSettings:Secret"] = Convert.ToBase64String(
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
}

// Request size limit: this API only accepts small JSON payloads.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_000_000;
});

// Add services to the container.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Task Management API",
            Version = "v1",
            Description =
                "Project and task management API with role-based access control, " +
                "labels, comments, pagination, filtering, sorting and searching."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description =
                "JWT access token issued by POST /api/auth/login. " +
                "Paste the accessToken value without the 'Bearer ' prefix."
        };

        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, _) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any()
            && !metadata.OfType<IAllowAnonymous>().Any();

        if (!requiresAuth)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", null!, null!)] = new List<string>()
        });

        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });

        return Task.CompletedTask;
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Register application and infrastructure services.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Rate limiting: a generous global window plus a strict limiter for the
// authentication endpoints to slow down brute-force attempts.
// Disabled in the Testing environment so integration tests are not throttled.
var rateLimitingEnabled = !builder.Environment.IsEnvironment("Testing");

if (rateLimitingEnabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = "60";

            await context.HttpContext.Response.WriteAsJsonAsync(
                new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.20",
                    Title = "Too Many Requests",
                    Detail = "Too many requests. Please try again later.",
                    Status = StatusCodes.Status429TooManyRequests
                },
                (System.Text.Json.JsonSerializerOptions?)null,
                "application/problem+json",
                cancellationToken);
        };

        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter
            .Create<HttpContext, string>(context =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1)
                    }));

        options.AddFixedWindowLimiter("auth", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(1);
            limiterOptions.QueueLimit = 0;
        });
    });
}

// CORS: allowed origins come from configuration so they can differ per
// environment. Without configured origins no cross-origin access is granted.
builder.Services.AddCors(corsOptions =>
{
    corsOptions.AddPolicy("Default", policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// Authentication and authorization.
// Authorization policies are registered by Infrastructure.AddInfrastructureServices.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

var app = builder.Build();

// Apply migrations and seed sample data in development.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying migrations and seeding the database.");
    }
}

// Request logging wraps everything, including exception handling.
app.UseMiddleware<RequestLoggingMiddleware>();

// Global exception handling.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("Task Management API")
            .AddPreferredSecuritySchemes("Bearer");
    });
}

app.UseHttpsRedirection();

if (rateLimitingEnabled)
{
    app.UseRateLimiter();
}

app.UseCors("Default");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposes the entry point to WebApplicationFactory in the test project.
public partial class Program
{
}
