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
        OneShotTaskLifecycle.TransitionAsync(command.Id, store, clock, static (task, now) => task.Complete(now), cancellationToken);
}

public sealed class PauseOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<PauseOneShotTaskCommand, TaskItem>
{
    public ValueTask<TaskItem> Handle(PauseOneShotTaskCommand command, CancellationToken cancellationToken) =>
        OneShotTaskLifecycle.TransitionAsync(command.Id, store, clock, static (task, now) => task.Pause(now), cancellationToken);
}

public sealed class ResumeOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<ResumeOneShotTaskCommand, TaskItem>
{
    public ValueTask<TaskItem> Handle(ResumeOneShotTaskCommand command, CancellationToken cancellationToken) =>
        OneShotTaskLifecycle.TransitionAsync(command.Id, store, clock, static (task, now) => task.Resume(now), cancellationToken);
}

public sealed class CancelOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<CancelOneShotTaskCommand, TaskItem>
{
    public ValueTask<TaskItem> Handle(CancelOneShotTaskCommand command, CancellationToken cancellationToken) =>
        OneShotTaskLifecycle.TransitionAsync(command.Id, store, clock, static (task, now) => task.Cancel(now), cancellationToken);
}

internal static class OneShotTaskLifecycle
{
    public static async ValueTask<TaskItem> TransitionAsync(long id, ITaskStore store, IClock clock, Func<TaskItem, DateTimeOffset, TaskItem> transition, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(id, cancellationToken) ?? throw new TaskNotFoundException(id);
        var updated = transition(task, clock.UtcNow);
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}
