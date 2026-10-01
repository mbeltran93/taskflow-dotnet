namespace TaskFlow.Application.DTOs.Tasks;

public record CreateTaskRequest(
    string Title,
    string? Description,
    int ProjectId,
    int? AssigneeId,
    DateTime? DueDate);
