using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteRecurringTaskInstanceReader(NaggerDbContext database) : IRecurringTaskInstanceReader
{
    public async ValueTask<IReadOnlyList<RecurringTaskInstance>> GetActiveAsync(CancellationToken cancellationToken) =>
        (
            await database
                .RecurringTaskInstances.AsNoTracking()
                .Where(x => x.Status == "active")
                .OrderBy(x => x.Id)
                .ToListAsync(cancellationToken)
        )
            .Select(ToModel)
            .ToList();

    private static RecurringTaskInstance ToModel(RecurringTaskInstanceEntity entity) =>
        new(
            entity.Id,
            entity.RecurringTaskId,
            entity.Title,
            entity.DueAt,
            entity.CreatedAt,
            entity.UpdatedAt,
            RecurringTaskInstanceStatuses.FromContractValue(entity.Status),
            entity.CompletedAt,
            entity.CancelledAt
        );
}
