using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Rules;

/// <summary>
/// Classifies task status transitions.
/// Forward transitions move work toward completion (including skipping steps
/// and cancelling work that is still active).
/// Backward transitions undo progress: moving to an earlier status, resurrecting
/// a cancelled task, or cancelling already-completed work.
/// </summary>
public static class TaskStatusTransitions
{
    private static readonly IReadOnlyDictionary<TaskItemStatus, int> ChainPositions =
        new Dictionary<TaskItemStatus, int>
        {
            [TaskItemStatus.Todo] = 0,
            [TaskItemStatus.InProgress] = 1,
            [TaskItemStatus.InReview] = 2,
            [TaskItemStatus.Done] = 3
        };

    public static bool IsSame(TaskItemStatus from, TaskItemStatus to) => from == to;

    public static bool IsBackward(TaskItemStatus from, TaskItemStatus to)
    {
        if (from == to)
        {
            return false;
        }

        // Resurrecting a cancelled task is always a governance action.
        if (from == TaskItemStatus.Cancelled)
        {
            return true;
        }

        // Cancelling completed work undoes its completion.
        if (to == TaskItemStatus.Cancelled)
        {
            return from == TaskItemStatus.Done;
        }

        return ChainPositions[from] > ChainPositions[to];
    }

    public static bool IsForward(TaskItemStatus from, TaskItemStatus to) =>
        !IsSame(from, to) && !IsBackward(from, to);
}
