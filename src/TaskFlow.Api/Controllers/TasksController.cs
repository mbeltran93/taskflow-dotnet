using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Lists tasks, optionally filtered by projectId and/or status (TODO | IN_PROGRESS | DONE).
    /// Public (read-only).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TaskResponse>>> GetFiltered(
        [FromQuery] int? projectId, [FromQuery] string? status)
    {
        return Ok(await _taskService.GetFilteredAsync(projectId, status));
    }

    /// <summary>Gets a single task by id. Public (read-only).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> GetById(int id)
    {
        return Ok(await _taskService.GetByIdAsync(id));
    }

    /// <summary>Creates a task inside a project. Requires JWT.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request)
    {
        var created = await _taskService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates a task's editable fields. Requires JWT.</summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> Update(int id, [FromBody] UpdateTaskRequest request)
    {
        return Ok(await _taskService.UpdateAsync(id, request));
    }

    /// <summary>Moves a task between TODO / IN_PROGRESS / DONE. Requires JWT.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize]
    [ProducesResponseType(typeof(TaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        return Ok(await _taskService.UpdateStatusAsync(id, request));
    }

    /// <summary>Deletes a task. Requires JWT.</summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _taskService.DeleteAsync(id);
        return NoContent();
    }
}
