using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;
    public int ProjectId { get; set; }
    public int? AssigneeId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
    public User? Assignee { get; set; }
}
