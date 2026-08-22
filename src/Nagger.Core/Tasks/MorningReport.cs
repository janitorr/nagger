using System.Globalization;
using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record MorningReportQuery(string? Date) : IQuery<MorningReport>;

public sealed record MorningReport(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    DateOnly Date,
    MorningReportSummary Summary,
    IReadOnlyList<MorningReportItem> Items
);

public sealed record MorningReportSummary(int DueToday, int Overdue, int Upcoming);

public sealed record MorningReportItem(
    long Id,
    string Title,
    DateTimeOffset DueAt,
    string Type,
    string DueState,
    int? DaysOverdue,
    int? DaysUntilDue,
    string ReminderPolicy
);

public sealed class MorningReportHandler(ITaskStore store, IRecurringTaskInstanceStore instanceStore, IClock clock)
    : IQueryHandler<MorningReportQuery, MorningReport>
{
    public async ValueTask<MorningReport> Handle(MorningReportQuery query, CancellationToken cancellationToken)
    {
        if (
            !DateOnly.TryParseExact(
                query.Date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var reportDate
            )
        )
            throw new ValidationException(
                new Dictionary<string, string[]> { ["date"] = ["Date must use YYYY-MM-DD format."] }
            );

        var tasks = await store.GetActiveAsync(cancellationToken);
        var instances = await instanceStore.GetActiveAsync(cancellationToken);

        var dueToday = 0;
        var overdue = 0;
        var upcoming = 0;
        var items = new List<MorningReportItem>();
        foreach (var task in tasks)
            AddItem(
                items,
                ref dueToday,
                ref overdue,
                ref upcoming,
                reportDate,
                clock.TimeZone,
                task.Id,
                task.Title,
                task.DueAt,
                task.ReminderPolicy.ToContractValue(),
                "one-shot"
            );
        foreach (var instance in instances)
            AddItem(
                items,
                ref dueToday,
                ref overdue,
                ref upcoming,
                reportDate,
                clock.TimeZone,
                instance.RecurringTaskId,
                instance.Title,
                instance.DueAt,
                instance.ReminderPolicy.ToContractValue(),
                "recurring"
            );

        items = items.OrderBy(x => x.DueAt).ToList();

        return new MorningReport(
            "3",
            clock.UtcNow,
            reportDate,
            new MorningReportSummary(dueToday, overdue, upcoming),
            items
        );
    }

    private static void AddItem(
        List<MorningReportItem> items,
        ref int dueToday,
        ref int overdue,
        ref int upcoming,
        DateOnly reportDate,
        TimeZoneInfo timeZone,
        long id,
        string title,
        DateTimeOffset dueAt,
        string reminderPolicy,
        string type
    )
    {
        var itemDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(dueAt, timeZone).DateTime);
        var comparison = itemDate.CompareTo(reportDate);
        if (comparison == 0)
        {
            dueToday++;
            items.Add(new MorningReportItem(id, title, dueAt, type, "due_today", null, null, reminderPolicy));
        }
        else if (comparison < 0)
        {
            overdue++;
            items.Add(
                new MorningReportItem(
                    id,
                    title,
                    dueAt,
                    type,
                    "overdue",
                    reportDate.DayNumber - itemDate.DayNumber,
                    null,
                    reminderPolicy
                )
            );
        }
        else if (itemDate <= reportDate.AddDays(7))
        {
            upcoming++;
            items.Add(
                new MorningReportItem(
                    id,
                    title,
                    dueAt,
                    type,
                    "upcoming",
                    null,
                    itemDate.DayNumber - reportDate.DayNumber,
                    reminderPolicy
                )
            );
        }
    }
}
