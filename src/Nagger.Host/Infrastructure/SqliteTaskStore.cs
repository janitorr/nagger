using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteTaskStore(NaggerDbContext database) : ITaskStore
{
    public async ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var entity = new TaskEntity
        {
            Title = task.Title,
            DueAt = task.DueAt,
            ReminderPolicy = task.ReminderPolicy.ToContractValue(),
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            LastReminderAt = task.LastReminderAt,
            Status = task.Status.ToContractValue(),
            CompletedAt = task.CompletedAt,
            CancelledAt = task.CancelledAt,
        };
        database.Tasks.Add(entity);
        await database.SaveChangesAsync(cancellationToken);
        return ToModel(entity);
    }

    public async ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken) =>
        (
            await database
                .Tasks.AsNoTracking()
                .Where(x => x.Status == "active")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
        )
            .Select(ToModel)
            .ToList();

    public async ValueTask<IReadOnlyList<TaskItem>> GetOpenOneShotTasksAsync(CancellationToken cancellationToken) =>
        (
            await database
                .Tasks.AsNoTracking()
                .Where(x => x.Status == "active" || x.Status == "paused")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
        )
            .Select(ToModel)
            .ToList();

    public async ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await database.Tasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var entity = await database.Tasks.SingleAsync(x => x.Id == task.Id, cancellationToken);
        entity.Status = task.Status.ToContractValue();
        entity.UpdatedAt = task.UpdatedAt;
        entity.CompletedAt = task.CompletedAt;
        entity.CancelledAt = task.CancelledAt;
        await database.SaveChangesAsync(cancellationToken);
    }

    private static TaskItem ToModel(TaskEntity entity) =>
        new(
            entity.Id,
            entity.Title,
            entity.DueAt,
            entity.ReminderPolicy == "once" ? ReminderPolicy.Once
                : entity.ReminderPolicy == "weekly-until-done" ? ReminderPolicy.WeeklyUntilDone
                : ReminderPolicy.None,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.LastReminderAt,
            OneShotTaskStatuses.FromContractValue(entity.Status),
            entity.CompletedAt,
            entity.CancelledAt
        );
}
