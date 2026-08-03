using System.Text.Json.Serialization;
using Mediator;
using Nagger.Core.Tasks;

namespace Nagger.Host.Api;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        app.MapPost("/tasks/one-shot", async (CreateTaskRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var task = await mediator.Send(new CreateOneShotTaskCommand(request.Title, request.DueAt, request.ReminderPolicy), cancellationToken);
            AppLog.TaskCreated(app.Logger, task.Id);
            return Results.Created($"/tasks/one-shot/{task.Id}", TaskResponse.From(task));
        });

        app.MapPost("/tasks/{id:long}/complete", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
            await RunLifecycleCommand(new CompleteOneShotTaskCommand(id), mediator, cancellationToken));
        app.MapPost("/tasks/{id:long}/pause", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
            await RunLifecycleCommand(new PauseOneShotTaskCommand(id), mediator, cancellationToken));
        app.MapPost("/tasks/{id:long}/resume", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
            await RunLifecycleCommand(new ResumeOneShotTaskCommand(id), mediator, cancellationToken));
        app.MapPost("/tasks/{id:long}/cancel", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
            await RunLifecycleCommand(new CancelOneShotTaskCommand(id), mediator, cancellationToken));
    }

    private static async Task<IResult> RunLifecycleCommand(ICommand<TaskItem> command, IMediator mediator, CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(TaskResponse.From(await mediator.Send(command, cancellationToken)));
        }
        catch (TaskNotFoundException)
        {
            return Results.NotFound();
        }
    }
}

public sealed record CreateTaskRequest(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("due_at")] string? DueAt,
    [property: JsonPropertyName("reminder_policy")] string? ReminderPolicy);

public sealed record TaskResponse(long Id, string Title, string Type, string Status,
    [property: JsonPropertyName("due_at")] DateTimeOffset DueAt,
    [property: JsonPropertyName("reminder_policy")] string ReminderPolicy,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("completed_at")] DateTimeOffset? CompletedAt,
    [property: JsonPropertyName("cancelled_at")] DateTimeOffset? CancelledAt)
{
    public static TaskResponse From(TaskItem task) => new(task.Id, task.Title, "one-shot", task.Status.ToContractValue(), task.DueAt, task.ReminderPolicy.ToContractValue(), task.CreatedAt, task.UpdatedAt, task.CompletedAt, task.CancelledAt);
}
