using Mediator;

namespace Nagger.Core.Tasks;

public sealed record CompleteRecurringTaskCommand(long Id) : ICommand<TaskItem>;
public sealed record PauseRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;
public sealed record ResumeRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;
public sealed record CancelRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;

public sealed class CompleteRecurringTaskHandler(ITaskStore store, IClock clock, IRecurringTaskTemplateStore recurringStore)
    : ICommandHandler<CompleteRecurringTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CompleteRecurringTaskCommand command, CancellationToken cancellationToken)
    {
        var task = await store.GetByIdAsync(command.Id, cancellationToken) ?? throw new TaskNotFoundException(command.Id);

        if (!task.RecurringTaskId.HasValue)
            throw new ValidationException(new Dictionary<string, string[]> { ["id"] = ["Task is not a recurring instance."] });

        var updated = task.Complete(clock.UtcNow);
        await store.UpdateAsync(updated, cancellationToken);

        var template = await recurringStore.GetByIdAsync(task.RecurringTaskId.Value, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(task.RecurringTaskId.Value);

        var nextDueDate = RecurrenceCalculator.CalculateNextDue(
            DateOnly.FromDateTime(updated.CompletedAt!.Value.Date),
            template.Recurrence);

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

        return updated;
    }
}

public sealed class PauseRecurringTaskHandler(IRecurringTaskTemplateStore recurringStore, ITaskStore taskStore, IClock clock)
    : ICommandHandler<PauseRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(PauseRecurringTaskCommand command, CancellationToken cancellationToken)
    {
        var template = await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var updated = template.Pause(clock.UtcNow);
        await recurringStore.UpdateAsync(updated, cancellationToken);

        var instances = await taskStore.GetByRecurringTaskIdAsync(command.Id, cancellationToken);
        var current = instances.FirstOrDefault(x => x.Status == OneShotTaskStatus.Active);
        if (current is not null)
            await taskStore.UpdateAsync(current.Pause(clock.UtcNow), cancellationToken);

        return updated;
    }
}

public sealed class ResumeRecurringTaskHandler(IRecurringTaskTemplateStore recurringStore, ITaskStore taskStore, IClock clock)
    : ICommandHandler<ResumeRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(ResumeRecurringTaskCommand command, CancellationToken cancellationToken)
    {
        var template = await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var updated = template.Resume(clock.UtcNow);
        await recurringStore.UpdateAsync(updated, cancellationToken);

        var instances = await taskStore.GetByRecurringTaskIdAsync(command.Id, cancellationToken);
        var current = instances.FirstOrDefault(x => x.Status == OneShotTaskStatus.Paused);
        if (current is not null)
            await taskStore.UpdateAsync(current.Resume(clock.UtcNow), cancellationToken);

        return updated;
    }
}

public sealed class CancelRecurringTaskHandler(IRecurringTaskTemplateStore recurringStore, ITaskStore taskStore, IClock clock)
    : ICommandHandler<CancelRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(CancelRecurringTaskCommand command, CancellationToken cancellationToken)
    {
        var template = await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var updated = template.Cancel(clock.UtcNow);
        await recurringStore.UpdateAsync(updated, cancellationToken);

        var instances = await taskStore.GetByRecurringTaskIdAsync(command.Id, cancellationToken);
        foreach (var instance in instances.Where(x => x.Status is OneShotTaskStatus.Active or OneShotTaskStatus.Paused))
            await taskStore.UpdateAsync(instance.Cancel(clock.UtcNow), cancellationToken);

        return updated;
    }
}
