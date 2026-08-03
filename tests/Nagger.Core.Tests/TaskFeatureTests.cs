using Nagger.Core.Tasks;

namespace Nagger.Core.Tests;

public sealed class TaskFeatureTests
{
    [Fact]
    public async Task Creates_task_with_assigned_id_and_timestamps()
    {
        var store = new MemoryStore();
        var handler = new CreateOneShotTaskHandler(store, new TestClock());

        var task = await handler.Handle(new("Pay rent", "2026-08-04T09:00:00+03:00", "weekly-until-done"), default);

        Assert.Equal(1, task.Id);
        Assert.Equal("Pay rent", task.Title);
        Assert.Equal(ReminderPolicy.WeeklyUntilDone, task.ReminderPolicy);
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero), task.CreatedAt);
    }

    [Theory]
    [InlineData(null, "2026-08-04T09:00:00+03:00", "once", "title")]
    [InlineData(" ", "2026-08-04T09:00:00+03:00", "once", "title")]
    [InlineData("Task", null, "once", "due_at")]
    [InlineData("Task", "2026-08-04T09:00:00", "once", "due_at")]
    [InlineData("Task", "2026-08-04T09:00:00+03:00", null, "reminder_policy")]
    [InlineData("Task", "2026-08-04T09:00:00+03:00", "daily", "reminder_policy")]
    public async Task Rejects_invalid_creation_values(string? title, string? dueAt, string? policy, string field)
    {
        var store = new MemoryStore();
        var handler = new CreateOneShotTaskHandler(store, new TestClock());

        var exception = await Assert.ThrowsAsync<ValidationException>(async () => await handler.Handle(new(title, dueAt, policy), default));

        Assert.Contains(field, exception.Errors.Keys);
        Assert.Empty(store.Tasks);
    }

    [Fact]
    public async Task Classifies_dates_and_leaves_store_unchanged()
    {
        var store = new MemoryStore(
            new TaskItem(1, "Today", new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero), ReminderPolicy.None, default, default),
            new TaskItem(2, "Old", new DateTimeOffset(2026, 8, 1, 8, 0, 0, TimeSpan.Zero), ReminderPolicy.Once, default, default),
            new TaskItem(3, "Later", new DateTimeOffset(2026, 8, 5, 8, 0, 0, TimeSpan.Zero), ReminderPolicy.None, default, default));
        var handler = new MorningReportHandler(store, new TestClock(TimeZoneInfo.Utc));

        var first = await handler.Handle(new("2026-08-04"), default);
        var second = await handler.Handle(new("2026-08-04"), default);

        Assert.Equal(new MorningReportSummary(1, 1, 1), first.Summary);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal(3, first.Items.Single(x => x.Id == 2).DaysOverdue);
        Assert.Equal(first.Items, second.Items);
        Assert.Equal(2, store.Reads);
    }

    [Fact]
    public async Task Uses_configured_timezone_for_date_boundary()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Helsinki");
        var store = new MemoryStore(new TaskItem(1, "Boundary", new DateTimeOffset(2026, 8, 3, 21, 30, 0, TimeSpan.Zero), ReminderPolicy.None, default, default));
        var report = await new MorningReportHandler(store, new TestClock(timezone)).Handle(new("2026-08-04"), default);

        Assert.Equal(1, report.Summary.DueToday);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026/08/04")]
    [InlineData("2026-8-4")]
    public async Task Rejects_invalid_report_date(string? date)
    {
        var handler = new MorningReportHandler(new MemoryStore(), new TestClock());
        var exception = await Assert.ThrowsAsync<ValidationException>(async () => await handler.Handle(new(date), default));
        Assert.Contains("date", exception.Errors.Keys);
    }

    private sealed class TestClock(TimeZoneInfo? timezone = null) : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 3, 6, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo TimeZone => timezone ?? TimeZoneInfo.Utc;
    }

    private sealed class MemoryStore(params TaskItem[] tasks) : ITaskStore
    {
        public List<TaskItem> Tasks { get; } = [.. tasks];
        public int Reads { get; private set; }
        public ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            task = task with { Id = Tasks.Count + 1 };
            Tasks.Add(task);
            return ValueTask.FromResult(task);
        }
        public ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken)
        {
            Reads++;
            return ValueTask.FromResult<IReadOnlyList<TaskItem>>(Tasks);
        }
    }
}
