using Moq;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;

namespace TaskFlow.UnitTests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_taskRepository.Object, _projectRepository.Object, _userRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_DefaultsToTodo_WhenProjectExists()
    {
        _projectRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Project { Id = 1 });

        TaskItem? added = null;
        _taskRepository.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
            .Callback<TaskItem>(t => added = t)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateTaskRequest("Write tests", null, 1, null, null));

        Assert.Equal("TODO", result.Status);
        Assert.NotNull(added);
        Assert.Equal(TaskItemStatus.Todo, added!.Status);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenProjectDoesNotExist()
    {
        _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateAsync(new CreateTaskRequest("Write tests", null, 999, null, null)));
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenAssigneeDoesNotExist()
    {
        _projectRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Project { Id = 1 });
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateAsync(new CreateTaskRequest("Write tests", null, 1, 42, null)));
    }

    [Fact]
    public async Task UpdateStatusAsync_MovesTaskToInProgress()
    {
        var task = new TaskItem { Id = 5, ProjectId = 1, Status = TaskItemStatus.Todo, Title = "Task" };
        _taskRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(task);

        var result = await _sut.UpdateStatusAsync(5, new UpdateTaskStatusRequest("IN_PROGRESS"));

        Assert.Equal("IN_PROGRESS", result.Status);
        Assert.Equal(TaskItemStatus.InProgress, task.Status);
        _taskRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_Throws_WhenStatusIsInvalid()
    {
        var task = new TaskItem { Id = 5, ProjectId = 1, Status = TaskItemStatus.Todo, Title = "Task" };
        _taskRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(task);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => _sut.UpdateStatusAsync(5, new UpdateTaskStatusRequest("NOT_A_STATUS")));
    }

    [Fact]
    public async Task GetFilteredAsync_PassesParsedStatusToRepository()
    {
        _taskRepository
            .Setup(r => r.GetFilteredAsync(1, TaskItemStatus.Done))
            .ReturnsAsync(new List<TaskItem>
            {
                new() { Id = 1, ProjectId = 1, Status = TaskItemStatus.Done, Title = "Done task" }
            });

        var result = await _sut.GetFilteredAsync(1, "DONE");

        Assert.Single(result);
        Assert.Equal("DONE", result[0].Status);
        _taskRepository.Verify(r => r.GetFilteredAsync(1, TaskItemStatus.Done), Times.Once);
    }
}
