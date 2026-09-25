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
    int? DaysUntilDue
);

public sealed class MorningReportHandler(
    ITaskStore store,
    IRecurringTaskInstanceReader instanceReader,
    TimeProvider timeProvider
) : IQueryHandler<MorningReportQuery, MorningReport>
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

        var timeZone = timeProvider.LocalTimeZone;
        var tasks = await store.GetActiveAsync(cancellationToken);
        var instances = await instanceReader.GetActiveAsync(cancellationToken);

        var items = tasks
            .Select(task => ClassifyItem(task.Id, task.Title, task.DueAt, "one-shot", reportDate, timeZone))
            .Concat(
                instances.Select(instance =>
                    ClassifyItem(
                        instance.RecurringTaskId,
                        instance.Title,
                        instance.DueAt,
                        "recurring",
                        reportDate,
                        timeZone
                    )
                )
            )
            .OfType<MorningReportItem>()
            .OrderBy(item => item.DueAt)
            .ToList();

        return new MorningReport(
            "4",
            timeProvider.GetUtcNow(),
            reportDate,
            new MorningReportSummary(
                items.Count(item => item.DueState == "due_today"),
                items.Count(item => item.DueState == "overdue"),
                items.Count(item => item.DueState == "upcoming")
            ),
            items
        );
    }

    private static MorningReportItem? ClassifyItem(
        long id,
        string title,
        DateTimeOffset dueAt,
        string type,
        DateOnly reportDate,
        TimeZoneInfo timeZone
    )
    {
        var itemDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(dueAt, timeZone).DateTime);
        var comparison = itemDate.CompareTo(reportDate);
        if (comparison == 0)
            return new MorningReportItem(id, title, dueAt, type, "due_today", null, null);
        if (comparison < 0)
            return new MorningReportItem(
                id,
                title,
                dueAt,
                type,
                "overdue",
                reportDate.DayNumber - itemDate.DayNumber,
                null
            );
        if (itemDate <= reportDate.AddDays(7))
            return new MorningReportItem(
                id,
                title,
                dueAt,
                type,
                "upcoming",
                null,
                itemDate.DayNumber - reportDate.DayNumber
            );
        return null;
    }
}
