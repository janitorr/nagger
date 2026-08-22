using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public interface ITaskStore
{
    ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken);

    ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken);

    ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TaskItem>> GetOpenOneShotTasksAsync(CancellationToken cancellationToken);
}

public interface IRecurringTaskTemplateStore
{
    ValueTask<RecurringTaskTemplate> AddAsync(RecurringTaskTemplate recurringTemplate, CancellationToken cancellationToken);

    ValueTask<RecurringTaskTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken);

    ValueTask UpdateAsync(RecurringTaskTemplate recurringTemplate, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(CancellationToken cancellationToken);
}

public interface IRecurringTaskInstanceStore
{
    ValueTask<RecurringTaskInstance> AddAsync(RecurringTaskInstance instance, CancellationToken cancellationToken);

    ValueTask<RecurringTaskInstance?> GetByIdAsync(long id, CancellationToken cancellationToken);

    ValueTask UpdateAsync(RecurringTaskInstance instance, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<RecurringTaskInstance>> GetActiveAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<RecurringTaskInstance>> GetByTemplateIdAsync(
        long recurringTaskId,
        CancellationToken cancellationToken
    );
}
