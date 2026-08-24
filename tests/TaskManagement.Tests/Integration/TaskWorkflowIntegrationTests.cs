using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace TaskManagement.Tests.Integration;

/// <summary>
/// End-to-end API workflow: register -> login -> create project -> add member ->
/// create task -> assign -> label -> comment -> filter -> sort -> paginate.
/// </summary>
public class TaskWorkflowIntegrationTests : IClassFixture<TaskManagementApiFactory>, IAsyncLifetime
{
    private const string Password = "Passw0rd!123";

    private readonly TaskManagementApiFactory _factory;
    private readonly HttpClient _client;

    public TaskWorkflowIntegrationTests(TaskManagementApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task FullWorkflow_FromRegistrationToPaginatedFilteredTaskList()
    {
        // Register and log in an owner and a member.
        var ownerEmail = $"owner-{Guid.NewGuid():N}@tests.local";
        var memberEmail = $"member-{Guid.NewGuid():N}@tests.local";

        var ownerId = await RegisterAsync(ownerEmail, "Owner");
        var memberId = await RegisterAsync(memberEmail, "Member");

        var ownerToken = await LoginAsync(ownerEmail);
        var memberToken = await LoginAsync(memberEmail);

        // /users/me reflects the authenticated identity.
        var me = await GetJsonAsync("/api/users/me", ownerToken);
        Assert.Equal(ownerEmail, (string?)me!["email"]);

        // Owner creates a project (automatically becomes Owner member).
        var project = await PostJsonAsync("/api/projects",
            new { name = "Integration Project" }, ownerToken, HttpStatusCode.Created);
        var projectId = (string?)project!["id"]!;

        // Owner adds the second user as a Member.
        await PostJsonAsync($"/api/projects/{projectId}/members",
            new { userId = memberId, role = "Member" }, ownerToken, HttpStatusCode.Created);

        // Member creates a task in the project.
        var task = await PostJsonAsync("/api/tasks",
            new { projectId, title = "First task", status = "Todo", priority = "Medium" },
            memberToken, HttpStatusCode.Created);
        var taskId = (string?)task!["id"]!;
        Assert.Equal("Todo", (string?)task["status"]);

        // Member assigns the task to the owner.
        var assigned = await PatchJsonAsync($"/api/tasks/{taskId}/assignee",
            new { userId = ownerId }, memberToken, HttpStatusCode.OK);
        Assert.Equal("Owner User", (string?)assigned!["assignedTo"]!["name"]);

        // Member sets a due date via the dedicated PATCH endpoint.
        var dueDateChanged = await PatchJsonAsync($"/api/tasks/{taskId}/due-date",
            new { dueDate = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-dd") }, memberToken, HttpStatusCode.OK);
        Assert.NotNull(dueDateChanged!["dueDate"]);

        // Owner creates a label and attaches it to the task.
        var label = await PostJsonAsync($"/api/projects/{projectId}/labels",
            new { name = "Bug", color = "#EF4444" }, ownerToken, HttpStatusCode.Created);
        var labelId = (string?)label!["id"]!;

        await PostJsonAsync($"/api/tasks/{taskId}/labels/{labelId}", null, ownerToken, HttpStatusCode.Created);

        // Member comments on the task.
        await PostJsonAsync($"/api/tasks/{taskId}/comments",
            new { content = "First comment" }, memberToken, HttpStatusCode.Created);

        var comments = await GetJsonAsync($"/api/tasks/{taskId}/comments", memberToken);
        Assert.Single(comments!["items"]!.AsArray());
        Assert.Equal(1, (int?)comments["totalCount"]);

        // Two more tasks so sorting and pagination have data to work with.
        await PostJsonAsync("/api/tasks",
            new { projectId, title = "High task", status = "Todo", priority = "High" },
            memberToken, HttpStatusCode.Created);
        await PostJsonAsync("/api/tasks",
            new { projectId, title = "Low task", status = "Todo", priority = "Low" },
            memberToken, HttpStatusCode.Created);

        // Filter by project and status.
        var filtered = await GetJsonAsync($"/api/tasks?projectId={projectId}&status=Todo", ownerToken);
        Assert.Equal(3, (int?)filtered!["totalCount"]);

        // Search within title/description.
        var searched = await GetJsonAsync($"/api/tasks?projectId={projectId}&search=High", ownerToken);
        Assert.Equal(1, (int?)searched!["totalCount"]);

        // Sort by priority descending.
        var sorted = await GetJsonAsync(
            $"/api/tasks?projectId={projectId}&sortBy=priority&sortDirection=desc", ownerToken);
        var priorities = sorted!["items"]!.AsArray().Select(t => (string?)t!["priority"]).ToArray();
        Assert.Equal(new[] { "High", "Medium", "Low" }, priorities);

        // Paginate.
        var paged = await GetJsonAsync($"/api/tasks?projectId={projectId}&page=1&pageSize=2", ownerToken);
        Assert.Equal(3, (int?)paged!["totalCount"]);
        Assert.Equal(2, (int?)paged["totalPages"]);
        Assert.Equal(2, paged["items"]!.AsArray().Count);
        Assert.True((bool?)paged["hasNextPage"]);

        var page2 = await GetJsonAsync($"/api/tasks?projectId={projectId}&page=2&pageSize=2", ownerToken);
        Assert.Single(page2!["items"]!.AsArray());
        Assert.False((bool?)page2["hasNextPage"]);
        Assert.True((bool?)page2["hasPreviousPage"]);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"wrongpw-{Guid.NewGuid():N}@tests.local";
        await RegisterAsync(email, "Wrong");

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass!123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup-{Guid.NewGuid():N}@tests.local";
        await RegisterAsync(email, "Dup");

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { firstName = "Dup", lastName = "User", email, password = Password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ---- helpers ----

    private async Task<string> RegisterAsync(string email, string firstName)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { firstName, lastName = "User", email, password = Password });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonNode>();
        return body!["id"]!.GetValue<string>();
    }

    private async Task<string> LoginAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonNode>();
        return body!["accessToken"]!.GetValue<string>();
    }

    private static HttpRequestMessage WithAuth(HttpRequestMessage request, string? token)
    {
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private async Task<JsonNode?> GetJsonAsync(string url, string token)
    {
        var response = await _client.SendAsync(WithAuth(new HttpRequestMessage(HttpMethod.Get, url), token));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonNode>();
    }

    private async Task<JsonNode?> PostJsonAsync(
        string url,
        object? body,
        string token,
        HttpStatusCode expected)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        var response = await _client.SendAsync(WithAuth(request, token));

        Assert.Equal(expected, response.StatusCode);

        if (response.Content.Headers.ContentLength == 0)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<JsonNode>();
    }

    private async Task<JsonNode?> PatchJsonAsync(string url, object body, string token, HttpStatusCode expected)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };

        var response = await _client.SendAsync(WithAuth(request, token));

        Assert.Equal(expected, response.StatusCode);

        return await response.Content.ReadFromJsonAsync<JsonNode>();
    }
}
