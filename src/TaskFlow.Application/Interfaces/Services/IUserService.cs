using TaskFlow.Application.DTOs.Users;

namespace TaskFlow.Application.Interfaces.Services;

public interface IUserService
{
    Task<List<UserResponse>> GetAllAsync();
    Task<UserResponse> GetByIdAsync(int id);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
}
