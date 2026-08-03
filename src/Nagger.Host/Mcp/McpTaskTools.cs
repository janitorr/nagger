using System.ComponentModel;
using System.Text.Json;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nagger.Core.Tasks;

namespace Nagger.Host.Mcp;

public sealed class McpTaskTools(IMediator mediator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "create_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Create a one-shot task with a title, offset-qualified due timestamp, and reminder policy.")]
    public Task<CallToolResult> CreateOneShotTask(
        [Description("Nonempty task title.")] string? title,
        [Description("ISO-8601 due timestamp with an explicit UTC offset.")] string? dueAt,
        [Description("One of none, once, or weekly-until-done.")] string? reminderPolicy,
        CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new CreateOneShotTaskCommand(title, dueAt, reminderPolicy), cancellationToken)));

    [McpServerTool(Name = "complete_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Mark an active one-shot task as done.")]
    public Task<CallToolResult> CompleteOneShotTask([Description("Task identifier.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new CompleteOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "pause_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Pause an active one-shot task.")]
    public Task<CallToolResult> PauseOneShotTask([Description("Task identifier.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new PauseOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "resume_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Resume a paused one-shot task.")]
    public Task<CallToolResult> ResumeOneShotTask([Description("Task identifier.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new ResumeOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "cancel_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Cancel an active or paused one-shot task.")]
    public Task<CallToolResult> CancelOneShotTask([Description("Task identifier.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new CancelOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "get_morning_report", ReadOnly = true, UseStructuredContent = true, OutputSchemaType = typeof(McpMorningReportResponse))]
    [Description("Get the read-only morning report for a date in the configured timezone.")]
    public Task<CallToolResult> GetMorningReport([Description("Report date in YYYY-MM-DD format.")] string? date, CancellationToken cancellationToken) =>
        Run(async () => McpMorningReportResponse.From(await mediator.Send(new MorningReportQuery(date), cancellationToken)));

    private static async Task<CallToolResult> Run<T>(Func<Task<T>> action)
    {
        try
        {
            var response = await action();
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = JsonSerializer.Serialize(response, JsonOptions) }],
                StructuredContent = JsonSerializer.SerializeToElement(response, JsonOptions)
            };
        }
        catch (ValidationException exception)
        {
            return Error(string.Join(" ", exception.Errors.SelectMany(error => error.Value)));
        }
        catch (TaskNotFoundException exception)
        {
            return Error(exception.Message);
        }
    }

    private static CallToolResult Error(string message) => new()
    {
        Content = [new TextContentBlock { Text = message }],
        IsError = true
    };
}

public sealed record McpTaskResponse(
    long Id,
    string Title,
    string Type,
    string Status,
    DateTimeOffset DueAt,
    string ReminderPolicy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt)
{
    public static McpTaskResponse From(TaskItem task) => new(task.Id, task.Title, "one-shot", task.Status.ToContractValue(), task.DueAt, task.ReminderPolicy.ToContractValue(), task.CreatedAt, task.UpdatedAt, task.CompletedAt, task.CancelledAt);
}

public sealed record McpMorningReportResponse(
    string SchemaVersion,
    DateTimeOffset GeneratedAt,
    string Date,
    McpMorningReportSummaryResponse Summary,
    IReadOnlyList<McpMorningReportItemResponse> Items)
{
    public static McpMorningReportResponse From(MorningReport report) => new(
        report.SchemaVersion,
        report.GeneratedAt,
        report.Date.ToString("yyyy-MM-dd"),
        new McpMorningReportSummaryResponse(report.Summary.DueToday, report.Summary.Overdue, report.Summary.Upcoming),
        report.Items.Select(item => new McpMorningReportItemResponse(item.Id, item.Title, item.DueAt, item.DueState, item.DaysOverdue, item.ReminderPolicy)).ToList());
}

public sealed record McpMorningReportSummaryResponse(int DueToday, int Overdue, int Upcoming);

public sealed record McpMorningReportItemResponse(long Id, string Title, DateTimeOffset DueAt, string DueState, int? DaysOverdue, string ReminderPolicy);
