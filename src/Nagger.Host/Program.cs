using System.Diagnostics;
using System.Text.Json.Serialization;
using Mediator;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Host;
using Nagger.Host.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5000");
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddDbContext<NaggerDbContext>(options =>
    options.UseSqlite($"Data Source={builder.Configuration["Nagger:DatabasePath"] ?? "nagger.db"}"));
builder.Services.AddScoped<ITaskStore, SqliteTaskStore>();
builder.Services.AddSingleton<IClock, ConfiguredClock>();
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
    options.Assemblies = [typeof(TaskItem)];
});

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Database.MigrateAsync();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (error is ValidationException validation)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ValidationError(validation.Errors));
        AppLog.ValidationRejected(app.Logger, context.Request.Path);
        return;
    }

    AppLog.UnexpectedFailure(app.Logger, context.Request.Path, error?.GetType().Name ?? "Unknown");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
}));

app.Use(async (context, next) =>
{
    var timer = Stopwatch.StartNew();
    await next(context);
    AppLog.RequestCompleted(app.Logger, context.Request.Path, context.Response.StatusCode, timer.ElapsedMilliseconds);
});

static async Task<IResult> RunLifecycleCommand(ICommand<TaskItem> command, IMediator mediator, CancellationToken cancellationToken)
{
    try
    {
        return Results.Ok(TaskResponse.From(await mediator.Send(command, cancellationToken)));
    }
    catch (TaskNotFoundException)
    {
        return Results.NotFound();
    }
}

app.MapPost("/tasks/one-shot", async (CreateTaskRequest request, IMediator mediator, CancellationToken cancellationToken) =>
{
    var task = await mediator.Send(new CreateOneShotTaskCommand(request.Title, request.DueAt, request.ReminderPolicy), cancellationToken);
    AppLog.TaskCreated(app.Logger, task.Id);
    return Results.Created($"/tasks/one-shot/{task.Id}", TaskResponse.From(task));
});

app.MapPost("/tasks/{id:long}/complete", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
    await RunLifecycleCommand(new CompleteOneShotTaskCommand(id), mediator, cancellationToken));

app.MapPost("/tasks/{id:long}/pause", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
    await RunLifecycleCommand(new PauseOneShotTaskCommand(id), mediator, cancellationToken));

app.MapPost("/tasks/{id:long}/resume", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
    await RunLifecycleCommand(new ResumeOneShotTaskCommand(id), mediator, cancellationToken));

app.MapPost("/tasks/{id:long}/cancel", async (long id, IMediator mediator, CancellationToken cancellationToken) =>
    await RunLifecycleCommand(new CancelOneShotTaskCommand(id), mediator, cancellationToken));

app.MapGet("/reports/morning", async (string? date, IMediator mediator, CancellationToken cancellationToken) =>
{
    var report = await mediator.Send(new MorningReportQuery(date), cancellationToken);
    return Results.Ok(MorningReportResponse.From(report));
});

app.Run();

public partial class Program;

public sealed record CreateTaskRequest(
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("due_at")] string? DueAt,
    [property: JsonPropertyName("reminder_policy")] string? ReminderPolicy);

public sealed record TaskResponse(long Id, string Title, string Type, string Status,
    [property: JsonPropertyName("due_at")] DateTimeOffset DueAt,
    [property: JsonPropertyName("reminder_policy")] string ReminderPolicy,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt,
    [property: JsonPropertyName("completed_at")] DateTimeOffset? CompletedAt,
    [property: JsonPropertyName("cancelled_at")] DateTimeOffset? CancelledAt)
{
    public static TaskResponse From(TaskItem task) => new(task.Id, task.Title, "one-shot", task.Status.ToContractValue(), task.DueAt, task.ReminderPolicy.ToContractValue(), task.CreatedAt, task.UpdatedAt, task.CompletedAt, task.CancelledAt);
}

public sealed record ValidationError([property: JsonPropertyName("errors")] IReadOnlyDictionary<string, string[]> Errors);

public sealed record MorningReportResponse(
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("generated_at")] DateTimeOffset GeneratedAt,
    string Date,
    MorningReportSummaryResponse Summary,
    IReadOnlyList<MorningReportItemResponse> Items)
{
    public static MorningReportResponse From(MorningReport report) => new(
        report.SchemaVersion,
        report.GeneratedAt,
        report.Date.ToString("yyyy-MM-dd"),
        new MorningReportSummaryResponse(report.Summary.DueToday, report.Summary.Overdue, report.Summary.Upcoming),
        report.Items.Select(x => new MorningReportItemResponse(x.Id, x.Title, x.DueAt, x.DueState, x.DaysOverdue, x.ReminderPolicy)).ToList());
}

public sealed record MorningReportSummaryResponse(
    [property: JsonPropertyName("due_today")] int DueToday,
    [property: JsonPropertyName("overdue")] int Overdue,
    [property: JsonPropertyName("upcoming")] int Upcoming);

public sealed record MorningReportItemResponse(
    long Id,
    string Title,
    [property: JsonPropertyName("due_at")] DateTimeOffset DueAt,
    [property: JsonPropertyName("due_state")] string DueState,
    [property: JsonPropertyName("days_overdue")] int? DaysOverdue,
    [property: JsonPropertyName("reminder_policy")] string ReminderPolicy);
