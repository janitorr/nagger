namespace Nagger.Core.Tasks;

public interface ITaskStore
{
    ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken);

    ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken);

    ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TaskItem>> GetOpenOneShotTasksAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TaskItem>> GetByRecurringTaskIdAsync(long recurringTaskId, CancellationToken cancellationToken);
}

public interface IRecurringTaskTemplateStore
{
    ValueTask<RecurringTaskTemplate> AddAsync(RecurringTaskTemplate template, CancellationToken cancellationToken);

    ValueTask<RecurringTaskTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken);

    ValueTask UpdateAsync(RecurringTaskTemplate template, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(CancellationToken cancellationToken);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    TimeZoneInfo TimeZone { get; }
}
