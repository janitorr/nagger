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
    public async Task CreateOneShotTask_GivenValidPayload_WhenCreateRequested_ThenReturnsCreatedTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Pay rent", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "once" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        task.RootElement.GetProperty("id").GetInt64().ShouldBe(1);
        task.RootElement.GetProperty("type").GetString().ShouldBe("one-shot");
        task.RootElement.GetProperty("dueAt").GetString().ShouldBe("2026-08-04T09:00:00+03:00");
        task.RootElement.GetProperty("reminderPolicy").GetString().ShouldBe("once");
        task.RootElement.GetProperty("createdAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        task.RootElement.GetProperty("updatedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        task.RootElement.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        task.RootElement.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task ListOpenOneShotTasks_GivenMixedStatuses_WhenRequested_ThenReturnsActiveAndPausedInIdOrder()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var activeId = await CreateTaskAsync(client, "Active");
        var pausedId = await CreateTaskAsync(client, "Paused");
        var doneId = await CreateTaskAsync(client, "Done");
        var cancelledId = await CreateTaskAsync(client, "Cancelled");
        using var paused = await client.PostAsync($"/tasks/{pausedId}/pause", null);
        using var completed = await client.PostAsync($"/tasks/{doneId}/complete", null);
        using var cancelled = await client.PostAsync($"/tasks/{cancelledId}/cancel", null);

        var response = await client.GetAsync("/tasks/one-shot");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var tasks = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        tasks.RootElement.EnumerateArray().Select(task => task.GetProperty("id").GetInt64()).ShouldBe([activeId, pausedId]);
        tasks.RootElement[0].GetProperty("status").GetString().ShouldBe("active");
        tasks.RootElement[1].GetProperty("status").GetString().ShouldBe("paused");
        tasks.RootElement[0].GetProperty("type").GetString().ShouldBe("one-shot");
    }

    [Fact]
    public async Task ListOpenOneShotTasks_GivenNoOpenTasks_WhenRequested_ThenReturnsEmptyArray()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/tasks/one-shot");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var tasks = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        tasks.RootElement.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task MorningReport_GivenTaskDueToday_WhenRequested_ThenReturnsTaskDetails()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Pay rent", "2026-08-04T09:00:00+03:00", "once");

        var response = await client.GetAsync("/reports/morning?date=2026-08-04");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var report = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        report.RootElement.GetProperty("schemaVersion").GetString().ShouldBe("1");
        report.RootElement.GetProperty("generatedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        report.RootElement.GetProperty("summary").GetProperty("dueToday").GetInt32().ShouldBe(1);
        var item = report.RootElement.GetProperty("items")[0];
        item.GetProperty("dueAt").GetString().ShouldBe("2026-08-04T09:00:00+03:00");
        item.GetProperty("dueState").GetString().ShouldBe("due_today");
        item.GetProperty("daysOverdue").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("reminderPolicy").GetString().ShouldBe("once");
    }

    [Fact]
    public async Task CreateOneShotTask_GivenInvalidPayload_WhenCreateRequested_ThenReturnsValidationErrorsWithoutPersistingTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "", dueAt = "not-a-date", reminderPolicy = "daily" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("title", out _).ShouldBeTrue();
        body.RootElement.GetProperty("errors").TryGetProperty("dueAt", out _).ShouldBeTrue();
        body.RootElement.GetProperty("errors").TryGetProperty("reminderPolicy", out _).ShouldBeTrue();
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task MorningReport_GivenMissingDate_WhenRequested_ThenReturnsValidationError()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/reports/morning");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MorningReport_GivenUpcomingTask_WhenRequested_ThenCountsTaskWithoutReturningItem()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Future", "2026-08-05T09:00:00+03:00", "none");

        var response = await client.GetStringAsync("/reports/morning?date=2026-08-04");

        using var report = JsonDocument.Parse(response);
        report.RootElement.GetProperty("summary").GetProperty("upcoming").GetInt32().ShouldBe(1);
        report.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task MorningReport_GivenUnchangedTasks_WhenRequestedTwice_ThenReturnsSameItems()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Future", "2026-08-05T09:00:00+03:00", "none");

        var first = await client.GetStringAsync("/reports/morning?date=2026-08-04");
        var second = await client.GetStringAsync("/reports/morning?date=2026-08-04");

        using var firstReport = JsonDocument.Parse(first);
        using var secondReport = JsonDocument.Parse(second);
        secondReport.RootElement.GetProperty("items").GetArrayLength().ShouldBe(firstReport.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task CompleteOneShotTask_GivenActiveTask_WhenCompleteRequested_ThenReturnsDoneTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var id = await CreateTaskAsync(client);

        var response = await client.PostAsync($"/tasks/{id}/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        task.RootElement.GetProperty("status").GetString().ShouldBe("done");
        task.RootElement.GetProperty("completedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        task.RootElement.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task PauseOneShotTask_GivenActiveTask_WhenPauseRequested_ThenReturnsPausedTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var id = await CreateTaskAsync(client);

        var response = await client.PostAsync($"/tasks/{id}/pause", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        task.RootElement.GetProperty("status").GetString().ShouldBe("paused");
        task.RootElement.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        task.RootElement.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task CancelOneShotTask_GivenActiveTask_WhenCancelRequested_ThenReturnsCancelledTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var id = await CreateTaskAsync(client);

        var response = await client.PostAsync($"/tasks/{id}/cancel", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        task.RootElement.GetProperty("status").GetString().ShouldBe("cancelled");
        task.RootElement.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        task.RootElement.GetProperty("cancelledAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task ResumeOneShotTask_GivenPausedTask_WhenResumeRequested_ThenReturnsActiveTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var id = await CreateTaskAsync(client);
        using var paused = await client.PostAsync($"/tasks/{id}/pause", null);

        var response = await client.PostAsync($"/tasks/{id}/resume", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        task.RootElement.GetProperty("status").GetString().ShouldBe("active");
        task.RootElement.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        task.RootElement.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task CompleteOneShotTask_GivenMissingTask_WhenCompleteRequested_ThenReturnsNotFound()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/tasks/42/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteOneShotTask_GivenPausedTask_WhenCompleteRequested_ThenReturnsValidationError()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var id = await CreateTaskAsync(client);
        using var paused = await client.PostAsync($"/tasks/{id}/pause", null);

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
        var id = await CreateTaskAsync(client);
        using var transition = await client.PostAsync($"/tasks/{id}/{action}", null);

        var response = await client.GetAsync("/reports/morning?date=2026-08-04");

        using var report = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        report.RootElement.GetProperty("summary").GetProperty("dueToday").GetInt32().ShouldBe(0);
        report.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task CreateOneShotTask_GivenThrowingStore_WhenCreateRequested_ThenReturnsSanitizedServerError()
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
    public void RequestCompleted_GivenTaskRequest_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.RequestCompleted(logger, "/tasks/one-shot", 201, 10);

        logger.EventIds.ShouldBe([1000]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    [Fact]
    public void ValidationRejected_GivenTaskRequest_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.ValidationRejected(logger, "/tasks/one-shot");

        logger.EventIds.ShouldBe([1001]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    [Fact]
    public void TaskCreated_GivenTaskIdentifier_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.TaskCreated(logger, 42);

        logger.EventIds.ShouldBe([1002]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    [Fact]
    public void UnexpectedFailure_GivenTaskRequest_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.UnexpectedFailure(logger, "/tasks/one-shot", "InvalidOperationException");

        logger.EventIds.ShouldBe([1003]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    private static async Task<long> CreateTaskAsync(HttpClient client, string title = "Task", string dueAt = "2026-08-04T09:00:00+03:00", string reminderPolicy = "none")
    {
        using var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title, dueAt, reminderPolicy });
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return task.RootElement.GetProperty("id").GetInt64();
    }
}

public sealed class ThrowingStore : ITaskStore
{
    public ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure containing task title");
    public ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure");
    public ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure");
    public ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure");
    public ValueTask<IReadOnlyList<TaskItem>> GetOpenOneShotTasksAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("storage failure");
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
