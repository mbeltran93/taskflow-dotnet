using Moq;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.UnitTests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(_projectRepository.Object, _userRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_Persists_WhenOwnerExists()
    {
        _userRepository.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(new User { Id = 7 });

        Project? added = null;
        _projectRepository.Setup(r => r.AddAsync(It.IsAny<Project>()))
            .Callback<Project>(p => added = p)
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(new CreateProjectRequest("Apollo", "Moon mission"), ownerId: 7);

        Assert.Equal("Apollo", result.Name);
        Assert.Equal(7, result.OwnerId);
        Assert.NotNull(added);
        _projectRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenOwnerDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.CreateAsync(new CreateProjectRequest("Apollo", null), ownerId: 999));

        _projectRepository.Verify(r => r.AddAsync(It.IsAny<Project>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesFields_WhenProjectExists()
    {
        var project = new Project { Id = 3, Name = "Old", Description = "Old desc", OwnerId = 1 };
        _projectRepository.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(project);

        var result = await _sut.UpdateAsync(3, new UpdateProjectRequest("New", "New desc"));

        Assert.Equal("New", result.Name);
        Assert.Equal("New desc", result.Description);
        _projectRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenProjectDoesNotExist()
    {
        _projectRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(42));
    }
}
