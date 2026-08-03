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
    [InlineData("Task", null, "once", "dueAt")]
    [InlineData("Task", "2026-08-04T09:00:00", "once", "dueAt")]
    [InlineData("Task", "2026-08-04T09:00:00+03:00", null, "reminderPolicy")]
    [InlineData("Task", "2026-08-04T09:00:00+03:00", "daily", "reminderPolicy")]
    public async Task Rejects_invalid_creation_values(string? title, string? dueAt, string? policy, string field)
    {
        var store = new MemoryStore();
        var handler = new CreateOneShotTaskHandler(store, new TestClock());

        var exception = await Assert.ThrowsAsync<ValidationException>(async () => await handler.Handle(new(title, dueAt, policy), default));

        Assert.Contains(field, exception.Errors.Keys);
        Assert.Empty(store.Tasks);
    }

    [Theory]
    [InlineData("08/04/2026 09:00:00+03:00")]
    [InlineData("2026-08-04 09:00:00+03:00")]
    public async Task CreateTask_GivenNonIsoDueTimestamp_WhenCreateRequested_ThenRejectsTask(string dueAt)
    {
        var store = new MemoryStore();
        var handler = new CreateOneShotTaskHandler(store, new TestClock());

        var exception = await Assert.ThrowsAsync<ValidationException>(async () => await handler.Handle(new("Task", dueAt, "once"), default));

        Assert.Contains("dueAt", exception.Errors.Keys);
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

    [Fact]
    public async Task MorningReport_GivenWeeklyReminderPolicy_WhenRequested_ThenReturnsContractValue()
    {
        var store = new MemoryStore(new TaskItem(1, "Weekly", new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero), ReminderPolicy.WeeklyUntilDone, default, default));

        var report = await new MorningReportHandler(store, new TestClock(TimeZoneInfo.Utc)).Handle(new("2026-08-04"), default);

        Assert.Equal("weekly-until-done", Assert.Single(report.Items).ReminderPolicy);
    }

    [Theory]
    [InlineData(OneShotTaskStatus.Paused)]
    [InlineData(OneShotTaskStatus.Done)]
    [InlineData(OneShotTaskStatus.Cancelled)]
    public async Task MorningReport_GivenInactiveTask_WhenRequested_ThenExcludesTask(OneShotTaskStatus status)
    {
        var store = new MemoryStore(new TaskItem(1, "Inactive", new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero), ReminderPolicy.None, default, default, Status: status));

        var report = await new MorningReportHandler(store, new TestClock(TimeZoneInfo.Utc)).Handle(new("2026-08-04"), default);

        Assert.Equal(new MorningReportSummary(0, 0, 0), report.Summary);
        Assert.Empty(report.Items);
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

    [Theory]
    [InlineData(OneShotTaskStatus.Active, OneShotTaskStatus.Done)]
    [InlineData(OneShotTaskStatus.Active, OneShotTaskStatus.Paused)]
    [InlineData(OneShotTaskStatus.Paused, OneShotTaskStatus.Active)]
    [InlineData(OneShotTaskStatus.Active, OneShotTaskStatus.Cancelled)]
    [InlineData(OneShotTaskStatus.Paused, OneShotTaskStatus.Cancelled)]
    public async Task LifecycleTransition_GivenAllowedSourceAndTarget_WhenRequested_ThenUpdatesTask(OneShotTaskStatus initial, OneShotTaskStatus expected)
    {
        var store = new MemoryStore(new TaskItem(1, "Task", default, ReminderPolicy.None, default, default, Status: initial));
        var handler = HandlerFor(expected, store);

        var task = await handler();

        Assert.Equal(expected, task.Status);
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero), task.UpdatedAt);
        if (expected == OneShotTaskStatus.Done)
        {
            Assert.Equal(task.UpdatedAt, task.CompletedAt);
            Assert.Null(task.CancelledAt);
        }
        else if (expected == OneShotTaskStatus.Cancelled)
        {
            Assert.Null(task.CompletedAt);
            Assert.Equal(task.UpdatedAt, task.CancelledAt);
        }
        else
        {
            Assert.Null(task.CompletedAt);
            Assert.Null(task.CancelledAt);
        }
    }

    [Fact]
    public async Task Complete_GivenMissingTask_WhenCompleteRequested_ThenThrowsNotFound()
    {
        var handler = new CompleteOneShotTaskHandler(new MemoryStore(), new TestClock());

        await Assert.ThrowsAsync<TaskNotFoundException>(async () => await handler.Handle(new(42), default));
    }

    [Theory]
    [InlineData(OneShotTaskStatus.Active, OneShotTaskStatus.Active)]
    [InlineData(OneShotTaskStatus.Paused, OneShotTaskStatus.Done)]
    [InlineData(OneShotTaskStatus.Done, OneShotTaskStatus.Active)]
    [InlineData(OneShotTaskStatus.Cancelled, OneShotTaskStatus.Paused)]
    [InlineData(OneShotTaskStatus.Done, OneShotTaskStatus.Cancelled)]
    public async Task LifecycleTransition_GivenInvalidOrTerminalSourceAndTarget_WhenRequested_ThenRejectsWithoutWrite(OneShotTaskStatus initial, OneShotTaskStatus target)
    {
        var original = new TaskItem(1, "Task", default, ReminderPolicy.None, default, default, Status: initial);
        var store = new MemoryStore(original);

        var exception = await Assert.ThrowsAsync<ValidationException>(async () => await HandlerFor(target, store)());

        Assert.Contains("status", exception.Errors.Keys);
        Assert.Equal(original, store.Tasks.Single());
        Assert.Equal(0, store.Updates);
    }

    private static Func<ValueTask<TaskItem>> HandlerFor(OneShotTaskStatus target, MemoryStore store) => target switch
    {
        OneShotTaskStatus.Done => () => new CompleteOneShotTaskHandler(store, new TestClock()).Handle(new(1), default),
        OneShotTaskStatus.Paused => () => new PauseOneShotTaskHandler(store, new TestClock()).Handle(new(1), default),
        OneShotTaskStatus.Active => () => new ResumeOneShotTaskHandler(store, new TestClock()).Handle(new(1), default),
        OneShotTaskStatus.Cancelled => () => new CancelOneShotTaskHandler(store, new TestClock()).Handle(new(1), default),
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private sealed class TestClock(TimeZoneInfo? timezone = null) : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 3, 6, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo TimeZone => timezone ?? TimeZoneInfo.Utc;
    }

    private sealed class MemoryStore(params TaskItem[] tasks) : ITaskStore
    {
        public List<TaskItem> Tasks { get; } = [.. tasks];
        public int Reads { get; private set; }
        public int Updates { get; private set; }
        public ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            task = task with { Id = Tasks.Count + 1 };
            Tasks.Add(task);
            return ValueTask.FromResult(task);
        }
        public ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken)
        {
            Reads++;
            return ValueTask.FromResult<IReadOnlyList<TaskItem>>(Tasks.Where(x => x.Status == OneShotTaskStatus.Active).ToList());
        }
        public ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken) => ValueTask.FromResult(Tasks.SingleOrDefault(x => x.Id == id));
        public ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Tasks[Tasks.FindIndex(x => x.Id == task.Id)] = task;
            Updates++;
            return ValueTask.CompletedTask;
        }
    }
}
