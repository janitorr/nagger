using System.Globalization;
using Nagger.Core.Tasks;
using Shouldly;

namespace Nagger.Core.Tests;

public sealed class TaskFeatureTests
{
    [Fact]
    public async Task Creates_task_with_assigned_id_and_timestamps()
    {
        var store = new MemoryStore();
        var handler = new CreateOneShotTaskHandler(store, new TestClock());

        var task = await handler.Handle(new("Pay rent", "2026-08-04T09:00:00+03:00", "weekly-until-done"), default);

        task.Id.ShouldBe(1);
        task.Title.ShouldBe("Pay rent");
        task.ReminderPolicy.ShouldBe(ReminderPolicy.WeeklyUntilDone);
        task.CreatedAt.ShouldBe(new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero));
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

        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(title, dueAt, policy), default));

        exception.Errors.Keys.ShouldContain(field);
        store.Tasks.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("08/04/2026 09:00:00+03:00")]
    [InlineData("2026-08-04 09:00:00+03:00")]
    public async Task CreateTask_GivenNonIsoDueTimestamp_WhenCreateRequested_ThenRejectsTask(string dueAt)
    {
        var store = new MemoryStore();
        var handler = new CreateOneShotTaskHandler(store, new TestClock());

        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new("Task", dueAt, "once"), default));

        exception.Errors.Keys.ShouldContain("dueAt");
        store.Tasks.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("2026-08-04T09:00:00.1234567+03:00")]
    [InlineData("2026-08-04T06:00:00Z")]
    public async Task CreateTask_GivenFractionalOrUtcDueTimestamp_WhenCreateRequested_ThenCreatesTask(string dueAt)
    {
        var store = new MemoryStore();
        var handler = new CreateOneShotTaskHandler(store, new TestClock());

        var task = await handler.Handle(new("Task", dueAt, "once"), default);

        task.DueAt.ShouldBe(DateTimeOffset.Parse(dueAt, CultureInfo.InvariantCulture));
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

        first.Summary.ShouldBe(new MorningReportSummary(1, 1, 1));
        first.Items.Count.ShouldBe(2);
        first.Items.Single(x => x.Id == 2).DaysOverdue.ShouldBe(3);
        first.Items.ShouldBe(second.Items);
        store.Reads.ShouldBe(2);
    }

    [Fact]
    public async Task Uses_configured_timezone_for_date_boundary()
    {
        var timezone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Helsinki");
        var store = new MemoryStore(new TaskItem(1, "Boundary", new DateTimeOffset(2026, 8, 3, 21, 30, 0, TimeSpan.Zero), ReminderPolicy.None, default, default));
        var report = await new MorningReportHandler(store, new TestClock(timezone)).Handle(new("2026-08-04"), default);

        report.Summary.DueToday.ShouldBe(1);
    }

    [Fact]
    public async Task MorningReport_GivenWeeklyReminderPolicy_WhenRequested_ThenReturnsContractValue()
    {
        var store = new MemoryStore(new TaskItem(1, "Weekly", new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero), ReminderPolicy.WeeklyUntilDone, default, default));

        var report = await new MorningReportHandler(store, new TestClock(TimeZoneInfo.Utc)).Handle(new("2026-08-04"), default);

        report.Items.ShouldHaveSingleItem().ReminderPolicy.ShouldBe("weekly-until-done");
    }

    [Theory]
    [InlineData(OneShotTaskStatus.Paused)]
    [InlineData(OneShotTaskStatus.Done)]
    [InlineData(OneShotTaskStatus.Cancelled)]
    public async Task MorningReport_GivenInactiveTask_WhenRequested_ThenExcludesTask(OneShotTaskStatus status)
    {
        var store = new MemoryStore(new TaskItem(1, "Inactive", new DateTimeOffset(2026, 8, 4, 8, 0, 0, TimeSpan.Zero), ReminderPolicy.None, default, default, Status: status));

        var report = await new MorningReportHandler(store, new TestClock(TimeZoneInfo.Utc)).Handle(new("2026-08-04"), default);

        report.Summary.ShouldBe(new MorningReportSummary(0, 0, 0));
        report.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListOpenOneShotTasks_GivenMixedStatuses_WhenRequested_ThenReturnsActiveAndPausedInAscendingIdOrder()
    {
        var store = new MemoryStore(
            new TaskItem(4, "Done", default, ReminderPolicy.None, default, default, Status: OneShotTaskStatus.Done),
            new TaskItem(3, "Paused", default, ReminderPolicy.None, default, default, Status: OneShotTaskStatus.Paused),
            new TaskItem(2, "Cancelled", default, ReminderPolicy.None, default, default, Status: OneShotTaskStatus.Cancelled),
            new TaskItem(1, "Active", default, ReminderPolicy.None, default, default));

        var tasks = await new ListOpenOneShotTasksHandler(store).Handle(new(), default);

        tasks.Select(task => task.Id).ShouldBe([1, 3]);
    }

    [Fact]
    public async Task ListOpenOneShotTasks_GivenTasks_WhenRequested_ThenLeavesTaskDataUnchanged()
    {
        var original = new TaskItem(1, "Active", new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.FromHours(3)), ReminderPolicy.Once, new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero));
        var store = new MemoryStore(original);

        var tasks = await new ListOpenOneShotTasksHandler(store).Handle(new(), default);

        tasks.ShouldHaveSingleItem().ShouldBe(original);
        store.Tasks.ShouldHaveSingleItem().ShouldBe(original);
        store.Updates.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("2026/08/04")]
    [InlineData("2026-8-4")]
    public async Task Rejects_invalid_report_date(string? date)
    {
        var handler = new MorningReportHandler(new MemoryStore(), new TestClock());
        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(date), default));
        exception.Errors.Keys.ShouldContain("date");
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

        task.Status.ShouldBe(expected);
        task.UpdatedAt.ShouldBe(new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero));
        store.Tasks.Single().ShouldBe(task);
        store.Updates.ShouldBe(1);
        if (expected == OneShotTaskStatus.Done)
        {
            task.CompletedAt.ShouldBe(task.UpdatedAt);
            task.CancelledAt.ShouldBeNull();
        }
        else if (expected == OneShotTaskStatus.Cancelled)
        {
            task.CompletedAt.ShouldBeNull();
            task.CancelledAt.ShouldBe(task.UpdatedAt);
        }
        else
        {
            task.CompletedAt.ShouldBeNull();
            task.CancelledAt.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Complete_GivenMissingTask_WhenCompleteRequested_ThenThrowsNotFound()
    {
        var handler = new CompleteOneShotTaskHandler(new MemoryStore(), new TestClock(), new MemoryRecurringTemplateStore());

        await Should.ThrowAsync<TaskNotFoundException>(async () => await handler.Handle(new(42), default));
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

        var exception = await Should.ThrowAsync<ValidationException>(async () => await HandlerFor(target, store)());

        exception.Errors.Keys.ShouldContain("status");
        store.Tasks.Single().ShouldBe(original);
        store.Updates.ShouldBe(0);
    }

    private static Func<ValueTask<TaskItem>> HandlerFor(OneShotTaskStatus target, MemoryStore store) => target switch
    {
        OneShotTaskStatus.Done => () => new CompleteOneShotTaskHandler(store, new TestClock(), new MemoryRecurringTemplateStore()).Handle(new(1), default),
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
        public ValueTask<IReadOnlyList<TaskItem>> GetOpenOneShotTasksAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<TaskItem>>(Tasks.Where(x => x.Status is OneShotTaskStatus.Active or OneShotTaskStatus.Paused).OrderBy(x => x.Id).ToList());
        public ValueTask<IReadOnlyList<TaskItem>> GetByRecurringTaskIdAsync(long recurringTaskId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<TaskItem>>(Tasks.Where(x => x.RecurringTaskId == recurringTaskId).OrderBy(x => x.Id).ToList());
        public ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken) => ValueTask.FromResult(Tasks.SingleOrDefault(x => x.Id == id));
        public ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Tasks[Tasks.FindIndex(x => x.Id == task.Id)] = task;
            Updates++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MemoryRecurringTemplateStore(params RecurringTaskTemplate[] templates) : IRecurringTaskTemplateStore
    {
        public List<RecurringTaskTemplate> Templates { get; } = [.. templates];
        public int Updates { get; private set; }
        public ValueTask<RecurringTaskTemplate> AddAsync(RecurringTaskTemplate template, CancellationToken cancellationToken)
        {
            template = template with { Id = Templates.Count + 1 };
            Templates.Add(template);
            return ValueTask.FromResult(template);
        }
        public ValueTask<RecurringTaskTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken) => ValueTask.FromResult(Templates.SingleOrDefault(x => x.Id == id));
        public ValueTask UpdateAsync(RecurringTaskTemplate template, CancellationToken cancellationToken)
        {
            Templates[Templates.FindIndex(x => x.Id == template.Id)] = template;
            Updates++;
            return ValueTask.CompletedTask;
        }
        public ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RecurringTaskTemplate>>(Templates.OrderBy(x => x.Id).ToList());
    }
}
