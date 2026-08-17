using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteRecurringTaskInstanceStore(NaggerDbContext database) : IRecurringTaskInstanceStore
{
    public async ValueTask<RecurringTaskInstance> AddAsync(RecurringTaskInstance instance, CancellationToken cancellationToken)
    {
        var entity = new RecurringTaskInstanceEntity { RecurringTaskId = instance.RecurringTaskId, Title = instance.Title, DueAt = instance.DueAt, ReminderPolicy = instance.ReminderPolicy.ToContractValue(), Status = instance.Status.ToContractValue(), CreatedAt = instance.CreatedAt, UpdatedAt = instance.UpdatedAt, CompletedAt = instance.CompletedAt, CancelledAt = instance.CancelledAt };
        database.RecurringTaskInstances.Add(entity);
        await database.SaveChangesAsync(cancellationToken);
        return ToModel(entity);
    }

    public async ValueTask<RecurringTaskInstance?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await database.RecurringTaskInstances.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : ToModel(entity);
    }

    public async ValueTask UpdateAsync(RecurringTaskInstance instance, CancellationToken cancellationToken)
    {
        var entity = await database.RecurringTaskInstances.SingleAsync(x => x.Id == instance.Id, cancellationToken);
        entity.Status = instance.Status.ToContractValue();
        entity.UpdatedAt = instance.UpdatedAt;
        entity.CompletedAt = instance.CompletedAt;
        entity.CancelledAt = instance.CancelledAt;
        await database.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<RecurringTaskInstance>> GetActiveAsync(CancellationToken cancellationToken) =>
        (await database.RecurringTaskInstances.AsNoTracking().Where(x => x.Status == "active").OrderBy(x => x.Id).ToListAsync(cancellationToken)).Select(ToModel).ToList();

    public async ValueTask<IReadOnlyList<RecurringTaskInstance>> GetByTemplateIdAsync(long recurringTaskId, CancellationToken cancellationToken) =>
        (await database.RecurringTaskInstances.AsNoTracking().Where(x => x.RecurringTaskId == recurringTaskId).OrderBy(x => x.Id).ToListAsync(cancellationToken)).Select(ToModel).ToList();

    private static RecurringTaskInstance ToModel(RecurringTaskInstanceEntity entity) => new(
        entity.Id,
        entity.RecurringTaskId,
        entity.Title,
        entity.DueAt,
        ReminderPolicies.FromContractValue(entity.ReminderPolicy),
        entity.CreatedAt,
        entity.UpdatedAt,
        RecurringTaskInstanceStatuses.FromContractValue(entity.Status),
        entity.CompletedAt,
        entity.CancelledAt);
}
