using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public ProjectService(IProjectRepository projectRepository, IUserRepository userRepository)
    {
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<List<ProjectResponse>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Select(ToResponse).ToList();
    }

    public async Task<ProjectResponse> GetByIdAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Project), id);
        return ToResponse(project);
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, int ownerId)
    {
        _ = await _userRepository.GetByIdAsync(ownerId)
            ?? throw new NotFoundException(nameof(User), ownerId);

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            OwnerId = ownerId
        };

        await _projectRepository.AddAsync(project);
        await _projectRepository.SaveChangesAsync();

        return ToResponse(project);
    }

    public async Task<ProjectResponse> UpdateAsync(int id, UpdateProjectRequest request)
    {
        var project = await _projectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Project), id);

        project.Name = request.Name;
        project.Description = request.Description;

        await _projectRepository.SaveChangesAsync();

        return ToResponse(project);
    }

    public async Task DeleteAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(Project), id);

        _projectRepository.Remove(project);
        await _projectRepository.SaveChangesAsync();
    }

    private static ProjectResponse ToResponse(Project project) =>
        new(project.Id, project.Name, project.Description, project.OwnerId, project.CreatedAt);
}
