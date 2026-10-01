using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly TaskFlowDbContext _context;

    public TaskRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public Task<TaskItem?> GetByIdAsync(int id) =>
        _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

    public Task<List<TaskItem>> GetFilteredAsync(int? projectId, TaskItemStatus? status)
    {
        var query = _context.Tasks.AsQueryable();

        if (projectId is not null)
        {
            query = query.Where(t => t.ProjectId == projectId.Value);
        }

        if (status is not null)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return query.OrderBy(t => t.Id).ToListAsync();
    }

    public async Task AddAsync(TaskItem task) =>
        await _context.Tasks.AddAsync(task);

    public void Remove(TaskItem task) =>
        _context.Tasks.Remove(task);

    public Task SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
