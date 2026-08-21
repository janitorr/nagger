using Nagger.Core.Tasks;
using Nagger.Core.Tasks.Domain;
using Shouldly;

namespace Nagger.Core.Tests;

public sealed class RecurringTaskFeatureTests
{
    [Theory]
    [InlineData(RecurrenceUnit.Days, 3, "2026-08-06")]
    [InlineData(RecurrenceUnit.Weeks, 1, "2026-08-10")]
    [InlineData(RecurrenceUnit.Weeks, 2, "2026-08-17")]
    [InlineData(RecurrenceUnit.Months, 1, "2026-09-03")]
    public void CalculateNextDue_GivenRule_WhenCalculated_ThenReturnsDatePlusInterval(
        RecurrenceUnit unit,
        int every,
        string expected
    )
    {
        RecurrenceCalculator
            .CalculateNextDue(new DateOnly(2026, 8, 3), new RecurrenceRule(every, unit))
            .ShouldBe(DateOnly.Parse(expected));
    }

    [Theory]
    [InlineData("2026-01-31", 1, "2026-02-28")]
    [InlineData("2026-01-31", 2, "2026-03-31")]
    [InlineData("2026-10-31", 1, "2026-11-30")]
    [InlineData("2026-12-31", 1, "2027-01-31")]
    [InlineData("2026-01-15", 11, "2026-12-15")]
    public void CalculateNextDue_GivenMonthEndCompletion_WhenAddingMonths_ThenClampsToTargetMonthEnd(
        string completionDate,
        int months,
        string expected
    )
    {
        RecurrenceCalculator
            .CalculateNextDue(DateOnly.Parse(completionDate), new RecurrenceRule(months, RecurrenceUnit.Months))
            .ShouldBe(DateOnly.Parse(expected));
    }

    [Fact]
    public async Task CreateRecurringTask_GivenValidInput_WhenCreateRequested_ThenCreatesTemplateAndFirstInstance()
    {
        var instanceStore = new MemoryRecurringTaskInstanceStore();
        var templateStore = new MemoryRecurringTemplateStore();
        var handler = new CreateRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var template = await handler.Handle(
            new("Team sync", "2026-08-04", new RecurrenceRuleInput(1, "weeks"), "once"),
            default
        );

        template.Id.ShouldBe(1);
        template.Title.ShouldBe("Team sync");
        template.Status.ShouldBe(RecurringTaskStatus.Active);
        template.Recurrence.ShouldBe(new RecurrenceRule(1, RecurrenceUnit.Weeks));
        var instance = instanceStore.Instances.ShouldHaveSingleItem();
        instance.Title.ShouldBe("Team sync");
        instance.DueAt.ShouldBe(new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));
        instance.ReminderPolicy.ShouldBe(ReminderPolicy.Once);
        instance.Status.ShouldBe(RecurringTaskInstanceStatus.Active);
        instance.RecurringTaskId.ShouldBe(template.Id);
    }

    [Theory]
    [InlineData(null, "2026-08-04", 1, "weeks", "once", "title")]
    [InlineData(" ", "2026-08-04", 1, "weeks", "once", "title")]
    [InlineData("Task", null, 1, "weeks", "once", "startDate")]
    [InlineData("Task", "08/04/2026", 1, "weeks", "once", "startDate")]
    [InlineData("Task", "2026-08-04", null, "weeks", "once", "recurrence.every")]
    [InlineData("Task", "2026-08-04", 0, "weeks", "once", "recurrence.every")]
    [InlineData("Task", "2026-08-04", 1, null, "once", "recurrence.unit")]
    [InlineData("Task", "2026-08-04", 1, "hourly", "once", "recurrence.unit")]
    [InlineData("Task", "2026-08-04", 1, "weeks", null, "reminderPolicy")]
    [InlineData("Task", "2026-08-04", 1, "weeks", "daily", "reminderPolicy")]
    public async Task CreateRecurringTask_GivenInvalidInput_WhenCreateRequested_ThenRejectsWithoutPersisting(
        string? title,
        string? startDate,
        int? every,
        string? unit,
        string? policy,
        string field
    )
    {
        var instanceStore = new MemoryRecurringTaskInstanceStore();
        var templateStore = new MemoryRecurringTemplateStore();
        var handler = new CreateRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await handler.Handle(new(title, startDate, new RecurrenceRuleInput(every, unit), policy), default)
        );

        exception.Errors.Keys.ShouldContain(field);
        instanceStore.Instances.ShouldBeEmpty();
        templateStore.Templates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("days")]
    [InlineData("months")]
    public async Task CreateRecurringTask_GivenValidUnit_WhenCreateRequested_ThenCreatesTemplate(string unit)
    {
        var instanceStore = new MemoryRecurringTaskInstanceStore();
        var handler = new CreateRecurringTaskHandler(
            new MemoryRecurringTemplateStore(),
            instanceStore,
            new TestClock()
        );

        var template = await handler.Handle(
            new("Team sync", "2026-08-04", new RecurrenceRuleInput(2, unit), "once"),
            default
        );

        template.Recurrence.Unit.ToContractValue().ShouldBe(unit);
        instanceStore.Instances.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreateRecurringTask_GivenStartDateToday_WhenCreateRequested_ThenCreatesTemplate()
    {
        var handler = new CreateRecurringTaskHandler(
            new MemoryRecurringTemplateStore(),
            new MemoryRecurringTaskInstanceStore(),
            new TestClock()
        );

        var template = await handler.Handle(
            new("Team sync", "2026-08-03", new RecurrenceRuleInput(1, "weeks"), "once"),
            default
        );

        template.StartDate.ShouldBe(new DateOnly(2026, 8, 3));
    }

    [Fact]
    public async Task CreateRecurringTask_GivenPastStartDate_WhenCreateRequested_ThenRejectsStartDate()
    {
        var handler = new CreateRecurringTaskHandler(
            new MemoryRecurringTemplateStore(),
            new MemoryRecurringTaskInstanceStore(),
            new TestClock()
        );

        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await handler.Handle(new("Task", "2026-08-02", new RecurrenceRuleInput(1, "weeks"), "once"), default)
        );

        exception.Errors.Keys.ShouldContain("startDate");
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenTemplateWithActiveInstance_WhenCompleteRequested_ThenCompletesInstanceAndCreatesNext()
    {
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(
                1,
                1,
                "Team sync",
                new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.FromHours(3)),
                ReminderPolicy.Once,
                default,
                default
            )
        );
        var templateStore = new MemoryRecurringTemplateStore(
            new RecurringTaskTemplate(
                1,
                "Team sync",
                new DateOnly(2026, 8, 4),
                new RecurrenceRule(1, RecurrenceUnit.Weeks),
                ReminderPolicy.Once,
                RecurringTaskStatus.Active,
                default,
                default
            )
        );
        var handler = new CompleteRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var completed = await handler.Handle(new(1), default);

        completed.Status.ShouldBe(RecurringTaskInstanceStatus.Done);
        completed.CompletedAt.ShouldNotBeNull();
        completed.RecurringTaskId.ShouldBe(1);
        instanceStore.Instances.Count.ShouldBe(2);
        var next = instanceStore.Instances.Single(x => x.Status == RecurringTaskInstanceStatus.Active);
        next.Title.ShouldBe("Team sync");
        next.RecurringTaskId.ShouldBe(1);
        next.DueAt.ShouldBe(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenTemplateWithoutActiveInstance_WhenCompleteRequested_ThenRejectsWithoutChangingState()
    {
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(
                1,
                1,
                "Team sync",
                default,
                ReminderPolicy.Once,
                default,
                default,
                Status: RecurringTaskInstanceStatus.Done
            )
        );
        var templateStore = new MemoryRecurringTemplateStore(Template());
        var handler = new CompleteRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(1), default));

        exception.Errors["status"].ShouldBe(["Recurring task has no active instance to complete."]);
        instanceStore.Instances.ShouldHaveSingleItem().Status.ShouldBe(RecurringTaskInstanceStatus.Done);
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenMissingTemplate_WhenCompleteRequested_ThenThrowsNotFound()
    {
        var handler = new CompleteRecurringTaskHandler(
            new MemoryRecurringTemplateStore(),
            new MemoryRecurringTaskInstanceStore(),
            new TestClock()
        );

        await Should.ThrowAsync<RecurringTaskNotFoundException>(async () => await handler.Handle(new(42), default));
    }

    [Fact]
    public async Task PauseRecurringTask_GivenActiveTemplateWithActiveInstance_WhenPauseRequested_ThenPausesTemplateAndInstance()
    {
        var templateStore = new MemoryRecurringTemplateStore(Template());
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(1, 1, "Team sync", default, ReminderPolicy.Once, default, default)
        );
        var handler = new PauseRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var updated = await handler.Handle(new(1), default);

        updated.Status.ShouldBe(RecurringTaskStatus.Paused);
        templateStore.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Paused);
        instanceStore.Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Paused);
    }

    [Fact]
    public async Task PauseRecurringTask_GivenNoActiveInstance_WhenPauseRequested_ThenPausesTemplateAndLeavesInstances()
    {
        var templateStore = new MemoryRecurringTemplateStore(Template());
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(
                1,
                1,
                "Team sync",
                default,
                ReminderPolicy.Once,
                default,
                default,
                Status: RecurringTaskInstanceStatus.Paused
            )
        );
        var handler = new PauseRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var updated = await handler.Handle(new(1), default);

        updated.Status.ShouldBe(RecurringTaskStatus.Paused);
        instanceStore.Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Paused);
    }

    [Fact]
    public async Task PauseRecurringTask_GivenPausedTemplate_WhenPauseRequested_ThenRejectsWithoutChanges()
    {
        var templateStore = new MemoryRecurringTemplateStore(Template(status: RecurringTaskStatus.Paused));
        var instanceStore = new MemoryRecurringTaskInstanceStore();
        var handler = new PauseRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(1), default));

        exception.Errors.Keys.ShouldContain("status");
        templateStore.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Paused);
        instanceStore.Instances.ShouldBeEmpty();
    }

    [Fact]
    public async Task ResumeRecurringTask_GivenPausedTemplateWithPausedInstance_WhenResumeRequested_ThenResumesTemplateAndInstance()
    {
        var templateStore = new MemoryRecurringTemplateStore(Template(status: RecurringTaskStatus.Paused));
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(
                1,
                1,
                "Team sync",
                default,
                ReminderPolicy.Once,
                default,
                default,
                Status: RecurringTaskInstanceStatus.Paused
            )
        );
        var handler = new ResumeRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var updated = await handler.Handle(new(1), default);

        updated.Status.ShouldBe(RecurringTaskStatus.Active);
        templateStore.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Active);
        instanceStore.Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Active);
    }

    [Fact]
    public async Task ResumeRecurringTask_GivenNoPausedInstance_WhenResumeRequested_ThenResumesTemplateAndLeavesInstances()
    {
        var templateStore = new MemoryRecurringTemplateStore(Template(status: RecurringTaskStatus.Paused));
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(1, 1, "Team sync", default, ReminderPolicy.Once, default, default)
        );
        var handler = new ResumeRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var updated = await handler.Handle(new(1), default);

        updated.Status.ShouldBe(RecurringTaskStatus.Active);
        instanceStore.Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Active);
    }

    [Fact]
    public async Task ResumeRecurringTask_GivenActiveTemplate_WhenResumeRequested_ThenRejectsWithoutChanges()
    {
        var templateStore = new MemoryRecurringTemplateStore(Template());
        var instanceStore = new MemoryRecurringTaskInstanceStore();
        var handler = new ResumeRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(1), default));

        exception.Errors.Keys.ShouldContain("status");
        templateStore.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Active);
    }

    [Fact]
    public async Task CancelRecurringTask_GivenActiveTemplateWithOpenInstances_WhenCancelRequested_ThenCancelsTemplateAndOpenInstances()
    {
        var templateStore = new MemoryRecurringTemplateStore(Template());
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(1, 1, "Team sync", default, ReminderPolicy.Once, default, default),
            new RecurringTaskInstance(
                2,
                1,
                "Team sync",
                default,
                ReminderPolicy.Once,
                default,
                default,
                Status: RecurringTaskInstanceStatus.Paused
            ),
            new RecurringTaskInstance(
                3,
                1,
                "Team sync",
                default,
                ReminderPolicy.Once,
                default,
                default,
                Status: RecurringTaskInstanceStatus.Done
            )
        );
        var handler = new CancelRecurringTaskHandler(templateStore, instanceStore, new TestClock());

        var updated = await handler.Handle(new(1), default);

        updated.Status.ShouldBe(RecurringTaskStatus.Cancelled);
        updated.CancelledAt.ShouldNotBeNull();
        templateStore.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Cancelled);
        templateStore.Templates.Single().CancelledAt.ShouldNotBeNull();
        instanceStore.Instances.Single(x => x.Id == 1).Status.ShouldBe(RecurringTaskInstanceStatus.Cancelled);
        instanceStore.Instances.Single(x => x.Id == 2).Status.ShouldBe(RecurringTaskInstanceStatus.Cancelled);
        instanceStore.Instances.Single(x => x.Id == 3).Status.ShouldBe(RecurringTaskInstanceStatus.Done);
    }

    [Fact]
    public void RecurringTaskStatuses_GivenContractValue_WhenParsed_ThenReturnsStatus()
    {
        RecurringTaskStatuses.FromContractValue("active").ShouldBe(RecurringTaskStatus.Active);
        RecurringTaskStatuses.FromContractValue("paused").ShouldBe(RecurringTaskStatus.Paused);
        RecurringTaskStatuses.FromContractValue("cancelled").ShouldBe(RecurringTaskStatus.Cancelled);
    }

    [Fact]
    public void RecurringTaskInstanceStatuses_GivenContractValue_WhenParsed_ThenReturnsStatus()
    {
        RecurringTaskInstanceStatuses.FromContractValue("active").ShouldBe(RecurringTaskInstanceStatus.Active);
        RecurringTaskInstanceStatuses.FromContractValue("paused").ShouldBe(RecurringTaskInstanceStatus.Paused);
        RecurringTaskInstanceStatuses.FromContractValue("done").ShouldBe(RecurringTaskInstanceStatus.Done);
        RecurringTaskInstanceStatuses.FromContractValue("cancelled").ShouldBe(RecurringTaskInstanceStatus.Cancelled);
    }

    [Fact]
    public async Task ListRecurringTemplates_GivenTemplates_WhenRequested_ThenReturnsAllInAscendingIdOrder()
    {
        var templateStore = new MemoryRecurringTemplateStore(
            Template(Id: 2, title: "Second"),
            Template(Id: 1, title: "First")
        );

        var templates = await new ListRecurringTemplatesHandler(templateStore).Handle(new(), default);

        templates.Select(x => x.Id).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task CompleteOneShotTask_GivenTask_WhenCompleteRequested_ThenDoesNotCreateRecurringInstance()
    {
        var taskStore = new MemoryStore(new TaskItem(1, "Task", default, ReminderPolicy.None, default, default));
        var handler = new CompleteOneShotTaskHandler(taskStore, new TestClock());

        var completed = await handler.Handle(new(1), default);

        completed.Status.ShouldBe(OneShotTaskStatus.Done);
        taskStore.Tasks.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenLateEveningCompletionInHelsinki_WhenCompleteRequested_ThenNextDueUsesLocalCompletionDate()
    {
        var helsinki = TimeZoneInfo.FindSystemTimeZoneById("Europe/Helsinki");
        var now = new DateTimeOffset(2026, 8, 3, 22, 30, 0, TimeSpan.Zero);
        var instanceStore = new MemoryRecurringTaskInstanceStore(
            new RecurringTaskInstance(
                1,
                1,
                "Team sync",
                new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.FromHours(3)),
                ReminderPolicy.Once,
                default,
                default
            )
        );
        var templateStore = new MemoryRecurringTemplateStore(
            new RecurringTaskTemplate(
                1,
                "Team sync",
                new DateOnly(2026, 8, 4),
                new RecurrenceRule(1, RecurrenceUnit.Weeks),
                ReminderPolicy.Once,
                RecurringTaskStatus.Active,
                default,
                default
            )
        );
        var handler = new CompleteRecurringTaskHandler(templateStore, instanceStore, new TestClock(now, helsinki));

        var completed = await handler.Handle(new(1), default);

        completed.Status.ShouldBe(RecurringTaskInstanceStatus.Done);
        var next = instanceStore.Instances.Single(x => x.Status == RecurringTaskInstanceStatus.Active);
        next.DueAt.ShouldBe(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.FromHours(3)));
    }

    private static RecurringTaskTemplate Template(
        long Id = 1,
        string title = "Team sync",
        RecurringTaskStatus status = RecurringTaskStatus.Active
    ) =>
        new(
            Id,
            title,
            new DateOnly(2026, 8, 4),
            new RecurrenceRule(1, RecurrenceUnit.Weeks),
            ReminderPolicy.Once,
            status,
            default,
            default
        );

    private sealed class TestClock(DateTimeOffset? utcNow = null, TimeZoneInfo? timeZone = null) : IClock
    {
        public DateTimeOffset UtcNow => utcNow ?? new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero);
        public TimeZoneInfo TimeZone => timeZone ?? TimeZoneInfo.Utc;
    }

    private sealed class MemoryStore(params TaskItem[] tasks) : ITaskStore
    {
        public List<TaskItem> Tasks { get; } = [.. tasks];

        public ValueTask<TaskItem> AddAsync(TaskItem task, CancellationToken cancellationToken)
        {
            task = task with { Id = Tasks.Count + 1 };
            Tasks.Add(task);
            return ValueTask.FromResult(task);
        }

        public ValueTask<TaskItem?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Tasks.SingleOrDefault(x => x.Id == id));

        public ValueTask UpdateAsync(TaskItem task, CancellationToken cancellationToken)
        {
            Tasks[Tasks.FindIndex(x => x.Id == task.Id)] = task;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<TaskItem>> GetActiveAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<TaskItem>>(
                Tasks.Where(x => x.Status == OneShotTaskStatus.Active).ToList()
            );

        public ValueTask<IReadOnlyList<TaskItem>> GetOpenOneShotTasksAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<TaskItem>>(
                Tasks
                    .Where(x => x.Status is OneShotTaskStatus.Active or OneShotTaskStatus.Paused)
                    .OrderBy(x => x.Id)
                    .ToList()
            );
    }

    private sealed class MemoryRecurringTemplateStore(params RecurringTaskTemplate[] templates)
        : IRecurringTaskTemplateStore
    {
        public List<RecurringTaskTemplate> Templates { get; } = [.. templates];

        public ValueTask<RecurringTaskTemplate> AddAsync(
            RecurringTaskTemplate template,
            CancellationToken cancellationToken
        )
        {
            template = template with { Id = Templates.Count + 1 };
            Templates.Add(template);
            return ValueTask.FromResult(template);
        }

        public ValueTask<RecurringTaskTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Templates.SingleOrDefault(x => x.Id == id));

        public ValueTask UpdateAsync(RecurringTaskTemplate template, CancellationToken cancellationToken)
        {
            Templates[Templates.FindIndex(x => x.Id == template.Id)] = template;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RecurringTaskTemplate>>(Templates.OrderBy(x => x.Id).ToList());
    }

    private sealed class MemoryRecurringTaskInstanceStore(params RecurringTaskInstance[] instances)
        : IRecurringTaskInstanceStore
    {
        public List<RecurringTaskInstance> Instances { get; } = [.. instances];

        public ValueTask<RecurringTaskInstance> AddAsync(
            RecurringTaskInstance instance,
            CancellationToken cancellationToken
        )
        {
            instance = instance with { Id = Instances.Count + 1 };
            Instances.Add(instance);
            return ValueTask.FromResult(instance);
        }

        public ValueTask<RecurringTaskInstance?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Instances.SingleOrDefault(x => x.Id == id));

        public ValueTask UpdateAsync(RecurringTaskInstance instance, CancellationToken cancellationToken)
        {
            Instances[Instances.FindIndex(x => x.Id == instance.Id)] = instance;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<RecurringTaskInstance>> GetActiveAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RecurringTaskInstance>>(
                Instances.Where(x => x.Status == RecurringTaskInstanceStatus.Active).ToList()
            );

        public ValueTask<IReadOnlyList<RecurringTaskInstance>> GetByTemplateIdAsync(
            long recurringTaskId,
            CancellationToken cancellationToken
        ) =>
            ValueTask.FromResult<IReadOnlyList<RecurringTaskInstance>>(
                Instances.Where(x => x.RecurringTaskId == recurringTaskId).OrderBy(x => x.Id).ToList()
            );
    }
}
