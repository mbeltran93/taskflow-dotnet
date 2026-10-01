using TaskFlow.Application.DTOs.Auth;

namespace TaskFlow.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
