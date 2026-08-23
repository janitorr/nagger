using System.Globalization;
using Mediator;
using Nagger.Core.Tasks;

namespace Nagger.Host.Api;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this WebApplication app)
    {
        app.MapGet(
            "/reports/morning",
            async (string? date, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var report = await mediator.Send(new MorningReportQuery(date), cancellationToken);
                return Results.Ok(MorningReportResponse.From(report));
            }
        );
    }
}

public sealed record MorningReportResponse(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Date,
    MorningReportSummaryResponse Summary,
    IReadOnlyList<MorningReportItemResponse> Items
)
{
    public static MorningReportResponse From(MorningReport report) =>
        new(
            report.SchemaVersion,
            report.GeneratedAt,
            report.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            new MorningReportSummaryResponse(report.Summary.DueToday, report.Summary.Overdue, report.Summary.Upcoming),
            report
                .Items.Select(x => new MorningReportItemResponse(
                    x.Id,
                    x.Title,
                    x.DueAt,
                    x.Type,
                    x.DueState,
                    x.DaysOverdue,
                    x.DaysUntilDue
                ))
                .ToList()
        );
}

public sealed record MorningReportSummaryResponse(int DueToday, int Overdue, int Upcoming);

public sealed record MorningReportItemResponse(
    long Id,
    string Title,
    DateTimeOffset DueAt,
    string Type,
    string DueState,
    int? DaysOverdue,
    int? DaysUntilDue
);
