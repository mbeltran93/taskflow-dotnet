using System.Net.Http.Json;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.DTOs.Users;

namespace TaskFlow.IntegrationTests;

internal static class TestAuthHelper
{
    /// <summary>Registers a brand-new user (unique email) and logs in, returning (userId, jwt).</summary>
    public static async Task<(int UserId, string Token)> RegisterAndLoginAsync(HttpClient client, string? namePrefix = null)
    {
        var unique = Guid.NewGuid().ToString("N")[..10];
        var email = $"{namePrefix ?? "user"}-{unique}@taskflow.test";
        const string password = "SuperSecret123";

        var createResponse = await client.PostAsJsonAsync("/api/users",
            new CreateUserRequest($"Test User {unique}", email, password));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<UserResponse>();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (created!.Id, login!.Token);
    }
}
