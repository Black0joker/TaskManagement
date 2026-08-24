using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Application.Abstractions.Authentication;
using TaskManagement.Application.Abstractions.Identity;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;
using TaskManagement.Application.Common.Models;
using TaskManagement.Domain.Authorization;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Authentication;
using TaskManagement.Infrastructure.Authorization;
using TaskManagement.Infrastructure.Identity;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Projects;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>());

        AddIdentityServices(services);
        AddAuthenticationServices(services, configuration);
        AddAuthorizationServices(services);

        services.Configure<AuthenticationSettings>(
            configuration.GetSection(AuthenticationSettings.SectionName));

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IProjectAccessService, ProjectAccessService>();

        // Background cleanup of expired/revoked refresh tokens to prevent
        // unbounded growth of the RefreshTokens table.
        services.AddHostedService<RefreshTokenCleanupService>();

        services.AddScoped<DatabaseSeeder>();

        return services;
    }

    private static void AddIdentityServices(IServiceCollection services)
    {
        services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;

                // Per-account lockout against brute-force attacks. This complements
                // the IP-based rate limiting: distributed attackers can spread
                // attempts across IPs but cannot avoid the per-account counter.
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>();
    }

    private static void AddAuthenticationServices(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = new JwtSettings();
        configuration.Bind(JwtSettings.SectionName, jwtSettings);

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero
                };
            });
    }

    private static void AddAuthorizationServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Role-based policy for admin-only endpoints.
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole(ApplicationRoles.Admin));

            // Permission-based policies: one policy per permission.
            // Use with [Authorize(Policy = ApplicationPermissions.Projects.Create)].
            foreach (var permission in ApplicationPermissions.All)
            {
                options.AddPolicy(permission, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new PermissionRequirement(permission));
                });
            }
        });

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
    }
}
