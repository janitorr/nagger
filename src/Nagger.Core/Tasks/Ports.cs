namespace Nagger.Core.Tasks;

public interface ITaskStore
{
    ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken);

    ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken);

    ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken);
}

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    TimeZoneInfo TimeZone { get; }
}
