using Mediator;

namespace Nagger.Core.Tasks;

public sealed record CompleteOneShotTaskCommand(long Id) : ICommand<TaskItem>;
public sealed record PauseOneShotTaskCommand(long Id) : ICommand<TaskItem>;
public sealed record ResumeOneShotTaskCommand(long Id) : ICommand<TaskItem>;
public sealed record CancelOneShotTaskCommand(long Id) : ICommand<TaskItem>;

public sealed class TaskNotFoundException(long id) : Exception($"Task {id} was not found")
{
    public long Id { get; } = id;
}

public sealed class CompleteOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<CompleteOneShotTaskCommand, TaskItem>
{
    public ValueTask<TaskItem> Handle(CompleteOneShotTaskCommand command, CancellationToken cancellationToken) =>
        OneShotTaskLifecycle.TransitionAsync(command.Id, OneShotTaskStatus.Done, store, clock, cancellationToken);
}

public sealed class PauseOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<PauseOneShotTaskCommand, TaskItem>
{
    public ValueTask<TaskItem> Handle(PauseOneShotTaskCommand command, CancellationToken cancellationToken) =>
        OneShotTaskLifecycle.TransitionAsync(command.Id, OneShotTaskStatus.Paused, store, clock, cancellationToken);
}

public sealed class ResumeOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<ResumeOneShotTaskCommand, TaskItem>
{
    public ValueTask<TaskItem> Handle(ResumeOneShotTaskCommand command, CancellationToken cancellationToken) =>
        OneShotTaskLifecycle.TransitionAsync(command.Id, OneShotTaskStatus.Active, store, clock, cancellationToken);
}

public sealed class CancelOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<CancelOneShotTaskCommand, TaskItem>
{
    public ValueTask<TaskItem> Handle(CancelOneShotTaskCommand command, CancellationToken cancellationToken) =>
        OneShotTaskLifecycle.TransitionAsync(command.Id, OneShotTaskStatus.Cancelled, store, clock, cancellationToken);
}

internal static class OneShotTaskLifecycle
{
    public static async ValueTask<TaskItem> TransitionAsync(long id, OneShotTaskStatus target, ITaskStore store, IClock clock, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(id, cancellationToken) ?? throw new TaskNotFoundException(id);
        if (!IsAllowed(task.Status, target))
            throw new ValidationException(new Dictionary<string, string[]> { ["status"] = [$"Cannot transition a {task.Status.ToContractValue()} task to {target.ToContractValue()}."] });

        var now = clock.UtcNow;
        var updated = task with
        {
            Status = target,
            UpdatedAt = now,
            CompletedAt = target == OneShotTaskStatus.Done ? now : task.CompletedAt,
            CancelledAt = target == OneShotTaskStatus.Cancelled ? now : task.CancelledAt
        };
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }

    private static bool IsAllowed(OneShotTaskStatus current, OneShotTaskStatus target) => (current, target) switch
    {
        (OneShotTaskStatus.Active, OneShotTaskStatus.Paused or OneShotTaskStatus.Done or OneShotTaskStatus.Cancelled) => true,
        (OneShotTaskStatus.Paused, OneShotTaskStatus.Active or OneShotTaskStatus.Cancelled) => true,
        _ => false
    };
}
