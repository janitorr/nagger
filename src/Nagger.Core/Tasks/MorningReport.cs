using System.Globalization;
using Mediator;

namespace Nagger.Core.Tasks;

public sealed record MorningReportQuery(string? Date) : IQuery<MorningReport>;

public sealed record MorningReport(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    DateOnly Date,
    MorningReportSummary Summary,
    IReadOnlyList<MorningReportItem> Items);

public sealed record MorningReportSummary(int DueToday, int Overdue, int Upcoming);

public sealed record MorningReportItem(long Id, string Title, DateTimeOffset DueAt, string DueState, int? DaysOverdue, int? DaysUntilDue, string ReminderPolicy);

public sealed class MorningReportHandler(ITaskStore store, IClock clock)
    : IQueryHandler<MorningReportQuery, MorningReport>
{
    public async ValueTask<MorningReport> Handle(MorningReportQuery query, CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(query.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var reportDate))
            throw new ValidationException(new Dictionary<string, string[]> { ["date"] = ["Date must use YYYY-MM-DD format."] });

        var tasks = await store.GetActiveAsync(cancellationToken);
        var dueToday = 0;
        var overdue = 0;
        var upcoming = 0;
        var items = new List<MorningReportItem>();
        foreach (var task in tasks)
        {
            var taskDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(task.DueAt, clock.TimeZone).DateTime);
            var comparison = taskDate.CompareTo(reportDate);
            if (comparison == 0)
            {
                dueToday++;
                items.Add(ToItem(task, "due_today", null, null));
            }
            else if (comparison < 0)
            {
                overdue++;
                items.Add(ToItem(task, "overdue", reportDate.DayNumber - taskDate.DayNumber, null));
            }
            else if (taskDate <= reportDate.AddDays(7))
            {
                upcoming++;
                items.Add(ToItem(task, "upcoming", null, taskDate.DayNumber - reportDate.DayNumber));
            }
        }

        return new MorningReport("2", clock.UtcNow, reportDate, new MorningReportSummary(dueToday, overdue, upcoming), items);
    }

    private static MorningReportItem ToItem(TaskItem task, string dueState, int? daysOverdue, int? daysUntilDue) =>
        new(task.Id, task.Title, task.DueAt, dueState, daysOverdue, daysUntilDue, task.ReminderPolicy.ToContractValue());
}
