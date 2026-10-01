using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Interfaces.Services;

public interface ITaskService
{
    Task<List<TaskResponse>> GetFilteredAsync(int? projectId, string? status);
    Task<TaskResponse> GetByIdAsync(int id);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request);
    Task<TaskResponse> UpdateAsync(int id, UpdateTaskRequest request);
    Task<TaskResponse> UpdateStatusAsync(int id, UpdateTaskStatusRequest request);
    Task DeleteAsync(int id);
}
