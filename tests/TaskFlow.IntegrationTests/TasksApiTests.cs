using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public class TasksApiTests
{
    private readonly HttpClient _client;

    public TasksApiTests(TaskFlowApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(int ProjectId, string Token)> CreateAuthenticatedProjectAsync(string prefix)
    {
        var (_, token) = await TestAuthHelper.RegisterAndLoginAsync(_client, prefix);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var projectResponse = await _client.PostAsJsonAsync(
            "/api/projects", new CreateProjectRequest($"Project for {prefix}", null));
        var project = await projectResponse.Content.ReadFromJsonAsync<ProjectResponse>();

        return (project!.Id, token);
    }

    [Fact]
    public async Task Create_DefaultsStatusToTodo()
    {
        var (projectId, _) = await CreateAuthenticatedProjectAsync("task-create");

        var response = await _client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskRequest("Write unit tests", "Cover the services", projectId, null, null));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var task = await response.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("TODO", task!.Status);
        Assert.Equal(projectId, task.ProjectId);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task PatchStatus_MovesTaskThroughWorkflow()
    {
        var (projectId, _) = await CreateAuthenticatedProjectAsync("task-status");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskRequest("Ship feature", null, projectId, null, null));
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var inProgress = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task!.Id}/status", new UpdateTaskStatusRequest("IN_PROGRESS"));
        Assert.Equal(HttpStatusCode.OK, inProgress.StatusCode);
        var inProgressBody = await inProgress.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("IN_PROGRESS", inProgressBody!.Status);

        var done = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest("DONE"));
        Assert.Equal(HttpStatusCode.OK, done.StatusCode);
        var doneBody = await done.Content.ReadFromJsonAsync<TaskResponse>();
        Assert.Equal("DONE", doneBody!.Status);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task PatchStatus_WithInvalidValue_ReturnsBadRequest()
    {
        var (projectId, _) = await CreateAuthenticatedProjectAsync("task-status-invalid");

        var createResponse = await _client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskRequest("Ship feature", null, projectId, null, null));
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();

        var response = await _client.PatchAsJsonAsync(
            $"/api/tasks/{task!.Id}/status", new UpdateTaskStatusRequest("NOT_A_REAL_STATUS"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetFiltered_ByProjectIdAndStatus_ReturnsOnlyMatchingTasks()
    {
        var (projectId, _) = await CreateAuthenticatedProjectAsync("task-filter");

        var t1 = await (await _client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskRequest("Task A", null, projectId, null, null)))
            .Content.ReadFromJsonAsync<TaskResponse>();
        var t2 = await (await _client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskRequest("Task B", null, projectId, null, null)))
            .Content.ReadFromJsonAsync<TaskResponse>();

        await _client.PatchAsJsonAsync($"/api/tasks/{t2!.Id}/status", new UpdateTaskStatusRequest("DONE"));

        var doneTasks = await _client.GetFromJsonAsync<List<TaskResponse>>(
            $"/api/tasks?projectId={projectId}&status=DONE");

        Assert.NotNull(doneTasks);
        Assert.Contains(doneTasks!, t => t.Id == t2.Id);
        Assert.DoesNotContain(doneTasks!, t => t.Id == t1!.Id);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Create_WithUnknownProject_ReturnsNotFound()
    {
        var (_, token) = await TestAuthHelper.RegisterAndLoginAsync(_client, "task-orphan");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskRequest("Orphan task", null, 999999, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task Delete_RemovesTask()
    {
        var (projectId, _) = await CreateAuthenticatedProjectAsync("task-delete");

        var created = await (await _client.PostAsJsonAsync(
            "/api/tasks", new CreateTaskRequest("Temporary task", null, projectId, null, null)))
            .Content.ReadFromJsonAsync<TaskResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/tasks/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
