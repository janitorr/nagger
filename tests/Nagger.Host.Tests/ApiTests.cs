using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nagger.Core.Tasks;
using Nagger.Host;
using Nagger.Host.Infrastructure;
using Shouldly;

namespace Nagger.Host.Tests;

public sealed class ApiTests
{
    [Fact]
    public async Task Migrates_and_dispatches_create_and_report_requests()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Pay rent", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "once" });

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        created.RootElement.GetProperty("id").GetInt64().ShouldBe(1);
        created.RootElement.GetProperty("type").GetString().ShouldBe("one-shot");
        created.RootElement.GetProperty("dueAt").GetString().ShouldBe("2026-08-04T09:00:00+03:00");
        created.RootElement.GetProperty("reminderPolicy").GetString().ShouldBe("once");
        created.RootElement.GetProperty("createdAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        created.RootElement.GetProperty("updatedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        created.RootElement.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        created.RootElement.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);

        var report = await client.GetAsync("/reports/morning?date=2026-08-04");
        report.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("schemaVersion").GetString().ShouldBe("1");
        body.RootElement.GetProperty("generatedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        body.RootElement.GetProperty("summary").GetProperty("dueToday").GetInt32().ShouldBe(1);
        var item = body.RootElement.GetProperty("items")[0];
        item.GetProperty("dueAt").GetString().ShouldBe("2026-08-04T09:00:00+03:00");
        item.GetProperty("dueState").GetString().ShouldBe("due_today");
        item.GetProperty("daysOverdue").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("reminderPolicy").GetString().ShouldBe("once");

        using var scope = factory.Services.CreateScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.SingleAsync();
        persisted.Status.ShouldBe("active");
        persisted.CompletedAt.ShouldBeNull();
        persisted.CancelledAt.ShouldBeNull();
    }

    [Fact]
    public async Task Returns_structured_validation_errors_without_writes()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "", dueAt = "not-a-date", reminderPolicy = "daily" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("title", out _).ShouldBeTrue();
        body.RootElement.GetProperty("errors").TryGetProperty("dueAt", out _).ShouldBeTrue();
        body.RootElement.GetProperty("errors").TryGetProperty("reminderPolicy", out _).ShouldBeTrue();
        (await client.GetAsync("/reports/morning")).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Report_counts_upcoming_tasks_without_item_details_and_is_repeatable()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Future", dueAt = "2026-08-05T09:00:00+03:00", reminderPolicy = "none" });

        var first = await client.GetStringAsync("/reports/morning?date=2026-08-04");
        var second = await client.GetStringAsync("/reports/morning?date=2026-08-04");
        using var firstBody = JsonDocument.Parse(first);
        using var secondBody = JsonDocument.Parse(second);
        firstBody.RootElement.GetProperty("summary").GetProperty("upcoming").GetInt32().ShouldBe(1);
        firstBody.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
        secondBody.RootElement.GetProperty("items").GetArrayLength().ShouldBe(firstBody.RootElement.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData("complete", "done")]
    [InlineData("pause", "paused")]
    [InlineData("cancel", "cancelled")]
    public async Task Lifecycle_GivenActiveTask_WhenCompletePauseOrCancelRequested_ThenReturnsUpdatedTask(string action, string status)
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Task", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" });
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetInt64();

        var response = await client.PostAsync($"/tasks/{id}/{action}", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetString().ShouldBe(status);
        if (status == "paused")
        {
            body.RootElement.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
            body.RootElement.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
        }
        else
            body.RootElement.GetProperty(status == "done" ? "completedAt" : "cancelledAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Resume_GivenPausedTask_WhenResumeRequested_ThenReturnsActiveTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Task", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" });
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetInt64();
        await client.PostAsync($"/tasks/{id}/pause", null);

        var response = await client.PostAsync($"/tasks/{id}/resume", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetString().ShouldBe("active");
        body.RootElement.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        body.RootElement.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Complete_GivenMissingTask_WhenCompleteRequested_ThenReturnsNotFound()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        (await client.PostAsync("/tasks/42/complete", null)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Complete_GivenPausedTask_WhenCompleteRequested_ThenReturnsValidationError()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Task", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" });
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetInt64();
        await client.PostAsync($"/tasks/{id}/pause", null);

        var response = await client.PostAsync($"/tasks/{id}/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("status", out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData("pause")]
    [InlineData("complete")]
    [InlineData("cancel")]
    public async Task MorningReport_GivenInactiveTask_WhenRequested_ThenExcludesTask(string action)
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var created = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Task", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" });
        using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
        var id = createdBody.RootElement.GetProperty("id").GetInt64();
        await client.PostAsync($"/tasks/{id}/{action}", null);

        var report = await client.GetAsync("/reports/morning?date=2026-08-04");

        using var body = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("summary").GetProperty("dueToday").GetInt32().ShouldBe(0);
        body.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Converts_unhandled_failures_without_leaking_details()
    {
        using var factory = new NaggerFactory(services =>
        {
            services.RemoveAll<ITaskStore>();
            services.AddScoped<ITaskStore, ThrowingStore>();
        });
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Secret task", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" });

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("storage failure", Case.Insensitive);
    }

    [Fact]
    public async Task CreateOneShotTask_GivenSnakeCaseScheduleFields_WhenCreateRequested_ThenRejectsFields()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Task", due_at = "2026-08-04T09:00:00+03:00", reminder_policy = "once" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        errors.TryGetProperty("dueAt", out _).ShouldBeTrue();
        errors.TryGetProperty("reminderPolicy", out _).ShouldBeTrue();
        errors.TryGetProperty("due_at", out _).ShouldBeFalse();
        errors.TryGetProperty("reminder_policy", out _).ShouldBeFalse();
    }

    [Fact]
    public void Operational_logs_do_not_include_task_content()
    {
        var logger = new CapturingLogger();

        AppLog.RequestCompleted(logger, "/tasks/one-shot", 201, 10);
        AppLog.ValidationRejected(logger, "/tasks/one-shot");
        AppLog.TaskCreated(logger, 42);
        AppLog.UnexpectedFailure(logger, "/tasks/one-shot", "InvalidOperationException");

        logger.EventIds.ShouldBe([1000, 1001, 1002, 1003]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

}

public sealed class ThrowingStore : ITaskStore
{
    public ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure containing task title");
    public ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure");
    public ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure");
    public ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure");
}

public sealed class CapturingLogger : ILogger
{
    public List<int> EventIds { get; } = [];
    public List<string> Messages { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        EventIds.Add(eventId.Id);
        Messages.Add(formatter(state, exception));
    }
}
