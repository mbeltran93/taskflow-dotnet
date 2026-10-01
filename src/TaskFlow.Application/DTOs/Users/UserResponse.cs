namespace TaskFlow.Application.DTOs.Users;

public record UserResponse(int Id, string Name, string Email, DateTime CreatedAt);
