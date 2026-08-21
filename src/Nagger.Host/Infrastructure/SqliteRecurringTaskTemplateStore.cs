using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteRecurringTaskTemplateStore(NaggerDbContext dbContext)
    : IRecurringTaskTemplateStore
{
    public async ValueTask<RecurringTaskTemplate> AddAsync(
        RecurringTaskTemplate template,
        CancellationToken cancellationToken
    )
    {
        var entity = new RecurringTaskTemplateEntity
        {
            Title = template.Title,
            StartDate = template.StartDate,
            RecurrenceEvery = template.Recurrence.Every,
            RecurrenceUnit = template.Recurrence.Unit.ToString(),
            ReminderPolicy = template.ReminderPolicy.ToContractValue(),
            Status = template.Status.ToContractValue(),
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            CancelledAt = template.CancelledAt,
        };

        await dbContext.RecurringTaskTemplates.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return template with
        {
            Id = entity.Id,
        };
    }

    public async ValueTask<RecurringTaskTemplate?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken
    )
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

    public async ValueTask UpdateAsync(
        RecurringTaskTemplate template,
        CancellationToken cancellationToken
    )
    {
        var entity =
            await dbContext.RecurringTaskTemplates.FindAsync(
                new object[] { template.Id },
                cancellationToken: cancellationToken
            ) ?? throw new RecurringTaskNotFoundException(template.Id);

        entity.Title = template.Title;
        entity.StartDate = template.StartDate;
        entity.RecurrenceEvery = template.Recurrence.Every;
        entity.RecurrenceUnit = template.Recurrence.Unit.ToString();
        entity.ReminderPolicy = template.ReminderPolicy.ToContractValue();
        entity.Status = template.Status.ToContractValue();
        entity.UpdatedAt = template.UpdatedAt;
        entity.CancelledAt = template.CancelledAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(
        CancellationToken cancellationToken
    )
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
                Unit: Enum.Parse<RecurrenceUnit>(entity.RecurrenceUnit)
            ),
            ReminderPolicy: ReminderPolicies.FromContractValue(entity.ReminderPolicy),
            Status: RecurringTaskStatuses.FromContractValue(entity.Status),
            CreatedAt: entity.CreatedAt,
            UpdatedAt: entity.UpdatedAt,
            CancelledAt: entity.CancelledAt
        );
}
