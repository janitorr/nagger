using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nagger.Core.Tasks;
using Nagger.Host;
using Nagger.Host.Infrastructure;

namespace Nagger.Host.Tests;

public sealed class ApiTests
{
    [Fact]
    public async Task Migrates_and_dispatches_create_and_report_requests()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var create = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Pay rent", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "once" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        Assert.Equal(1, created.RootElement.GetProperty("id").GetInt64());
        Assert.Equal("one-shot", created.RootElement.GetProperty("type").GetString());
        Assert.Equal("2026-08-04T09:00:00+03:00", created.RootElement.GetProperty("dueAt").GetString());
        Assert.Equal("once", created.RootElement.GetProperty("reminderPolicy").GetString());
        Assert.NotEqual(JsonValueKind.Null, created.RootElement.GetProperty("createdAt").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, created.RootElement.GetProperty("updatedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.RootElement.GetProperty("completedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, created.RootElement.GetProperty("cancelledAt").ValueKind);

        var report = await client.GetAsync("/reports/morning?date=2026-08-04");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        using var body = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        Assert.Equal("1", body.RootElement.GetProperty("schemaVersion").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.RootElement.GetProperty("generatedAt").ValueKind);
        Assert.Equal(1, body.RootElement.GetProperty("summary").GetProperty("dueToday").GetInt32());
        var item = body.RootElement.GetProperty("items")[0];
        Assert.Equal("2026-08-04T09:00:00+03:00", item.GetProperty("dueAt").GetString());
        Assert.Equal("due_today", item.GetProperty("dueState").GetString());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("daysOverdue").ValueKind);
        Assert.Equal("once", item.GetProperty("reminderPolicy").GetString());

        using var scope = factory.Services.CreateScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.SingleAsync();
        Assert.Equal("active", persisted.Status);
        Assert.Null(persisted.CompletedAt);
        Assert.Null(persisted.CancelledAt);
    }

    [Fact]
    public async Task Returns_structured_validation_errors_without_writes()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "", dueAt = "not-a-date", reminderPolicy = "daily" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("title", out _));
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("dueAt", out _));
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("reminderPolicy", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/reports/morning")).StatusCode);
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
        Assert.Equal(1, firstBody.RootElement.GetProperty("summary").GetProperty("upcoming").GetInt32());
        Assert.Equal(0, firstBody.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(firstBody.RootElement.GetProperty("items").GetArrayLength(), secondBody.RootElement.GetProperty("items").GetArrayLength());
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(status, body.RootElement.GetProperty("status").GetString());
        if (status == "paused")
        {
            Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("completedAt").ValueKind);
            Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("cancelledAt").ValueKind);
        }
        else
            Assert.NotEqual(JsonValueKind.Null, body.RootElement.GetProperty(status == "done" ? "completedAt" : "cancelledAt").ValueKind);
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("active", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("completedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("cancelledAt").ValueKind);
    }

    [Fact]
    public async Task Complete_GivenMissingTask_WhenCompleteRequested_ThenReturnsNotFound()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/tasks/42/complete", null)).StatusCode);
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

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("status", out _));
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
        Assert.Equal(0, body.RootElement.GetProperty("summary").GetProperty("dueToday").GetInt32());
        Assert.Equal(0, body.RootElement.GetProperty("items").GetArrayLength());
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

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("storage failure", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOneShotTask_GivenSnakeCaseScheduleFields_WhenCreateRequested_ThenRejectsFields()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Task", due_at = "2026-08-04T09:00:00+03:00", reminder_policy = "once" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("dueAt", out _));
        Assert.True(errors.TryGetProperty("reminderPolicy", out _));
        Assert.False(errors.TryGetProperty("due_at", out _));
        Assert.False(errors.TryGetProperty("reminder_policy", out _));
    }

    [Fact]
    public void Operational_logs_do_not_include_task_content()
    {
        var logger = new CapturingLogger();

        AppLog.RequestCompleted(logger, "/tasks/one-shot", 201, 10);
        AppLog.ValidationRejected(logger, "/tasks/one-shot");
        AppLog.TaskCreated(logger, 42);
        AppLog.UnexpectedFailure(logger, "/tasks/one-shot", "InvalidOperationException");

        Assert.Equal([1000, 1001, 1002, 1003], logger.EventIds);
        Assert.DoesNotContain("Pay rent", string.Join(" ", logger.Messages));
    }

    [Fact]
    public async Task Mcp_GivenInitializedSession_WhenTaskToolsAreCalled_ThenReturnsStructuredResults()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        var tools = await SendMcpAsync(client, session, 2, "tools/list", new { });
        var names = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString()).ToList();
        Assert.Contains("create_one_shot_task", names);
        Assert.Contains("complete_one_shot_task", names);
        Assert.Contains("pause_one_shot_task", names);
        Assert.Contains("resume_one_shot_task", names);
        Assert.Contains("cancel_one_shot_task", names);
        Assert.Contains("get_morning_report", names);

        var createTool = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "create_one_shot_task");
        Assert.Contains("single task", createTool.GetProperty("description").GetString(), StringComparison.OrdinalIgnoreCase);
        var requiredCreateFields = createTool.GetProperty("inputSchema").GetProperty("required").EnumerateArray()
            .Select(field => field.GetString()).ToList();
        Assert.Contains("title", requiredCreateFields);
        Assert.Contains("dueAt", requiredCreateFields);
        Assert.Contains("reminderPolicy", requiredCreateFields);

        var pauseTool = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "pause_one_shot_task");
        Assert.Contains("temporarily", pauseTool.GetProperty("description").GetString(), StringComparison.OrdinalIgnoreCase);

        var create = await SendMcpAsync(client, session, 3, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "Pay rent", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "once" }
        });
        var created = create.RootElement.GetProperty("result").GetProperty("structuredContent");
        var id = created.GetProperty("id").GetInt64();
        Assert.Equal("active", created.GetProperty("status").GetString());

        var pause = await SendMcpAsync(client, session, 4, "tools/call", new { name = "pause_one_shot_task", arguments = new { id } });
        Assert.Equal("paused", pause.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString());
        var resume = await SendMcpAsync(client, session, 5, "tools/call", new { name = "resume_one_shot_task", arguments = new { id } });
        Assert.Equal("active", resume.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString());
        var complete = await SendMcpAsync(client, session, 6, "tools/call", new { name = "complete_one_shot_task", arguments = new { id } });
        Assert.Equal("done", complete.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString());

        var secondCreate = await SendMcpAsync(client, session, 7, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "Cancel me", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" }
        });
        var secondId = secondCreate.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("id").GetInt64();
        var cancel = await SendMcpAsync(client, session, 8, "tools/call", new { name = "cancel_one_shot_task", arguments = new { id = secondId } });
        Assert.Equal("cancelled", cancel.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString());

        var report = await SendMcpAsync(client, session, 9, "tools/call", new { name = "get_morning_report", arguments = new { date = "2026-08-04" } });
        Assert.Equal("1", report.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("schemaVersion").GetString());
        Assert.Equal(0, report.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("summary").GetProperty("dueToday").GetInt32());
    }

    [Fact]
    public async Task Mcp_GivenInvalidInputOrTransition_WhenToolCalled_ThenReturnsErrorsWithoutUnexpectedWrites()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        var invalidCreate = await SendMcpAsync(client, session, 2, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "", dueAt = "not-a-date", reminderPolicy = "daily" }
        });
        Assert.True(invalidCreate.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());

        var unknown = await SendMcpAsync(client, session, 3, "tools/call", new { name = "complete_one_shot_task", arguments = new { id = 42 } });
        Assert.True(unknown.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());

        var create = await SendMcpAsync(client, session, 4, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "Task", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" }
        });
        var id = create.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("id").GetInt64();
        await SendMcpAsync(client, session, 5, "tools/call", new { name = "pause_one_shot_task", arguments = new { id } });

        var invalidTransition = await SendMcpAsync(client, session, 6, "tools/call", new { name = "complete_one_shot_task", arguments = new { id } });
        Assert.True(invalidTransition.RootElement.GetProperty("result").GetProperty("isError").GetBoolean());

        using var scope = factory.Services.CreateScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.SingleAsync();
        Assert.Equal("paused", persisted.Status);
    }

    private static async Task<McpSession> InitializeMcpAsync(HttpClient client)
    {
        using var request = CreateMcpRequest(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new { protocolVersion = "2025-11-25", capabilities = new { }, clientInfo = new { name = "Nagger tests", version = "1.0" } }
        });
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        using var body = ParseMcpResponse(responseBody);
        response.Headers.TryGetValues("Mcp-Session-Id", out var sessionIds);
        return new McpSession(sessionIds?.SingleOrDefault(), body.RootElement.GetProperty("result").GetProperty("protocolVersion").GetString()!);
    }

    private static async Task<JsonDocument> SendMcpAsync(HttpClient client, McpSession session, int id, string method, object parameters)
    {
        using var request = CreateMcpRequest(new { jsonrpc = "2.0", id, method, @params = parameters });
        if (session.Id is not null)
            request.Headers.Add("Mcp-Session-Id", session.Id);
        request.Headers.Add("Mcp-Protocol-Version", session.ProtocolVersion);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return ParseMcpResponse(await response.Content.ReadAsStringAsync());
    }

    private static HttpRequestMessage CreateMcpRequest(object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp") { Content = JsonContent.Create(payload) };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        return request;
    }

    private static JsonDocument ParseMcpResponse(string response)
    {
        const string dataPrefix = "data: ";
        var dataStart = response.IndexOf(dataPrefix, StringComparison.Ordinal);
        return JsonDocument.Parse(dataStart >= 0 ? response[(dataStart + dataPrefix.Length)..].Trim() : response);
    }

    private sealed record McpSession(string? Id, string ProtocolVersion);
}

public sealed class NaggerFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"nagger-{Guid.NewGuid():N}.db");
    private readonly Action<IServiceCollection>? _configureServices;

    public NaggerFactory(Action<IServiceCollection>? configureServices = null) => _configureServices = configureServices;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Nagger:DatabasePath"] = _databasePath,
            ["Nagger:TimeZone"] = "Europe/Helsinki"
        }));
        if (_configureServices is not null)
            builder.ConfigureServices(_configureServices);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (File.Exists(_databasePath))
            File.Delete(_databasePath);
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
