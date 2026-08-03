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
        var create = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Pay rent", due_at = "2026-08-04T09:00:00+03:00", reminder_policy = "once" });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        Assert.Equal(1, created.RootElement.GetProperty("id").GetInt64());
        Assert.Equal("one-shot", created.RootElement.GetProperty("type").GetString());

        var report = await client.GetAsync("/reports/morning?date=2026-08-04");
        Assert.Equal(HttpStatusCode.OK, report.StatusCode);
        using var body = JsonDocument.Parse(await report.Content.ReadAsStringAsync());
        Assert.Equal("1", body.RootElement.GetProperty("schema_version").GetString());
        Assert.Equal(1, body.RootElement.GetProperty("summary").GetProperty("due_today").GetInt32());

        using var scope = factory.Services.CreateScope();
        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Tasks.CountAsync());
    }

    [Fact]
    public async Task Returns_structured_validation_errors_without_writes()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "", due_at = "not-a-date", reminder_policy = "daily" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("title", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/reports/morning")).StatusCode);
    }

    [Fact]
    public async Task Report_counts_upcoming_tasks_without_item_details_and_is_repeatable()
    {
        using var factory = new NaggerFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Future", due_at = "2026-08-05T09:00:00+03:00", reminder_policy = "none" });

        var first = await client.GetStringAsync("/reports/morning?date=2026-08-04");
        var second = await client.GetStringAsync("/reports/morning?date=2026-08-04");
        using var firstBody = JsonDocument.Parse(first);
        using var secondBody = JsonDocument.Parse(second);
        Assert.Equal(1, firstBody.RootElement.GetProperty("summary").GetProperty("upcoming").GetInt32());
        Assert.Equal(0, firstBody.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal(firstBody.RootElement.GetProperty("items").GetArrayLength(), secondBody.RootElement.GetProperty("items").GetArrayLength());
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

        var response = await client.PostAsJsonAsync("/tasks/one-shot", new { title = "Secret task", due_at = "2026-08-04T09:00:00+03:00", reminder_policy = "none" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("storage failure", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
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
