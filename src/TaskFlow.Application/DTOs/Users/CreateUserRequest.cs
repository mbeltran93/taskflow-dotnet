namespace TaskFlow.Application.DTOs.Users;

/// <summary>
/// Public registration endpoint request. Not part of the minimal contract spec, but needed
/// so there is a way to create the accounts that later authenticate via /api/auth/login.
/// </summary>
public record CreateUserRequest(string Name, string Email, string Password);
