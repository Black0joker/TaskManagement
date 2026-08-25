using System.Reflection;
using MediatR;
using TaskManagement.Application.Abstractions.Persistence;
using TaskManagement.Application.Abstractions.Projects;

namespace TaskManagement.Tests.Unit;

/// <summary>
/// Architecture guard: controller policies are claim-based and not
/// project-scoped, so every handler touching project-scoped data must depend
/// on <see cref="IProjectAccessService"/> and enforce membership itself.
/// A future handler that omits the dependency fails here instead of
/// introducing a silent authorization hole.
/// </summary>
public class AuthorizationArchitectureTests
{
    private static readonly string[] ProjectScopedFeaturePrefixes =
    {
        "TaskManagement.Application.Features.Tasks",
        "TaskManagement.Application.Features.Comments",
        "TaskManagement.Application.Features.Labels",
        "TaskManagement.Application.Features.ProjectMembers",
        "TaskManagement.Application.Features.Projects"
    };

    /// <summary>
    /// Handlers with a documented reason for not checking membership on an
    /// existing project: CreateProject establishes ownership for the creator,
    /// and ListProjects scopes its query by membership instead of checking a
    /// single project.
    /// </summary>
    private static readonly HashSet<string> AllowListedHandlers = new()
    {
        "CreateProjectCommandHandler",
        "ListProjectsQueryHandler"
    };

    public static TheoryData<Type> ProjectScopedHandlers()
    {
        var handlerTypes = typeof(IApplicationDbContext).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericType: false })
            .Where(t => t.GetInterfaces().Any(IsRequestHandlerForProjectScopedRequest))
            .OrderBy(t => t.Name)
            .ToList();

        var data = new TheoryData<Type>();
        foreach (var type in handlerTypes)
        {
            data.Add(type);
        }

        Assert.True(data.Count >= 20, "Expected to discover the project-scoped handler suite.");
        return data;
    }

    [Theory]
    [MemberData(nameof(ProjectScopedHandlers))]
    public void ProjectScopedHandler_MustDependOnProjectAccessService(Type handlerType)
    {
        if (AllowListedHandlers.Contains(handlerType.Name))
        {
            return;
        }

        var dependsOnAccessService = handlerType
            .GetConstructors()
            .Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IProjectAccessService)));

        Assert.True(
            dependsOnAccessService,
            $"{handlerType.Name} touches project-scoped data but does not depend on " +
            $"{nameof(IProjectAccessService)}. Controller policies are not project-scoped; " +
            "handlers must enforce project membership themselves.");
    }

    private static bool IsRequestHandlerForProjectScopedRequest(Type interfaceType)
    {
        if (!interfaceType.IsGenericType)
        {
            return false;
        }

        var definition = interfaceType.GetGenericTypeDefinition();
        if (definition != typeof(IRequestHandler<,>) && definition != typeof(IRequestHandler<>))
        {
            return false;
        }

        var requestType = interfaceType.GetGenericArguments()[0];

        return ProjectScopedFeaturePrefixes.Any(prefix =>
            requestType.Namespace?.StartsWith(prefix, StringComparison.Ordinal) == true);
    }
}
