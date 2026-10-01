using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id);
    Task<List<Project>> GetAllAsync();
    Task AddAsync(Project project);
    void Remove(Project project);
    Task SaveChangesAsync();
}
