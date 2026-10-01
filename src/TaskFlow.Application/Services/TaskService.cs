using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
    }

    public async Task<List<TaskResponse>> GetFilteredAsync(int? projectId, string? status)
    {
        TaskItemStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = TaskStatusMapper.Parse(status);
        }

        var tasks = await _taskRepository.GetFilteredAsync(projectId, parsedStatus);
        return tasks.Select(ToResponse).ToList();
    }

    public async Task<TaskResponse> GetByIdAsync(int id)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(TaskItem), id);
        return ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request)
    {
        _ = await _projectRepository.GetByIdAsync(request.ProjectId)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        if (request.AssigneeId is not null)
        {
            _ = await _userRepository.GetByIdAsync(request.AssigneeId.Value)
                ?? throw new NotFoundException(nameof(User), request.AssigneeId.Value);
        }

        var task = new TaskItem
        {
            Title = request.Title,
            Description = request.Description,
            ProjectId = request.ProjectId,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            Status = TaskItemStatus.Todo
        };

        await _taskRepository.AddAsync(task);
        await _taskRepository.SaveChangesAsync();

        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(int id, UpdateTaskRequest request)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        if (request.AssigneeId is not null)
        {
            _ = await _userRepository.GetByIdAsync(request.AssigneeId.Value)
                ?? throw new NotFoundException(nameof(User), request.AssigneeId.Value);
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.AssigneeId = request.AssigneeId;
        task.DueDate = request.DueDate;

        await _taskRepository.SaveChangesAsync();

        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateStatusAsync(int id, UpdateTaskStatusRequest request)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        task.Status = TaskStatusMapper.Parse(request.Status);

        await _taskRepository.SaveChangesAsync();

        return ToResponse(task);
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _taskRepository.GetByIdAsync(id)
            ?? throw new NotFoundException(nameof(TaskItem), id);

        _taskRepository.Remove(task);
        await _taskRepository.SaveChangesAsync();
    }

    private static TaskResponse ToResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        TaskStatusMapper.ToApiString(task.Status),
        task.ProjectId,
        task.AssigneeId,
        task.DueDate,
        task.CreatedAt);
}
