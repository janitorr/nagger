using System.ComponentModel;
using System.Text.Json;
using Mediator;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Nagger.Core.Tasks;
using RequiredAttribute = System.ComponentModel.DataAnnotations.RequiredAttribute;

namespace Nagger.Host.Mcp;

public sealed class McpTaskTools(IMediator mediator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "create_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Use when the user asks to remember a single task at a specific time. Creates a non-recurring one-shot task; do not use for recurring reminders.")]
    public Task<CallToolResult> CreateOneShotTask(
        [RequiredAttribute, Description("Required nonempty task title.")] string? title,
        [RequiredAttribute, Description("Required ISO-8601 due timestamp with an explicit UTC offset, for example 2026-08-04T09:00:00+03:00.")] string? dueAt,
        [RequiredAttribute, Description("Required reminder policy: none, once, or weekly-until-done.")] string? reminderPolicy,
        CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new CreateOneShotTaskCommand(title, dueAt, reminderPolicy), cancellationToken)));

    [McpServerTool(Name = "complete_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Use only when the user says an active one-shot task is finished. Changes it to done; it cannot be resumed.")]
    public Task<CallToolResult> CompleteOneShotTask([Description("Identifier returned by create_one_shot_task or get_morning_report.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new CompleteOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "pause_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Use when the user wants to temporarily stop an active one-shot task without finishing or cancelling it. The task can later be resumed.")]
    public Task<CallToolResult> PauseOneShotTask([Description("Identifier returned by create_one_shot_task or get_morning_report.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new PauseOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "resume_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Use only to reactivate a paused one-shot task. It changes the task back to active.")]
    public Task<CallToolResult> ResumeOneShotTask([Description("Identifier returned by create_one_shot_task or get_morning_report.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new ResumeOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "cancel_one_shot_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Use when the user no longer wants an active or paused one-shot task. Permanently cancels it; it cannot be resumed.")]
    public Task<CallToolResult> CancelOneShotTask([Description("Identifier returned by create_one_shot_task or get_morning_report.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new CancelOneShotTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "list_one_shot_tasks", ReadOnly = true, UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse[]))]
    [Description("Use to discover active and paused one-shot tasks. Each returned id is the identifier required by lifecycle tools.")]
    public Task<CallToolResult> ListOneShotTasks(CancellationToken cancellationToken) =>
        Run(async () => (await mediator.Send(new ListOpenOneShotTasksQuery(), cancellationToken)).Select(McpTaskResponse.From).ToArray());

    [McpServerTool(Name = "create_recurring_task", UseStructuredContent = true, OutputSchemaType = typeof(McpRecurringTemplateResponse))]
    [Description("Use when the user wants to set up a task that repeats on an interval, such as a weekly or monthly obligation. Creates a recurring template and its first instance; do not use for one-off reminders.")]
    public Task<CallToolResult> CreateRecurringTask(
        [RequiredAttribute, Description("Required nonempty task title.")] string? title,
        [RequiredAttribute, Description("Required first due date in YYYY-MM-DD format.")] string? startDate,
        [RequiredAttribute, Description("Required positive interval between recurrences, for example 1.")] int? recurrenceEvery,
        [RequiredAttribute, Description("Required recurrence unit: days, weeks, or months.")] string? recurrenceUnit,
        [RequiredAttribute, Description("Required reminder policy: none, once, or weekly-until-done.")] string? reminderPolicy,
        CancellationToken cancellationToken) =>
        Run(async () => McpRecurringTemplateResponse.From(await mediator.Send(new CreateRecurringTaskCommand(title, startDate, new RecurrenceRuleInput(recurrenceEvery, recurrenceUnit), reminderPolicy), cancellationToken)));

    [McpServerTool(Name = "complete_recurring_task", UseStructuredContent = true, OutputSchemaType = typeof(McpTaskResponse))]
    [Description("Use when the user says an active recurring-task instance is finished. Marks it done and schedules the next instance from the template's recurrence.")]
    public Task<CallToolResult> CompleteRecurringTask([Description("Identifier of the recurring-task instance returned by list_recurring_tasks or the morning report.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpTaskResponse.From(await mediator.Send(new CompleteRecurringTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "pause_recurring_task", UseStructuredContent = true, OutputSchemaType = typeof(McpRecurringTemplateResponse))]
    [Description("Use when the user wants to temporarily stop an active recurring task. Pauses the template and its current instance; it can later be resumed.")]
    public Task<CallToolResult> PauseRecurringTask([Description("Identifier returned by create_recurring_task or list_recurring_tasks.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpRecurringTemplateResponse.From(await mediator.Send(new PauseRecurringTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "resume_recurring_task", UseStructuredContent = true, OutputSchemaType = typeof(McpRecurringTemplateResponse))]
    [Description("Use only to reactivate a paused recurring task. Sets the template and its current instance back to active.")]
    public Task<CallToolResult> ResumeRecurringTask([Description("Identifier returned by create_recurring_task or list_recurring_tasks.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpRecurringTemplateResponse.From(await mediator.Send(new ResumeRecurringTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "cancel_recurring_task", UseStructuredContent = true, OutputSchemaType = typeof(McpRecurringTemplateResponse))]
    [Description("Use when the user no longer wants a recurring task to continue. Cancels the template and all its generated instances; it cannot be resumed.")]
    public Task<CallToolResult> CancelRecurringTask([Description("Identifier returned by create_recurring_task or list_recurring_tasks.")] long id, CancellationToken cancellationToken) =>
        Run(async () => McpRecurringTemplateResponse.From(await mediator.Send(new CancelRecurringTaskCommand(id), cancellationToken)));

    [McpServerTool(Name = "list_recurring_tasks", ReadOnly = true, UseStructuredContent = true, OutputSchemaType = typeof(McpRecurringTemplateResponse[]))]
    [Description("Use to discover recurring task templates. Each returned id is the identifier required by recurring lifecycle tools.")]
    public Task<CallToolResult> ListRecurringTasks(CancellationToken cancellationToken) =>
        Run(async () => (await mediator.Send(new ListRecurringTemplatesQuery(), cancellationToken)).Select(McpRecurringTemplateResponse.From).ToArray());

    [McpServerTool(Name = "get_morning_report", ReadOnly = true, UseStructuredContent = true, OutputSchemaType = typeof(McpMorningReportResponse))]
    [Description("Use to review active one-shot tasks for a specific date in the configured timezone. Returns due-today, overdue, and upcoming counts without changing task state.")]
    public Task<CallToolResult> GetMorningReport([RequiredAttribute, Description("Required report date in YYYY-MM-DD format, interpreted in the configured timezone.")] string? date, CancellationToken cancellationToken) =>
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
        catch (RecurringTaskNotFoundException exception)
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

public sealed record McpRecurringTemplateResponse(
    long Id,
    string Title,
    string StartDate,
    McpRecurrenceRuleResponse Recurrence,
    string ReminderPolicy,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CancelledAt)
{
    public static McpRecurringTemplateResponse From(RecurringTaskTemplate template) => new(
        template.Id,
        template.Title,
        template.StartDate.ToString("yyyy-MM-dd"),
        new McpRecurrenceRuleResponse(template.Recurrence.Every, template.Recurrence.Unit.ToContractValue()),
        template.ReminderPolicy.ToContractValue(),
        template.Status.ToContractValue(),
        template.CreatedAt,
        template.UpdatedAt,
        template.CancelledAt);
}

public sealed record McpRecurrenceRuleResponse(int Every, string Unit);

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
