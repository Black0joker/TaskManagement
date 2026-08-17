using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Authorization;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Tests.Integration;

/// <summary>
/// Boots the real API pipeline (controllers, middleware, JWT auth, permissions)
/// against an EF Core InMemory database. Runs in the "Testing" environment so
/// the development seeder (which calls MigrateAsync) is skipped.
/// </summary>
public sealed class TaskManagementApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"integration-{Guid.NewGuid():N}";
    private bool _initialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // The SQL connection string is replaced, never used.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Server=unused;Database=unused;");

        // JWT configuration required by the authentication services.
        builder.UseSetting("JwtSettings:Secret",
            "integration-test-signing-key-0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b");
        builder.UseSetting("JwtSettings:Issuer", "TaskManagement.Api");
        builder.UseSetting("JwtSettings:Audience", "TaskManagement.Client");
        builder.UseSetting("JwtSettings:AccessTokenExpirationMinutes", "30");
        builder.UseSetting("JwtSettings:RefreshTokenExpirationDays", "7");

        builder.ConfigureTestServices(services =>
        {
            // Remove the SQL Server EF provider entirely; two database providers
            // cannot coexist in one service provider.
            const string SqlServerAssemblyName = "Microsoft.EntityFrameworkCore.SqlServer";

            var sqlServerDescriptors = services
                .Where(d =>
                    d.ServiceType.Assembly.GetName().Name == SqlServerAssemblyName
                    || d.ImplementationType?.Assembly.GetName().Name == SqlServerAssemblyName)
                .ToList();

            foreach (var descriptor in sqlServerDescriptors)
            {
                services.Remove(descriptor);
            }

            var contextDescriptors = services
                .Where(d => d.ServiceType == typeof(AppDbContext)
                    || d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                    || d.ServiceType == typeof(Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<AppDbContext>))
                .ToList();

            foreach (var descriptor in contextDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>
    /// Creates the schema and the identity roles that are normally seeded at startup.
    /// Idempotent; safe to call from every test's InitializeAsync.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (_initialized)
        {
            return;
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in ApplicationRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        _initialized = true;
    }
}
