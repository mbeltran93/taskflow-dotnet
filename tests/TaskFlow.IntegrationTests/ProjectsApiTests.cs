using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskFlow.Application.DTOs.Projects;

namespace TaskFlow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class ProjectsApiTests
{
    private readonly HttpClient _client;

    public ProjectsApiTests(TaskFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("No Auth Project", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_Then_Get_RoundTrips()
    {
        var (ownerId, token) = await TestAuthHelper.RegisterAndLoginAsync(_client, "project-owner");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("Apollo", "Moon mission"));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(created);
        Assert.Equal("Apollo", created!.Name);
        Assert.Equal(ownerId, created.OwnerId);

        var getResponse = await _client.GetAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal(created.Id, fetched!.Id);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Update_ChangesNameAndDescription()
    {
        var (_, token) = await TestAuthHelper.RegisterAndLoginAsync(_client, "project-updater");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("Original", "Original desc"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/projects/{created!.Id}", new UpdateProjectRequest("Renamed", "Updated desc"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal("Updated desc", updated.Description);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Delete_RemovesProject_AndSubsequentGetReturnsNotFound()
    {
        var (_, token) = await TestAuthHelper.RegisterAndLoginAsync(_client, "project-deleter");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest("To Delete", null));
        var created = await createResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/projects/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/projects/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/api/projects/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
