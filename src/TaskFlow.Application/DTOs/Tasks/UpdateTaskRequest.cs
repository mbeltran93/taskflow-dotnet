namespace TaskFlow.Application.DTOs.Tasks;

public record UpdateTaskRequest(
    string Title,
    string? Description,
    int? AssigneeId,
    DateTime? DueDate);
