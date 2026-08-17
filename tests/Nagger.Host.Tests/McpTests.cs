using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nagger.Host.Infrastructure;
using Shouldly;

namespace Nagger.Host.Tests;

public sealed class McpTests
{
    [Fact]
    public async Task Mcp_GivenNewClient_WhenInitialized_ThenNegotiatesProtocolVersion()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();

        var session = await InitializeMcpAsync(client);

        session.ProtocolVersion.ShouldBe("2025-11-25");
    }

    [Fact]
    public async Task Mcp_GivenInitializedSession_WhenToolsListed_ThenAdvertisesTaskTools()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var tools = await SendMcpAsync(client, session, 2, "tools/list", new { });
        var names = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString()).ToList();

        names.ShouldContain("create_one_shot_task");
        names.ShouldContain("complete_one_shot_task");
        names.ShouldContain("pause_one_shot_task");
        names.ShouldContain("resume_one_shot_task");
        names.ShouldContain("cancel_one_shot_task");
        names.ShouldContain("list_one_shot_tasks");
        names.ShouldContain("get_morning_report");
    }

    [Fact]
    public async Task Mcp_GivenOpenAndTerminalTasks_WhenListRequested_ThenReturnsOpenTasksInIdOrder()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var activeId = await CreateTaskAsync(client, session, "Active", 2);
        var pausedId = await CreateTaskAsync(client, session, "Paused", 3);
        var doneId = await CreateTaskAsync(client, session, "Done", 4);
        var cancelledId = await CreateTaskAsync(client, session, "Cancelled", 5);
        using var paused = await SendMcpAsync(client, session, 6, "tools/call", new { name = "pause_one_shot_task", arguments = new { id = pausedId } });
        using var completed = await SendMcpAsync(client, session, 7, "tools/call", new { name = "complete_one_shot_task", arguments = new { id = doneId } });
        using var cancelled = await SendMcpAsync(client, session, 8, "tools/call", new { name = "cancel_one_shot_task", arguments = new { id = cancelledId } });

        using var response = await SendMcpAsync(client, session, 9, "tools/call", new { name = "list_one_shot_tasks", arguments = new { } });
        var tasks = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        tasks.EnumerateArray().Select(task => task.GetProperty("id").GetInt64()).ShouldBe([activeId, pausedId]);
        tasks[0].GetProperty("status").GetString().ShouldBe("active");
        tasks[1].GetProperty("status").GetString().ShouldBe("paused");
    }

    [Fact]
    public async Task Mcp_GivenNoOpenTasks_WhenListRequested_ThenReturnsEmptyArray()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var response = await SendMcpAsync(client, session, 2, "tools/call", new { name = "list_one_shot_tasks", arguments = new { } });

        response.RootElement.GetProperty("result").GetProperty("structuredContent").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Mcp_GivenInitializedSession_WhenCreateToolListed_ThenDescribesRequiredTaskFields()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var tools = await SendMcpAsync(client, session, 2, "tools/list", new { });
        var createTool = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "create_one_shot_task");
        var requiredFields = createTool.GetProperty("inputSchema").GetProperty("required").EnumerateArray()
            .Select(field => field.GetString()).ToList();

        createTool.GetProperty("description").GetString()!.ShouldContain("single task", Case.Insensitive);
        requiredFields.ShouldContain("title");
        requiredFields.ShouldContain("dueAt");
        requiredFields.ShouldContain("reminderPolicy");
    }

    [Fact]
    public async Task Mcp_GivenInitializedSession_WhenPauseToolListed_ThenDescribesTemporaryPause()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var tools = await SendMcpAsync(client, session, 2, "tools/list", new { });
        var pauseTool = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "pause_one_shot_task");

        pauseTool.GetProperty("description").GetString()!.ShouldContain("temporarily", Case.Insensitive);
    }

    [Fact]
    public async Task Mcp_GivenValidTaskInput_WhenCreateRequested_ThenReturnsActiveTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var response = await SendMcpAsync(client, session, 2, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "Pay rent", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "once" }
        });
        var task = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        task.GetProperty("id").GetInt64().ShouldBe(1);
        task.GetProperty("status").GetString().ShouldBe("active");
    }

    [Fact]
    public async Task Mcp_GivenActiveTask_WhenPauseRequested_ThenReturnsPausedTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var id = await CreateTaskAsync(client, session);

        using var response = await SendMcpAsync(client, session, 3, "tools/call", new { name = "pause_one_shot_task", arguments = new { id } });
        var task = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        task.GetProperty("status").GetString().ShouldBe("paused");
        task.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        task.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Mcp_GivenPausedTask_WhenResumeRequested_ThenReturnsActiveTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var id = await CreateTaskAsync(client, session);
        using var paused = await SendMcpAsync(client, session, 3, "tools/call", new { name = "pause_one_shot_task", arguments = new { id } });

        using var response = await SendMcpAsync(client, session, 4, "tools/call", new { name = "resume_one_shot_task", arguments = new { id } });
        var task = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        task.GetProperty("status").GetString().ShouldBe("active");
        task.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        task.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Mcp_GivenActiveTask_WhenCompleteRequested_ThenReturnsDoneTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var id = await CreateTaskAsync(client, session);

        using var response = await SendMcpAsync(client, session, 3, "tools/call", new { name = "complete_one_shot_task", arguments = new { id } });
        var task = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        task.GetProperty("status").GetString().ShouldBe("done");
        task.GetProperty("completedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
        task.GetProperty("cancelledAt").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Mcp_GivenActiveTask_WhenCancelRequested_ThenReturnsCancelledTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var id = await CreateTaskAsync(client, session);

        using var response = await SendMcpAsync(client, session, 3, "tools/call", new { name = "cancel_one_shot_task", arguments = new { id } });
        var task = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        task.GetProperty("status").GetString().ShouldBe("cancelled");
        task.GetProperty("completedAt").ValueKind.ShouldBe(JsonValueKind.Null);
        task.GetProperty("cancelledAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Mcp_GivenTaskDueToday_WhenMorningReportRequested_ThenReturnsDueTaskSummary()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        await CreateTaskAsync(client, session);

        using var response = await SendMcpAsync(client, session, 3, "tools/call", new { name = "get_morning_report", arguments = new { date = "2026-08-04" } });
        var report = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        report.GetProperty("schemaVersion").GetString().ShouldBe("1");
        report.GetProperty("summary").GetProperty("dueToday").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Mcp_GivenInvalidTaskInput_WhenCreateRequested_ThenReturnsErrorWithoutPersistingTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var response = await SendMcpAsync(client, session, 2, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "", dueAt = "not-a-date", reminderPolicy = "daily" }
        });

        response.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Mcp_GivenMissingTask_WhenCompleteRequested_ThenReturnsError()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var response = await SendMcpAsync(client, session, 2, "tools/call", new { name = "complete_one_shot_task", arguments = new { id = 42 } });

        response.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Mcp_GivenPausedTask_WhenCompleteRequested_ThenReturnsErrorWithoutChangingTask()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var id = await CreateTaskAsync(client, session);
        using var paused = await SendMcpAsync(client, session, 3, "tools/call", new { name = "pause_one_shot_task", arguments = new { id } });

        using var response = await SendMcpAsync(client, session, 4, "tools/call", new { name = "complete_one_shot_task", arguments = new { id } });

        response.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.SingleAsync()).Status.ShouldBe("paused");
    }

    [Fact]
    public async Task Mcp_GivenInitializedSession_WhenToolsListed_ThenAdvertisesRecurringTools()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var tools = await SendMcpAsync(client, session, 2, "tools/list", new { });
        var names = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString()).ToList();

        names.ShouldContain("create_recurring_task");
        names.ShouldContain("complete_recurring_task");
        names.ShouldContain("pause_recurring_task");
        names.ShouldContain("resume_recurring_task");
        names.ShouldContain("cancel_recurring_task");
        names.ShouldContain("list_recurring_tasks");
    }

    [Fact]
    public async Task Mcp_GivenValidRecurringInput_WhenCreateRequested_ThenCreatesTemplateAndFirstInstance()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var startDate = FutureStartDate();

        using var response = await SendMcpAsync(client, session, 2, "tools/call", new
        {
            name = "create_recurring_task",
            arguments = new { title = "Team sync", startDate, recurrenceEvery = 1, recurrenceUnit = "weeks", reminderPolicy = "once" }
        });
        var template = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        template.GetProperty("id").GetInt64().ShouldBe(1);
        template.GetProperty("status").GetString().ShouldBe("active");
        template.GetProperty("startDate").GetString().ShouldBe(startDate);
        template.GetProperty("recurrence").GetProperty("every").GetInt32().ShouldBe(1);
        template.GetProperty("recurrence").GetProperty("unit").GetString().ShouldBe("weeks");
    }

    [Fact]
    public async Task Mcp_GivenActiveRecurringInstance_WhenCompleteRequested_ThenCompletesAndCreatesNextInstance()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        await CreateRecurringTaskAsync(client, session, 2);
        var instanceId = await SingleRecurringInstanceIdAsync(client, session, 3);

        using var response = await SendMcpAsync(client, session, 4, "tools/call", new { name = "complete_recurring_task", arguments = new { id = instanceId } });
        var task = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        task.GetProperty("status").GetString().ShouldBe("done");
        task.GetProperty("completedAt").ValueKind.ShouldNotBe(JsonValueKind.Null);

        var remaining = await SendMcpAsync(client, session, 5, "tools/call", new { name = "list_one_shot_tasks", arguments = new { } });
        remaining.RootElement.GetProperty("result").GetProperty("structuredContent").EnumerateArray().Single().GetProperty("title").GetString().ShouldBe("Team sync");
    }

    [Fact]
    public async Task Mcp_GivenRecurringTemplate_WhenPauseRequested_ThenPausesTemplateAndInstance()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var templateId = await CreateRecurringTaskAsync(client, session, 2);

        using var response = await SendMcpAsync(client, session, 3, "tools/call", new { name = "pause_recurring_task", arguments = new { id = templateId } });
        var template = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        template.GetProperty("status").GetString().ShouldBe("paused");

        var tasks = await SendMcpAsync(client, session, 4, "tools/call", new { name = "list_one_shot_tasks", arguments = new { } });
        tasks.RootElement.GetProperty("result").GetProperty("structuredContent").EnumerateArray().Single().GetProperty("status").GetString().ShouldBe("paused");
    }

    [Fact]
    public async Task Mcp_GivenPausedRecurringTemplate_WhenResumeRequested_ThenResumesTemplateAndInstance()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var templateId = await CreateRecurringTaskAsync(client, session, 2);
        using var paused = await SendMcpAsync(client, session, 3, "tools/call", new { name = "pause_recurring_task", arguments = new { id = templateId } });

        using var response = await SendMcpAsync(client, session, 4, "tools/call", new { name = "resume_recurring_task", arguments = new { id = templateId } });
        var template = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        template.GetProperty("status").GetString().ShouldBe("active");

        var tasks = await SendMcpAsync(client, session, 5, "tools/call", new { name = "list_one_shot_tasks", arguments = new { } });
        tasks.RootElement.GetProperty("result").GetProperty("structuredContent").EnumerateArray().Single().GetProperty("status").GetString().ShouldBe("active");
    }

    [Fact]
    public async Task Mcp_GivenRecurringTemplate_WhenCancelRequested_ThenCancelsTemplate()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        var templateId = await CreateRecurringTaskAsync(client, session, 2);

        using var response = await SendMcpAsync(client, session, 3, "tools/call", new { name = "cancel_recurring_task", arguments = new { id = templateId } });
        var template = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        template.GetProperty("status").GetString().ShouldBe("cancelled");
        template.GetProperty("cancelledAt").ValueKind.ShouldNotBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Mcp_GivenRecurringTemplates_WhenListRequested_ThenReturnsTemplates()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);
        await CreateRecurringTaskAsync(client, session, 2);

        using var response = await SendMcpAsync(client, session, 3, "tools/call", new { name = "list_recurring_tasks", arguments = new { } });
        var templates = response.RootElement.GetProperty("result").GetProperty("structuredContent");

        templates.EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).ShouldBe([1]);
        templates[0].GetProperty("status").GetString().ShouldBe("active");
    }

    [Fact]
    public async Task Mcp_GivenInvalidRecurringInput_WhenCreateRequested_ThenReturnsErrorWithoutPersisting()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        using var response = await SendMcpAsync(client, session, 2, "tools/call", new
        {
            name = "create_recurring_task",
            arguments = new { title = "", startDate = "not-a-date", recurrenceEvery = 0, recurrenceUnit = "hourly", reminderPolicy = "daily" }
        });

        response.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().RecurringTaskTemplates.CountAsync()).ShouldBe(0);
    }

    private static async Task<long> CreateRecurringTaskAsync(HttpClient client, McpSession session, int requestId)
    {
        using var response = await SendMcpAsync(client, session, requestId, "tools/call", new
        {
            name = "create_recurring_task",
            arguments = new { title = "Team sync", startDate = FutureStartDate(), recurrenceEvery = 1, recurrenceUnit = "weeks", reminderPolicy = "once" }
        });
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("id").GetInt64();
    }

    private static string FutureStartDate() => DateTime.UtcNow.Date.AddDays(7).ToString("yyyy-MM-dd");

    private static async Task<long> SingleRecurringInstanceIdAsync(HttpClient client, McpSession session, int requestId)
    {
        using var response = await SendMcpAsync(client, session, requestId, "tools/call", new { name = "list_one_shot_tasks", arguments = new { } });
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").EnumerateArray().Single().GetProperty("id").GetInt64();
    }

    private static async Task<long> CreateTaskAsync(HttpClient client, McpSession session, string title = "Task", int requestId = 2)
    {
        using var response = await SendMcpAsync(client, session, requestId, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title, dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" }
        });
        return response.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("id").GetInt64();
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
        using var body = ParseMcpResponse(await response.Content.ReadAsStringAsync());
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
