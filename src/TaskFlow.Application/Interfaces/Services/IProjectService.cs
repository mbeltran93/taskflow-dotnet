using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.Application.Interfaces.Services;

public interface IProjectService
{
    Task<List<ProjectResponse>> GetAllAsync();
    Task<ProjectResponse> GetByIdAsync(int id);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, int ownerId);
    Task<ProjectResponse> UpdateAsync(int id, UpdateProjectRequest request);
    Task DeleteAsync(int id);
}
