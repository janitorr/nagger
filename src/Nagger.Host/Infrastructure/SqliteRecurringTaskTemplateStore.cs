using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteRecurringTaskTemplateStore(NaggerDbContext dbContext) : IRecurringTaskTemplateStore
{
    public async ValueTask<RecurringTaskTemplate> AddAsync(
        RecurringTaskTemplate recurringTemplate,
        CancellationToken cancellationToken
    )
    {
        var entity = new RecurringTaskTemplateEntity
        {
            Title = recurringTemplate.Title,
            StartDate = recurringTemplate.StartDate,
            RecurrenceEvery = recurringTemplate.Recurrence.Every,
            RecurrenceUnit = recurringTemplate.Recurrence.Unit.ToContractValue(),
            Status = recurringTemplate.Status.ToContractValue(),
            CreatedAt = recurringTemplate.CreatedAt,
            UpdatedAt = recurringTemplate.UpdatedAt,
            CancelledAt = recurringTemplate.CancelledAt,
        };

        await dbContext.RecurringTaskTemplates.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return recurringTemplate with
        {
            Id = entity.Id,
        };
    }

    public async ValueTask<RecurringTaskTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.RecurringTaskTemplates.FindAsync(
            new object[] { id },
            cancellationToken: cancellationToken
        );

        if (entity == null)
        {
            return null;
        }

        return ToModel(entity);
    }

    public async ValueTask UpdateAsync(RecurringTaskTemplate recurringTemplate, CancellationToken cancellationToken)
    {
        var entity =
            await dbContext.RecurringTaskTemplates.FindAsync(
                new object[] { recurringTemplate.Id },
                cancellationToken: cancellationToken
            ) ?? throw new RecurringTaskNotFoundException(recurringTemplate.Id);

        entity.Title = recurringTemplate.Title;
        entity.StartDate = recurringTemplate.StartDate;
        entity.RecurrenceEvery = recurringTemplate.Recurrence.Every;
        entity.RecurrenceUnit = recurringTemplate.Recurrence.Unit.ToContractValue();
        entity.Status = recurringTemplate.Status.ToContractValue();
        entity.UpdatedAt = recurringTemplate.UpdatedAt;
        entity.CancelledAt = recurringTemplate.CancelledAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await dbContext
            .RecurringTaskTemplates.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToList();
    }

    private static RecurringTaskTemplate ToModel(RecurringTaskTemplateEntity entity) =>
        new(
            Id: entity.Id,
            Title: entity.Title,
            StartDate: entity.StartDate,
            Recurrence: new RecurrenceRule(
                Every: entity.RecurrenceEvery,
                Unit: RecurrenceUnits.FromContractValue(entity.RecurrenceUnit)
            ),
            Status: RecurringTaskStatuses.FromContractValue(entity.Status),
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt,
            CancelledAt: entity.CancelledAt
        );
}
