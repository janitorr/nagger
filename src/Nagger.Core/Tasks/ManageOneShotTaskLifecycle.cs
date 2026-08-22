using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record CompleteOneShotTaskCommand(long Id) : ICommand<TaskItem>;

public sealed record PauseOneShotTaskCommand(long Id) : ICommand<TaskItem>;

public sealed record ResumeOneShotTaskCommand(long Id) : ICommand<TaskItem>;

public sealed record CancelOneShotTaskCommand(long Id) : ICommand<TaskItem>;

public sealed class TaskNotFoundException(long id) : Exception($"Task {id} was not found")
{
    public long Id { get; } = id;
}

public sealed class CompleteOneShotTaskHandler(ITaskStore store, TimeProvider timeProvider)
    : ICommandHandler<CompleteOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CompleteOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task =
            await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Complete(timeProvider.GetUtcNow());
        await store.UpdateAsync(updated, cancellationToken);

        return updated;
    }
}

public sealed class PauseOneShotTaskHandler(ITaskStore store, TimeProvider timeProvider)
    : ICommandHandler<PauseOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(PauseOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task =
            await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Pause(timeProvider.GetUtcNow());
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}

public sealed class ResumeOneShotTaskHandler(ITaskStore store, TimeProvider timeProvider)
    : ICommandHandler<ResumeOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(ResumeOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task =
            await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Resume(timeProvider.GetUtcNow());
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}

public sealed class CancelOneShotTaskHandler(ITaskStore store, TimeProvider timeProvider)
    : ICommandHandler<CancelOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CancelOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task =
            await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Cancel(timeProvider.GetUtcNow());
        await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}
