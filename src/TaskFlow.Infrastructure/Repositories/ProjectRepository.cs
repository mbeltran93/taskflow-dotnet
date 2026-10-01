using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly TaskFlowDbContext _context;

    public ProjectRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public Task<Project?> GetByIdAsync(int id) =>
        _context.Projects.FirstOrDefaultAsync(p => p.Id == id);

    public Task<List<Project>> GetAllAsync() =>
        _context.Projects.OrderBy(p => p.Id).ToListAsync();

    public async Task AddAsync(Project project) =>
        await _context.Projects.AddAsync(project);

    public void Remove(Project project) =>
        _context.Projects.Remove(project);

    public Task SaveChangesAsync() =>
        _context.SaveChangesAsync();
}
