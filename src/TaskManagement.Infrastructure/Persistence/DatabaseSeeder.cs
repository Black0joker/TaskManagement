using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

public class DatabaseSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    private const string AdminEmail = "admin@taskmanagement.local";
    private const string AdminPassword = "Admin@12345";
    private const string UserEmail = "user@taskmanagement.local";
    private const string UserPassword = "User@12345";

    private readonly AppDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        AppDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async System.Threading.Tasks.Task SeedAsync()
    {
        await _context.Database.MigrateAsync();

        await SeedRolesAsync();
        var admin = await SeedAdminUserAsync();
        var user = await SeedNormalUserAsync();
        await SeedSampleDataAsync(admin, user);
    }

    private async System.Threading.Tasks.Task SeedRolesAsync()
    {
        string[] roles = { AdminRole, UserRole };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create role {Role}: {Errors}", role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async System.Threading.Tasks.Task<User> SeedAdminUserAsync()
    {
        var admin = await _userManager.FindByEmailAsync(AdminEmail);
        if (admin is null)
        {
            admin = new User
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(admin, AdminPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await _userManager.AddToRoleAsync(admin, AdminRole);
        }

        return admin;
    }

    private async System.Threading.Tasks.Task<User> SeedNormalUserAsync()
    {
        var user = await _userManager.FindByEmailAsync(UserEmail);
        if (user is null)
        {
            user = new User
            {
                UserName = UserEmail,
                Email = UserEmail,
                FirstName = "Sample",
                LastName = "User",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, UserPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create normal user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            await _userManager.AddToRoleAsync(user, UserRole);
        }

        return user;
    }

    private async System.Threading.Tasks.Task SeedSampleDataAsync(User admin, User user)
    {
        if (await _context.Projects.AnyAsync())
        {
            return; // Sample data already seeded.
        }

        var now = DateTime.UtcNow;

        var project = new Project
        {
            Name = "Website Redesign",
            Description = "Redesign the public-facing marketing website.",
            CreatedById = admin.Id,
            CreatedAt = now,
            UpdatedAt = now
        };

        var labels = new List<Label>
        {
            new() { Name = "Backend", Color = "#3B82F6", ProjectId = project.Id },
            new() { Name = "Frontend", Color = "#10B981", ProjectId = project.Id },
            new() { Name = "Bug", Color = "#EF4444", ProjectId = project.Id },
            new() { Name = "Documentation", Color = "#F59E0B", ProjectId = project.Id }
        };

        var tasks = new List<TaskItem>
        {
            new()
            {
                ProjectId = project.Id,
                Title = "Design landing page",
                Description = "Create the initial design mockups for the new landing page.",
                Status = TaskItemStatus.InProgress,
                Priority = TaskItemPriority.High,
                DueDate = now.AddDays(7),
                AssignedToId = user.Id,
                CreatedById = admin.Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                ProjectId = project.Id,
                Title = "Set up CI pipeline",
                Description = "Configure build and deployment automation.",
                Status = TaskItemStatus.Todo,
                Priority = TaskItemPriority.Medium,
                DueDate = now.AddDays(14),
                AssignedToId = admin.Id,
                CreatedById = admin.Id,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                ProjectId = project.Id,
                Title = "Write API documentation",
                Description = "Document all public endpoints and models.",
                Status = TaskItemStatus.Todo,
                Priority = TaskItemPriority.Low,
                AssignedToId = user.Id,
                CreatedById = admin.Id,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        project.Tasks = tasks;

        _context.Projects.Add(project);
        _context.Labels.AddRange(labels);

        await _context.SaveChangesAsync();

        // Assign the Backend and Bug labels to the first task.
        var firstTask = tasks[0];
        _context.TaskItemLabels.AddRange(
            new TaskItemLabel { TaskItemId = firstTask.Id, LabelId = labels[0].Id },
            new TaskItemLabel { TaskItemId = firstTask.Id, LabelId = labels[2].Id });

        await _context.SaveChangesAsync();

        _logger.LogInformation("Seeded sample project '{Project}' with {TaskCount} tasks and {LabelCount} labels.",
            project.Name, tasks.Count, labels.Count);
    }
}
