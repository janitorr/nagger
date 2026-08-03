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
    public async Task Mcp_GivenInitializedSession_WhenTaskToolsAreCalled_ThenReturnsStructuredResults()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var session = await InitializeMcpAsync(client);

        var tools = await SendMcpAsync(client, session, 2, "tools/list", new { });
        var names = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString()).ToList();
        names.ShouldContain("create_one_shot_task");
        names.ShouldContain("complete_one_shot_task");
        names.ShouldContain("pause_one_shot_task");
        names.ShouldContain("resume_one_shot_task");
        names.ShouldContain("cancel_one_shot_task");
        names.ShouldContain("get_morning_report");

        var createTool = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "create_one_shot_task");
        createTool.GetProperty("description").GetString()!.ShouldContain("single task", Case.Insensitive);
        var requiredCreateFields = createTool.GetProperty("inputSchema").GetProperty("required").EnumerateArray()
            .Select(field => field.GetString()).ToList();
        requiredCreateFields.ShouldContain("title");
        requiredCreateFields.ShouldContain("dueAt");
        requiredCreateFields.ShouldContain("reminderPolicy");

        var pauseTool = tools.RootElement.GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "pause_one_shot_task");
        pauseTool.GetProperty("description").GetString()!.ShouldContain("temporarily", Case.Insensitive);

        var create = await SendMcpAsync(client, session, 3, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "Pay rent", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "once" }
        });
        var created = create.RootElement.GetProperty("result").GetProperty("structuredContent");
        var id = created.GetProperty("id").GetInt64();
        created.GetProperty("status").GetString().ShouldBe("active");

        var pause = await SendMcpAsync(client, session, 4, "tools/call", new { name = "pause_one_shot_task", arguments = new { id } });
        pause.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString().ShouldBe("paused");
        var resume = await SendMcpAsync(client, session, 5, "tools/call", new { name = "resume_one_shot_task", arguments = new { id } });
        resume.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString().ShouldBe("active");
        var complete = await SendMcpAsync(client, session, 6, "tools/call", new { name = "complete_one_shot_task", arguments = new { id } });
        complete.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString().ShouldBe("done");

        var secondCreate = await SendMcpAsync(client, session, 7, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "Cancel me", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" }
        });
        var secondId = secondCreate.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("id").GetInt64();
        var cancel = await SendMcpAsync(client, session, 8, "tools/call", new { name = "cancel_one_shot_task", arguments = new { id = secondId } });
        cancel.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("status").GetString().ShouldBe("cancelled");

        var report = await SendMcpAsync(client, session, 9, "tools/call", new { name = "get_morning_report", arguments = new { date = "2026-08-04" } });
        report.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("schemaVersion").GetString().ShouldBe("1");
        report.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("summary").GetProperty("dueToday").GetInt32().ShouldBe(0);
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
        invalidCreate.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();

        var unknown = await SendMcpAsync(client, session, 3, "tools/call", new { name = "complete_one_shot_task", arguments = new { id = 42 } });
        unknown.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();

        var create = await SendMcpAsync(client, session, 4, "tools/call", new
        {
            name = "create_one_shot_task",
            arguments = new { title = "Task", dueAt = "2026-08-04T09:00:00+03:00", reminderPolicy = "none" }
        });
        var id = create.RootElement.GetProperty("result").GetProperty("structuredContent").GetProperty("id").GetInt64();
        await SendMcpAsync(client, session, 5, "tools/call", new { name = "pause_one_shot_task", arguments = new { id } });

        var invalidTransition = await SendMcpAsync(client, session, 6, "tools/call", new { name = "complete_one_shot_task", arguments = new { id } });
        invalidTransition.RootElement.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();

        using var scope = factory.Services.CreateScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.SingleAsync();
        persisted.Status.ShouldBe("paused");
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
