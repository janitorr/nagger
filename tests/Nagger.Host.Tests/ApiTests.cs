using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;
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

        var response = await client.PostAsJsonAsync(
            "/tasks/one-shot",
            new { title = "Pay rent", dueAt = "2026-08-04T09:00:00+03:00" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        task.RootElement.GetProperty("id").GetInt64().ShouldBe(1);
        task.RootElement.GetProperty("type").GetString().ShouldBe("one-shot");
        task.RootElement.GetProperty("dueAt").GetString().ShouldBe("2026-08-04T09:00:00+03:00");
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
        tasks
            .RootElement.EnumerateArray()
            .Select(task => task.GetProperty("id").GetInt64())
            .ShouldBe([activeId, pausedId]);
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
    public async Task MorningReport_GivenMixedDueStates_WhenRequested_ThenOrdersItemsChronologically()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Due today morning", "2026-08-04T08:00:00+03:00");
        await CreateTaskAsync(client, "Upcoming", "2026-08-05T09:00:00+03:00");
        await CreateTaskAsync(client, "Overdue", "2026-08-03T09:00:00+03:00");
        await CreateTaskAsync(client, "Due today noon", "2026-08-04T12:00:00+03:00");

        using var report = JsonDocument.Parse(await client.GetStringAsync("/reports/morning?date=2026-08-04"));

        var items = report.RootElement.GetProperty("items");
        items.EnumerateArray().Select(item => item.GetProperty("id").GetInt64()).ShouldBe([3, 1, 4, 2]);
        items
            .EnumerateArray()
            .Select(item => item.GetProperty("dueState").GetString())
            .ShouldBe(["overdue", "due_today", "due_today", "upcoming"]);
    }

    [Fact]
    public async Task MorningReport_GivenTaskDueToday_WhenRequested_ThenReturnsTaskDetails()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Pay rent", "2026-08-04T09:00:00+03:00");

        var response = await client.GetAsync("/reports/morning?date=2026-08-04");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var report = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        report.RootElement.GetProperty("schemaVersion").GetString().ShouldBe("4");
        report.RootElement.GetProperty("generatedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        report.RootElement.GetProperty("summary").GetProperty("dueToday").GetInt32().ShouldBe(1);
        var item = report.RootElement.GetProperty("items")[0];
        item.GetProperty("type").GetString().ShouldBe("one-shot");
        item.GetProperty("dueAt").GetString().ShouldBe("2026-08-04T09:00:00+03:00");
        item.GetProperty("dueState").GetString().ShouldBe("due_today");
        item.GetProperty("daysOverdue").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("daysUntilDue").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task CreateOneShotTask_GivenInvalidPayload_WhenCreateRequested_ThenReturnsValidationErrorsWithoutPersistingTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "", dueAt = "not-a-date" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("title", out _).ShouldBeTrue();
        body.RootElement.GetProperty("errors").TryGetProperty("dueAt", out _).ShouldBeTrue();
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task CreateOneShotTask_GivenPastDueAt_WhenCreateRequested_ThenReturnsValidationErrorWithoutPersistingTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/tasks/one-shot",
            new { title = "Pay rent", dueAt = "2026-08-02T09:00:00+03:00" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("dueAt", out _).ShouldBeTrue();
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
    public async Task MorningReport_GivenUpcomingTaskWithinWindow_WhenRequested_ThenReturnsTaskDetail()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Future", "2026-08-05T09:00:00+03:00");

        var response = await client.GetStringAsync("/reports/morning?date=2026-08-04");

        using var report = JsonDocument.Parse(response);
        report.RootElement.GetProperty("summary").GetProperty("upcoming").GetInt32().ShouldBe(1);
        var item = report.RootElement.GetProperty("items")[0];
        item.GetProperty("dueState").GetString().ShouldBe("upcoming");
        item.GetProperty("daysOverdue").ValueKind.ShouldBe(JsonValueKind.Null);
        item.GetProperty("daysUntilDue").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task MorningReport_GivenUpcomingTaskOutsideWindow_WhenRequested_ThenExcludesTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Future", "2026-08-12T09:00:00+03:00");

        using var report = JsonDocument.Parse(await client.GetStringAsync("/reports/morning?date=2026-08-04"));

        report.RootElement.GetProperty("summary").GetProperty("upcoming").GetInt32().ShouldBe(0);
        report.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task MorningReport_GivenUnchangedTasks_WhenRequestedTwice_ThenReturnsSameItems()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateTaskAsync(client, "Future", "2026-08-05T09:00:00+03:00");

        var first = await client.GetStringAsync("/reports/morning?date=2026-08-04");
        var second = await client.GetStringAsync("/reports/morning?date=2026-08-04");

        using var firstReport = JsonDocument.Parse(first);
        using var secondReport = JsonDocument.Parse(second);
        secondReport
            .RootElement.GetProperty("items")
            .GetArrayLength()
            .ShouldBe(firstReport.RootElement.GetProperty("items").GetArrayLength());
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

        var response = await client.PostAsJsonAsync(
            "/tasks/one-shot",
            new { title = "Secret task", dueAt = "2026-08-04T09:00:00+03:00" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("storage failure", Case.Insensitive);
    }

    [Fact]
    public async Task CreateOneShotTask_GivenSnakeCaseScheduleFields_WhenCreateRequested_ThenRejectsFields()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/tasks/one-shot",
            new { title = "Task", due_at = "2026-08-04T09:00:00+03:00" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        errors.TryGetProperty("dueAt", out _).ShouldBeTrue();
        errors.TryGetProperty("due_at", out _).ShouldBeFalse();
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
    public void DispatchSucceeded_GivenTaskDispatch_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.DispatchSucceeded(logger, "CreateOneShotTaskCommand", 10);

        logger.EventIds.ShouldBe([1004]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    [Fact]
    public void DispatchValidationFailed_GivenTaskDispatch_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.DispatchValidationFailed(logger, "CreateOneShotTaskCommand", "ValidationException", 10);

        logger.EventIds.ShouldBe([1005]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    [Fact]
    public void DispatchFailed_GivenTaskDispatch_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.DispatchFailed(logger, "CreateOneShotTaskCommand", "InvalidOperationException", 10);

        logger.EventIds.ShouldBe([1006]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    [Fact]
    public void DispatchNotFound_GivenTaskDispatch_WhenLogged_ThenDoesNotIncludeTaskContent()
    {
        var logger = new CapturingLogger();

        AppLog.DispatchNotFound(logger, "CompleteOneShotTaskCommand", "TaskNotFoundException", 10);

        logger.EventIds.ShouldBe([1007]);
        string.Join(" ", logger.Messages).ShouldNotContain("Pay rent");
    }

    private static async Task<long> CreateTaskAsync(
        HttpClient client,
        string title = "Task",
        string dueAt = "2026-08-04T09:00:00+03:00"
    )
    {
        using var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title, dueAt });
        using var task = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return task.RootElement.GetProperty("id").GetInt64();
    }

    [Fact]
    public async Task CreateRecurringTask_GivenValidPayload_WhenCreateRequested_ThenReturnsCreatedTemplateAndFirstInstance()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var startDate = FutureStartDate();

        var response = await client.PostAsJsonAsync(
            "/tasks/recurring",
            new
            {
                title = "Team sync",
                startDate,
                recurrence = new { every = 1, unit = "weeks" },
            }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var template = body.RootElement.GetProperty("template");
        template.GetProperty("id").GetInt64().ShouldBe(1);
        template.GetProperty("title").GetString().ShouldBe("Team sync");
        template.GetProperty("startDate").GetString().ShouldBe(startDate);
        template.GetProperty("recurrence").GetProperty("every").GetInt32().ShouldBe(1);
        template.GetProperty("recurrence").GetProperty("unit").GetString().ShouldBe("weeks");
        template.GetProperty("status").GetString().ShouldBe("active");
        template.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);

        var firstInstance = body.RootElement.GetProperty("firstInstance");
        firstInstance.GetProperty("id").GetInt64().ShouldBe(1);
        firstInstance.GetProperty("title").GetString().ShouldBe("Team sync");
        firstInstance.GetProperty("type").GetString().ShouldBe("recurring");
        firstInstance.GetProperty("status").GetString().ShouldBe("active");
        firstInstance.GetProperty("recurringTaskId").GetInt64().ShouldBe(1);
        firstInstance.GetProperty("dueAt").GetString().ShouldBe($"{startDate}T00:00:00+03:00");

        using var tasks = JsonDocument.Parse(await client.GetStringAsync("/tasks/one-shot"));
        tasks.RootElement.GetArrayLength().ShouldBe(0);
        using var scope = factory.Services.CreateScope();
        var instance = await scope
            .ServiceProvider.GetRequiredService<NaggerDbContext>()
            .RecurringTaskInstances.SingleAsync();
        instance.Title.ShouldBe("Team sync");
        instance.Status.ShouldBe("active");
        instance.RecurringTaskId.ShouldBe(1);
    }

    [Fact]
    public async Task CreateRecurringTask_GivenInvalidPayload_WhenCreateRequested_ThenReturnsValidationErrorsWithoutPersisting()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/tasks/recurring",
            new
            {
                title = "",
                startDate = "not-a-date",
                recurrence = new { every = 0, unit = "hourly" },
            }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        errors.TryGetProperty("title", out _).ShouldBeTrue();
        errors.TryGetProperty("startDate", out _).ShouldBeTrue();
        errors.TryGetProperty("recurrence.every", out _).ShouldBeTrue();
        errors.TryGetProperty("recurrence.unit", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task ListRecurringTemplates_GivenNoTemplates_WhenRequested_ThenReturnsEmptyArray()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/tasks/recurring");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var templates = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        templates.RootElement.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task ListRecurringTemplates_GivenTemplates_WhenRequested_ThenReturnsInAscendingIdOrder()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await CreateRecurringTemplateAsync(client, "Second");
        await CreateRecurringTemplateAsync(client, "First");

        using var templates = JsonDocument.Parse(await client.GetStringAsync("/tasks/recurring"));

        templates.RootElement.EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenTemplateWithActiveInstance_WhenCompleteRequested_ThenCompletesInstanceAndCreatesNext()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var templateId = await CreateRecurringTemplateAsync(client);

        var response = await client.PostAsync($"/tasks/recurring/{templateId}/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var completed = body.RootElement.GetProperty("completedInstance");
        completed.GetProperty("status").GetString().ShouldBe("done");
        completed.GetProperty("type").GetString().ShouldBe("recurring");
        completed.GetProperty("recurringTaskId").GetInt64().ShouldBe(templateId);
        completed.GetProperty("completedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);

        var next = body.RootElement.GetProperty("nextInstance");
        next.GetProperty("status").GetString().ShouldBe("active");
        next.GetProperty("type").GetString().ShouldBe("recurring");
        next.GetProperty("recurringTaskId").GetInt64().ShouldBe(templateId);
        next.GetProperty("title").GetString().ShouldBe("Team sync");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NaggerDbContext>();
        (await db.RecurringTaskInstances.CountAsync()).ShouldBe(2);
        (await db.RecurringTaskInstances.SingleAsync(x => x.Status == "active")).Title.ShouldBe("Team sync");
        (await db.RecurringTaskInstances.SingleAsync(x => x.Status == "done")).CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenMissingTemplate_WhenCompleteRequested_ThenReturnsNotFound()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/tasks/recurring/42/complete", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PauseRecurringTask_GivenActiveTemplate_WhenPauseRequested_ThenPausesTemplateAndInstance()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var templateId = await CreateRecurringTemplateAsync(client);

        var response = await client.PostAsync($"/tasks/recurring/{templateId}/pause", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var template = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        template.RootElement.GetProperty("status").GetString().ShouldBe("paused");
        using var scope = factory.Services.CreateScope();
        (
            await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().RecurringTaskInstances.SingleAsync()
        ).Status.ShouldBe("paused");
    }

    [Fact]
    public async Task ResumeRecurringTask_GivenPausedTemplate_WhenResumeRequested_ThenResumesTemplateAndInstance()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var templateId = await CreateRecurringTemplateAsync(client);
        using var paused = await client.PostAsync($"/tasks/recurring/{templateId}/pause", null);

        var response = await client.PostAsync($"/tasks/recurring/{templateId}/resume", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var template = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        template.RootElement.GetProperty("status").GetString().ShouldBe("active");
        using var scope = factory.Services.CreateScope();
        (
            await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().RecurringTaskInstances.SingleAsync()
        ).Status.ShouldBe("active");
    }

    [Fact]
    public async Task CancelRecurringTask_GivenActiveTemplate_WhenCancelRequested_ThenCancelsTemplateAndInstances()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var templateId = await CreateRecurringTemplateAsync(client);

        var response = await client.PostAsync($"/tasks/recurring/{templateId}/cancel", null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var template = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        template.RootElement.GetProperty("status").GetString().ShouldBe("cancelled");
        template.RootElement.GetProperty("cancelledAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        using var scope = factory.Services.CreateScope();
        (
            await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().RecurringTaskInstances.SingleAsync()
        ).Status.ShouldBe("cancelled");
    }

    [Fact]
    public async Task PauseRecurringTask_GivenPausedTemplate_WhenPauseRequested_ThenReturnsValidationError()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var templateId = await CreateRecurringTemplateAsync(client);
        using var paused = await client.PostAsync($"/tasks/recurring/{templateId}/pause", null);

        var response = await client.PostAsync($"/tasks/recurring/{templateId}/pause", null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("status", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task RecurringTaskLifecycle_GivenMissingTemplate_WhenPauseRequested_ThenReturnsNotFound()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/tasks/recurring/42/pause", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<long> CreateRecurringTemplateAsync(HttpClient client, string title = "Team sync")
    {
        using var response = await client.PostAsJsonAsync(
            "/tasks/recurring",
            new
            {
                title,
                startDate = FutureStartDate(),
                recurrence = new { every = 1, unit = "weeks" },
            }
        );
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("template").GetProperty("id").GetInt64();
    }

    private static string FutureStartDate() => DateTime.UtcNow.Date.AddDays(7).ToString("yyyy-MM-dd");
}

public sealed class ThrowingStore : ITaskStore
{
    public ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("storage failure containing task title");

    public ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("storage failure");

    public ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("storage failure");

    public ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("storage failure");

    public ValueTask<IReadOnlyList<TaskItem>> GetOpenOneShotTasksAsync(CancellationToken cancellationToken) =>
        throw new InvalidOperationException("storage failure");
}

public sealed class CapturingLogger : ILogger
{
    public List<int> EventIds { get; } = [];
    public List<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        EventIds.Add(eventId.Id);
        Messages.Add(formatter(state, exception));
    }
}
