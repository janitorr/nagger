using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record CompleteRecurringTaskCommand(long Id) : ICommand<RecurringTaskInstance>;

public sealed record PauseRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;

public sealed record ResumeRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;

public sealed record CancelRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;

public sealed class CompleteRecurringTaskHandler(
    IRecurringTaskTemplateStore recurringStore,
    IRecurringTaskInstanceStore instanceStore,
    TimeProvider timeProvider
) : ICommandHandler<CompleteRecurringTaskCommand, RecurringTaskInstance>
{
    public async ValueTask<RecurringTaskInstance> Handle(
        CompleteRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var instances = await instanceStore.GetByTemplateIdAsync(command.Id, cancellationToken);
        var current = instances.FirstOrDefault(x => x.Status == RecurringTaskInstanceStatus.Active);
        if (current is null)
            throw new ValidationException(
                new Dictionary<string, string[]> { ["status"] = ["Recurring task has no active instance to complete."] }
            );

        var completed = current.Complete(timeProvider.GetUtcNow());
        await instanceStore.UpdateAsync(completed, cancellationToken);

        var completedLocal = TimeZoneInfo.ConvertTime(completed.CompletedAt!.Value, timeProvider.LocalTimeZone);
        var nextDueDate = RecurrenceCalculator.CalculateNextDue(
            DateOnly.FromDateTime(completedLocal.Date),
            template.Recurrence
        );

        var nextInstance = new RecurringTaskInstance(
            Id: 0,
            RecurringTaskId: template.Id,
            Title: template.Title,
            DueAt: nextDueDate.ToDateTimeOffset(timeProvider.LocalTimeZone),
            ReminderPolicy: template.ReminderPolicy,
            CreatedAt: timeProvider.GetUtcNow(),
            UpdatedAt: timeProvider.GetUtcNow(),
            Status: RecurringTaskInstanceStatus.Active
        );

        await instanceStore.AddAsync(nextInstance, cancellationToken);

        return completed;
    }
}

public sealed class PauseRecurringTaskHandler(
    IRecurringTaskTemplateStore recurringStore,
    IRecurringTaskInstanceStore instanceStore,
    TimeProvider timeProvider
) : ICommandHandler<PauseRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(
        PauseRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var updated = template.Pause(timeProvider.GetUtcNow());
        await recurringStore.UpdateAsync(updated, cancellationToken);

        var instances = await instanceStore.GetByTemplateIdAsync(command.Id, cancellationToken);
        var current = instances.FirstOrDefault(x => x.Status == RecurringTaskInstanceStatus.Active);
        if (current is not null)
            await instanceStore.UpdateAsync(current.Pause(timeProvider.GetUtcNow()), cancellationToken);

        return updated;
    }
}

public sealed class ResumeRecurringTaskHandler(
    IRecurringTaskTemplateStore recurringStore,
    IRecurringTaskInstanceStore instanceStore,
    TimeProvider timeProvider
) : ICommandHandler<ResumeRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(
        ResumeRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var updated = template.Resume(timeProvider.GetUtcNow());
        await recurringStore.UpdateAsync(updated, cancellationToken);

        var instances = await instanceStore.GetByTemplateIdAsync(command.Id, cancellationToken);
        var current = instances.FirstOrDefault(x => x.Status == RecurringTaskInstanceStatus.Paused);
        if (current is not null)
            await instanceStore.UpdateAsync(current.Resume(timeProvider.GetUtcNow()), cancellationToken);

        return updated;
    }
}

public sealed class CancelRecurringTaskHandler(
    IRecurringTaskTemplateStore recurringStore,
    IRecurringTaskInstanceStore instanceStore,
    TimeProvider timeProvider
) : ICommandHandler<CancelRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(
        CancelRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var updated = template.Cancel(timeProvider.GetUtcNow());
        await recurringStore.UpdateAsync(updated, cancellationToken);

        var instances = await instanceStore.GetByTemplateIdAsync(command.Id, cancellationToken);
        foreach (
            var instance in instances.Where(x =>
                x.Status is RecurringTaskInstanceStatus.Active or RecurringTaskInstanceStatus.Paused
            )
        )
            await instanceStore.UpdateAsync(instance.Cancel(timeProvider.GetUtcNow()), cancellationToken);

        return updated;
    }
}
