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
    ValueTask<RecurringTaskTemplate> AddAsync(
        RecurringTaskTemplate recurringTemplate,
        CancellationToken cancellationToken
    );

    ValueTask<RecurringTaskTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken);

    ValueTask<RecurringTaskTemplate> UpdateAsync(
        RecurringTaskTemplate recurringTemplate,
        CancellationToken cancellationToken
    );

    ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(CancellationToken cancellationToken);
}

public interface IRecurringTaskInstanceReader
{
    ValueTask<IReadOnlyList<RecurringTaskInstance>> GetActiveAsync(CancellationToken cancellationToken);
}

public interface IShoppingItemStore
{
    ValueTask<ShoppingItem> AddAsync(ShoppingItem item, CancellationToken cancellationToken);

    ValueTask<ShoppingItem?> GetByNameAsync(string name, CancellationToken cancellationToken);

    ValueTask RemoveAsync(ShoppingItem item, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<ShoppingItem>> GetAllAsync(CancellationToken cancellationToken);
}
