namespace TaskFlow.Application.DTOs.Tasks;

public record TaskResponse(
    int Id,
    string Title,
    string? Description,
    string Status,
    int ProjectId,
    int? AssigneeId,
    DateTime? DueDate,
    DateTime CreatedAt);
