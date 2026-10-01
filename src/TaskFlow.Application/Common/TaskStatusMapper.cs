using TaskFlow.Application.Exceptions;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Common;

/// <summary>
/// The API contract exposes task status as the strings TODO / IN_PROGRESS / DONE
/// (matching the sibling backends in the portfolio), while the domain enum uses
/// regular C# member names. This maps between the two.
/// </summary>
public static class TaskStatusMapper
{
    public static string ToApiString(TaskItemStatus status) => status switch
    {
        TaskItemStatus.Todo => "TODO",
        TaskItemStatus.InProgress => "IN_PROGRESS",
        TaskItemStatus.Done => "DONE",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static TaskItemStatus Parse(string status) => status.Trim().ToUpperInvariant() switch
    {
        "TODO" => TaskItemStatus.Todo,
        "IN_PROGRESS" => TaskItemStatus.InProgress,
        "DONE" => TaskItemStatus.Done,
        _ => throw new ValidationAppException(
            $"Invalid status '{status}'. Allowed values are TODO, IN_PROGRESS, DONE.")
    };

    public static bool TryParse(string status, out TaskItemStatus parsed)
    {
        switch (status.Trim().ToUpperInvariant())
        {
            case "TODO":
                parsed = TaskItemStatus.Todo;
                return true;
            case "IN_PROGRESS":
                parsed = TaskItemStatus.InProgress;
                return true;
            case "DONE":
                parsed = TaskItemStatus.Done;
                return true;
            default:
                parsed = default;
                return false;
        }
    }
}
