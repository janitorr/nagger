using Mediator;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Api;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this WebApplication app)
    {
        app.MapPost(
            "/tasks/one-shot",
            async (CreateOneShotTaskCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var task = await mediator.Send(command, cancellationToken);
                AppLog.TaskCreated(app.Logger, task.Id);
                return Results.Created($"/tasks/one-shot/{task.Id}", TaskResponse.From(task));
            }
        );

        app.MapGet(
            "/tasks/one-shot",
            async (IMediator mediator, CancellationToken cancellationToken) =>
                Results.Ok(
                    (await mediator.Send(new ListOpenOneShotTasksQuery(), cancellationToken))
                        .Select(TaskResponse.From)
                        .ToList()
                )
        );

        app.MapPost(
            "/tasks/{id:long}/complete",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                await RunLifecycleCommand(new CompleteOneShotTaskCommand(id), mediator, cancellationToken)
        );
        app.MapPost(
            "/tasks/{id:long}/pause",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                await RunLifecycleCommand(new PauseOneShotTaskCommand(id), mediator, cancellationToken)
        );
        app.MapPost(
            "/tasks/{id:long}/resume",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                await RunLifecycleCommand(new ResumeOneShotTaskCommand(id), mediator, cancellationToken)
        );
        app.MapPost(
            "/tasks/{id:long}/cancel",
            async (long id, IMediator mediator, CancellationToken cancellationToken) =>
                await RunLifecycleCommand(new CancelOneShotTaskCommand(id), mediator, cancellationToken)
        );
    }

    private static async Task<IResult> RunLifecycleCommand(
        ICommand<TaskItem> command,
        IMediator mediator,
        CancellationToken cancellationToken
    )
    {
        return Results.Ok(TaskResponse.From(await mediator.Send(command, cancellationToken)));
    }
}

public sealed record TaskResponse(
    long Id,
    string Title,
    string Type,
    string Status,
    DateTimeOffset DueAt,
    string ReminderPolicy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt
)
{
    public static TaskResponse From(TaskItem task) =>
        new(
            task.Id,
            task.Title,
            "one-shot",
            task.Status.ToContractValue(),
            task.DueAt,
            task.ReminderPolicy.ToContractValue(),
            task.CreatedAt,
            task.UpdatedAt,
            task.CompletedAt,
            task.CancelledAt
        );
}
