using Moq;
using TaskFlow.Application.DTOs.Users;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Security;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.UnitTests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _sut = new UserService(_userRepository.Object, _passwordHasher.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenFound()
    {
        var user = new User { Id = 1, Name = "Ada", Email = "ada@test.com", PasswordHash = "hash" };
        _userRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        var result = await _sut.GetByIdAsync(1);

        Assert.Equal(1, result.Id);
        Assert.Equal("Ada", result.Name);
        Assert.Equal("ada@test.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenNotFound()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99));
    }

    [Fact]
    public async Task CreateAsync_HashesPasswordAndPersists_WhenEmailIsFree()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync("new@test.com")).ReturnsAsync(false);
        _passwordHasher.Setup(h => h.Hash("plain-pw")).Returns("hashed-pw");

        User? added = null;
        _userRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => added = u)
            .Returns(Task.CompletedTask);

        var request = new CreateUserRequest("New User", "new@test.com", "plain-pw");

        var result = await _sut.CreateAsync(request);

        Assert.Equal("New User", result.Name);
        Assert.Equal("new@test.com", result.Email);
        Assert.NotNull(added);
        Assert.Equal("hashed-pw", added!.PasswordHash);
        _userRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenEmailAlreadyExists()
    {
        _userRepository.Setup(r => r.ExistsByEmailAsync("dup@test.com")).ReturnsAsync(true);

        var request = new CreateUserRequest("Dup", "dup@test.com", "plain-pw");

        await Assert.ThrowsAsync<ConflictException>(() => _sut.CreateAsync(request));
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
    }
}
