using Moq;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Security;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_userRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);
    }

    [Fact]
    public async Task LoginAsync_ReturnsToken_WhenCredentialsAreValid()
    {
        var user = new User { Id = 1, Email = "ada@test.com", PasswordHash = "hashed" };
        _userRepository.Setup(r => r.GetByEmailAsync("ada@test.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("plain", "hashed")).Returns(true);
        _jwtTokenGenerator.Setup(j => j.GenerateToken(user)).Returns("jwt-token");

        var result = await _sut.LoginAsync(new LoginRequest("ada@test.com", "plain"));

        Assert.Equal("jwt-token", result.Token);
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenUserDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(new LoginRequest("missing@test.com", "plain")));
    }

    [Fact]
    public async Task LoginAsync_Throws_WhenPasswordIsWrong()
    {
        var user = new User { Id = 1, Email = "ada@test.com", PasswordHash = "hashed" };
        _userRepository.Setup(r => r.GetByEmailAsync("ada@test.com")).ReturnsAsync(user);
        _passwordHasher.Setup(h => h.Verify("wrong", "hashed")).Returns(false);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.LoginAsync(new LoginRequest("ada@test.com", "wrong")));

        _jwtTokenGenerator.Verify(j => j.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
