using System.Net;
using System.Net.Http.Json;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.DTOs.Users;

namespace TaskFlow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class AuthApiTests
{
    private readonly HttpClient _client;

    public AuthApiTests(TaskFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Then_Login_ReturnsJwt()
    {
        var (_, token) = await TestAuthHelper.RegisterAndLoginAsync(_client, "login-flow");

        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(3, token.Split('.').Length); // header.payload.signature
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@taskflow.test";
        var request = new CreateUserRequest("Dup User", email, "SuperSecret123");

        var first = await _client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"wrongpw-{Guid.NewGuid():N}@taskflow.test";
        await _client.PostAsJsonAsync("/api/users", new CreateUserRequest("User", email, "SuperSecret123"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("nobody@taskflow.test", "whatever123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/users", new CreateUserRequest("", "not-an-email", "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
