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

public sealed class CompleteOneShotTaskHandler(ITaskStore store, IClock clock, IRecurringTaskTemplateStore recurringStore) : ICommandHandler<CompleteOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CompleteOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);
        var updated = task.Complete(clock.UtcNow);
        await store.UpdateAsync(updated, cancellationToken);

        // Handle recurring task instance completion
        if (task.RecurringTaskId.HasValue)
        {
            var template = await recurringStore.GetByIdAsync(task.RecurringTaskId.Value, cancellationToken)
                ?? throw new RecurringTaskNotFoundException(task.RecurringTaskId.Value);

            // Calculate next due date
            var nextDueDate = RecurrenceCalculator.CalculateNextDue(
                DateOnly.FromDateTime(updated.CompletedAt!.Value.Date),
                template.Recurrence);

            // Create next instance
            var nextInstance = new TaskItem(
                Id: 0,
                Title: template.Title,
                DueAt: nextDueDate.ToDateTimeOffset(clock.TimeZone),
                ReminderPolicy: template.ReminderPolicy,
                CreatedAt: clock.UtcNow,
                UpdatedAt: clock.UtcNow,
                Status: OneShotTaskStatus.Active,
                RecurringTaskId: template.Id);

            await store.AddAsync(nextInstance, cancellationToken);
        }

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
