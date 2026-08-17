using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Rules;

namespace TaskManagement.Tests.Unit;

public class TaskStatusTransitionsTests
{
    [Theory]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.InReview)]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.InReview)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.InReview, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Cancelled)]
    [InlineData(TaskItemStatus.InReview, TaskItemStatus.Cancelled)]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Cancelled)]
    public void IsForward_IsTrue_ForProgressTransitions(TaskItemStatus from, TaskItemStatus to)
    {
        Assert.True(TaskStatusTransitions.IsForward(from, to));
        Assert.False(TaskStatusTransitions.IsBackward(from, to));
        Assert.False(TaskStatusTransitions.IsSame(from, to));
    }

    [Theory]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.InReview, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InReview, TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InReview)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Cancelled)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Done)]
    public void IsBackward_IsTrue_ForReworkTransitions(TaskItemStatus from, TaskItemStatus to)
    {
        Assert.True(TaskStatusTransitions.IsBackward(from, to));
        Assert.False(TaskStatusTransitions.IsForward(from, to));
        Assert.False(TaskStatusTransitions.IsSame(from, to));
    }

    [Theory]
    [InlineData(TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InReview)]
    [InlineData(TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Cancelled)]
    public void IsSame_IsTrue_OnlyForIdenticalStatus(TaskItemStatus status)
    {
        Assert.True(TaskStatusTransitions.IsSame(status, status));
        Assert.False(TaskStatusTransitions.IsForward(status, status));
        Assert.False(TaskStatusTransitions.IsBackward(status, status));
    }
}
