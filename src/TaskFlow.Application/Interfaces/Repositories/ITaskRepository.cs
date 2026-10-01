using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(int id);
    Task<List<TaskItem>> GetFilteredAsync(int? projectId, TaskItemStatus? status);
    Task AddAsync(TaskItem task);
    void Remove(TaskItem task);
    Task SaveChangesAsync();
}
