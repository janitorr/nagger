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
    public async ValueTask<TaskItem> Handle(CompleteOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Complete(clock.UtcNow);
        await store.UpdateAsync(updated, cancellationToken);

        return updated;
    }
}

public sealed class PauseOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<PauseOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(PauseOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Pause(clock.UtcNow);
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}

public sealed class ResumeOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<ResumeOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(ResumeOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Resume(clock.UtcNow);
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}

public sealed class CancelOneShotTaskHandler(ITaskStore store, IClock clock) : ICommandHandler<CancelOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CancelOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Cancel(clock.UtcNow);
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}
