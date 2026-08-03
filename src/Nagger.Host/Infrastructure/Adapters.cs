using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;

namespace Nagger.Host.Infrastructure;

public sealed class SqliteTaskStore(NaggerDbContext database) : ITaskStore
{
    public async ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        var entity = new TaskEntity { Title = task.Title, DueAt = task.DueAt, ReminderPolicy = task.ReminderPolicy.ToContractValue(), CreatedAt = task.CreatedAt, UpdatedAt = task.UpdatedAt, LastReminderAt = task.LastReminderAt };
        database.Tasks.Add(entity);
        await database.SaveChangesAsync(cancellationToken);
        return ToModel(entity);
    }

    public async ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken) =>
        await database.Tasks.AsNoTracking().OrderBy(x => x.Id).Select(x => new TaskItem(x.Id, x.Title, x.DueAt, x.ReminderPolicy == "once" ? ReminderPolicy.Once : x.ReminderPolicy == "weekly-until-done" ? ReminderPolicy.WeeklyUntilDone : ReminderPolicy.None, x.CreatedAt, x.UpdatedAt, x.LastReminderAt)).ToListAsync(cancellationToken);

    private static TaskItem ToModel(TaskEntity entity) => new(entity.Id, entity.Title, entity.DueAt, entity.ReminderPolicy == "once" ? ReminderPolicy.Once : entity.ReminderPolicy == "weekly-until-done" ? ReminderPolicy.WeeklyUntilDone : ReminderPolicy.None, entity.CreatedAt, entity.UpdatedAt, entity.LastReminderAt);
}

public sealed class ConfiguredClock(IConfiguration configuration) : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(configuration["Nagger:TimeZone"] ?? "Europe/Helsinki");
}
