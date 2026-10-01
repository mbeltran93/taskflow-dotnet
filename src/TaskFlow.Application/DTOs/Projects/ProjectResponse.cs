namespace TaskFlow.Application.DTOs.Projects;

public record ProjectResponse(
    int Id,
    string Name,
    string? Description,
    int OwnerId,
    DateTime CreatedAt);
