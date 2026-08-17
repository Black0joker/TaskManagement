using TaskManagement.Application.Features.Authentication.Register;
using TaskManagement.Application.Features.Tasks.CreateTask;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Tests.Unit;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator = new();

    [Fact]
    public async Task ValidCommand_Passes()
    {
        var command = new CreateTaskCommand(
            "project-1", "A valid title", "A description", TaskItemStatus.Todo, TaskItemPriority.Medium, null,
            DateTime.UtcNow.AddDays(3));

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MissingProjectId_Fails(string? projectId)
    {
        var command = new CreateTaskCommand(
            projectId!, "Title", null, TaskItemStatus.Todo, TaskItemPriority.Medium, null, null);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.ProjectId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task MissingTitle_Fails(string? title)
    {
        var command = new CreateTaskCommand(
            "project-1", title!, null, TaskItemStatus.Todo, TaskItemPriority.Medium, null, null);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Title));
    }

    [Fact]
    public async Task OverlongTitle_Fails()
    {
        var command = new CreateTaskCommand(
            "project-1", new string('x', 201), null, TaskItemStatus.Todo, TaskItemPriority.Medium, null, null);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task PastDueDate_Fails()
    {
        var command = new CreateTaskCommand(
            "project-1", "Title", null, TaskItemStatus.Todo, TaskItemPriority.Medium, null,
            DateTime.UtcNow.Date.AddDays(-2));

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.DueDate));
    }
}

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public async Task ValidRegistration_Passes()
    {
        var command = new RegisterCommand("Ada", "Lovelace", "ada@test.local", "Str0ng!Pass");

        var result = await _validator.ValidateAsync(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar1A")]
    [InlineData("Sh1!")]
    public async Task WeakPasswords_Fail(string password)
    {
        var command = new RegisterCommand("Ada", "Lovelace", "ada@test.local", password);

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Password));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b@c.com")]
    [InlineData("")]
    public async Task InvalidEmail_Fails(string email)
    {
        var command = new RegisterCommand("Ada", "Lovelace", email, "Str0ng!Pass");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Email));
    }
}
