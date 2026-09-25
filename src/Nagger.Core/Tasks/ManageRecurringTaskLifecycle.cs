using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record CompleteRecurringTaskResult(
    RecurringTaskInstance CompletedInstance,
    RecurringTaskInstance NextInstance
);

public sealed record CompleteRecurringTaskCommand(long Id) : ICommand<CompleteRecurringTaskResult>;

public sealed record PauseRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;

public sealed record ResumeRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;

public sealed record CancelRecurringTaskCommand(long Id) : ICommand<RecurringTaskTemplate>;

public sealed class CompleteRecurringTaskHandler(IRecurringTaskTemplateStore recurringStore, TimeProvider timeProvider)
    : ICommandHandler<CompleteRecurringTaskCommand, CompleteRecurringTaskResult>
{
    public async ValueTask<CompleteRecurringTaskResult> Handle(
        CompleteRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        var now = timeProvider.GetUtcNow();
        var updated = template.Complete(now, timeProvider.LocalTimeZone);
        var persisted = await recurringStore.UpdateAsync(updated, cancellationToken);

        return new CompleteRecurringTaskResult(
            CompletedInstance: persisted.Instances.First(x => x.Status == RecurringTaskInstanceStatus.Done),
            NextInstance: persisted.Instances.First(x => x.Status == RecurringTaskInstanceStatus.Active)
        );
    }
}

public sealed class PauseRecurringTaskHandler(IRecurringTaskTemplateStore recurringStore, TimeProvider timeProvider)
    : ICommandHandler<PauseRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(
        PauseRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        return await recurringStore.UpdateAsync(template.Pause(timeProvider.GetUtcNow()), cancellationToken);
    }
}

public sealed class ResumeRecurringTaskHandler(IRecurringTaskTemplateStore recurringStore, TimeProvider timeProvider)
    : ICommandHandler<ResumeRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(
        ResumeRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        return await recurringStore.UpdateAsync(template.Resume(timeProvider.GetUtcNow()), cancellationToken);
    }
}

public sealed class CancelRecurringTaskHandler(IRecurringTaskTemplateStore recurringStore, TimeProvider timeProvider)
    : ICommandHandler<CancelRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(
        CancelRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var template =
            await recurringStore.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new RecurringTaskNotFoundException(command.Id);

        return await recurringStore.UpdateAsync(template.Cancel(timeProvider.GetUtcNow()), cancellationToken);
    }
}
