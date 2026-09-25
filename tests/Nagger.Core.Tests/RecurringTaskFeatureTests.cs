using System.Globalization;
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
            .ShouldBe(DateOnly.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture));
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
            .CalculateNextDue(
                DateOnly.ParseExact(completionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                new RecurrenceRule(months, RecurrenceUnit.Months)
            )
            .ShouldBe(DateOnly.ParseExact(expected, "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task CreateRecurringTask_GivenValidInput_WhenCreateRequested_ThenCreatesTemplateAndFirstInstance()
    {
        var store = new MemoryRecurringTemplateStore();
        var handler = new CreateRecurringTaskHandler(store, new TestTimeProvider());
        var result = await handler.Handle(new("Team sync", "2026-08-04", new RecurrenceRuleInput(1, "weeks")), default);
        result.Template.Id.ShouldBe(1);
        result.Template.Title.ShouldBe("Team sync");
        result.Template.Status.ShouldBe(RecurringTaskStatus.Active);
        result.Template.Recurrence.ShouldBe(new RecurrenceRule(1, RecurrenceUnit.Weeks));
        result.Template.Instances.ShouldHaveSingleItem();
        var instance = result.FirstInstance;
        instance.ShouldBe(result.Template.Instances.Single());
        instance.Id.ShouldBe(1);
        instance.RecurringTaskId.ShouldBe(result.Template.Id);
        instance.Title.ShouldBe("Team sync");
        instance.DueAt.ShouldBe(new DateTimeOffset(2026, 8, 4, 0, 0, 0, TimeSpan.Zero));
        instance.Status.ShouldBe(RecurringTaskInstanceStatus.Active);
    }

    [Theory]
    [InlineData(null, "2026-08-04", 1, "weeks", "title")]
    [InlineData(" ", "2026-08-04", 1, "weeks", "title")]
    [InlineData("Task", null, 1, "weeks", "startDate")]
    [InlineData("Task", "08/04/2026", 1, "weeks", "startDate")]
    [InlineData("Task", "2026-08-04", null, "weeks", "recurrence.every")]
    [InlineData("Task", "2026-08-04", 0, "weeks", "recurrence.every")]
    [InlineData("Task", "2026-08-04", 1, null, "recurrence.unit")]
    [InlineData("Task", "2026-08-04", 1, "hourly", "recurrence.unit")]
    public async Task CreateRecurringTask_GivenInvalidInput_WhenCreateRequested_ThenRejectsWithoutPersisting(
        string? title,
        string? startDate,
        int? every,
        string? unit,
        string field
    )
    {
        var store = new MemoryRecurringTemplateStore();
        var handler = new CreateRecurringTaskHandler(store, new TestTimeProvider());
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await handler.Handle(new(title, startDate, new RecurrenceRuleInput(every, unit)), default)
        );
        exception.Errors.Keys.ShouldContain(field);
        store.Templates.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("days")]
    [InlineData("months")]
    public async Task CreateRecurringTask_GivenValidUnit_WhenCreateRequested_ThenCreatesTemplate(string unit)
    {
        var store = new MemoryRecurringTemplateStore();
        var handler = new CreateRecurringTaskHandler(store, new TestTimeProvider());
        var result = await handler.Handle(new("Team sync", "2026-08-04", new RecurrenceRuleInput(2, unit)), default);
        result.Template.Recurrence.Unit.ToContractValue().ShouldBe(unit);
        result.Template.Instances.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreateRecurringTask_GivenStartDateToday_WhenCreateRequested_ThenCreatesTemplate()
    {
        var handler = new CreateRecurringTaskHandler(new MemoryRecurringTemplateStore(), new TestTimeProvider());
        var result = await handler.Handle(new("Team sync", "2026-08-03", new RecurrenceRuleInput(1, "weeks")), default);
        result.Template.StartDate.ShouldBe(new DateOnly(2026, 8, 3));
    }

    [Fact]
    public async Task CreateRecurringTask_GivenPastStartDate_WhenCreateRequested_ThenRejectsStartDate()
    {
        var handler = new CreateRecurringTaskHandler(new MemoryRecurringTemplateStore(), new TestTimeProvider());
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await handler.Handle(new("Task", "2026-08-02", new RecurrenceRuleInput(1, "weeks")), default)
        );
        exception.Errors.Keys.ShouldContain("startDate");
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenActiveInstance_WhenCompleteRequested_ThenCompletesInstanceAndCreatesNext()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(
                instances: new RecurringTaskInstance(
                    1,
                    1,
                    "Team sync",
                    new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.FromHours(3)),
                    default,
                    default
                )
            )
        );
        var handler = new CompleteRecurringTaskHandler(store, new TestTimeProvider());
        var result = await handler.Handle(new(1), default);
        result.CompletedInstance.Status.ShouldBe(RecurringTaskInstanceStatus.Done);
        result.CompletedInstance.CompletedAt.ShouldNotBeNull();
        result.CompletedInstance.RecurringTaskId.ShouldBe(1);
        result.NextInstance.Status.ShouldBe(RecurringTaskInstanceStatus.Active);
        result.NextInstance.Title.ShouldBe("Team sync");
        result.NextInstance.RecurringTaskId.ShouldBe(1);
        result.NextInstance.DueAt.ShouldBe(new DateTimeOffset(2026, 8, 10, 0, 0, 0, TimeSpan.Zero));
        store.Templates.Single().Instances.Count.ShouldBe(2);
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenNoActiveInstance_WhenCompleteRequested_ThenRejectsWithoutChangingState()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(
                instances: new RecurringTaskInstance(
                    1,
                    1,
                    "Team sync",
                    default,
                    default,
                    default,
                    Status: RecurringTaskInstanceStatus.Paused
                )
            )
        );
        var handler = new CompleteRecurringTaskHandler(store, new TestTimeProvider());
        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(1), default));
        exception.Errors["status"].ShouldBe(["Recurring task has no active instance to complete."]);
        store.Templates.Single().Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Paused);
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenMissingTemplate_WhenCompleteRequested_ThenThrowsNotFound()
    {
        var handler = new CompleteRecurringTaskHandler(new MemoryRecurringTemplateStore(), new TestTimeProvider());
        await Should.ThrowAsync<RecurringTaskNotFoundException>(async () => await handler.Handle(new(42), default));
    }

    [Fact]
    public async Task PauseRecurringTask_GivenActiveTemplateWithActiveInstance_WhenPauseRequested_ThenPausesTemplateAndInstance()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(instances: new RecurringTaskInstance(1, 1, "Team sync", default, default, default))
        );
        var handler = new PauseRecurringTaskHandler(store, new TestTimeProvider());
        var updated = await handler.Handle(new(1), default);
        updated.Status.ShouldBe(RecurringTaskStatus.Paused);
        store.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Paused);
        store.Templates.Single().Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Paused);
    }

    [Fact]
    public async Task PauseRecurringTask_GivenNoActiveInstance_WhenPauseRequested_ThenPausesTemplateAndLeavesInstances()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(
                instances: new RecurringTaskInstance(
                    1,
                    1,
                    "Team sync",
                    default,
                    default,
                    default,
                    Status: RecurringTaskInstanceStatus.Paused
                )
            )
        );
        var handler = new PauseRecurringTaskHandler(store, new TestTimeProvider());
        var updated = await handler.Handle(new(1), default);
        updated.Status.ShouldBe(RecurringTaskStatus.Paused);
        store.Templates.Single().Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Paused);
    }

    [Fact]
    public async Task PauseRecurringTask_GivenPausedTemplate_WhenPauseRequested_ThenRejectsWithoutChanges()
    {
        var store = new MemoryRecurringTemplateStore(Template(status: RecurringTaskStatus.Paused));
        var handler = new PauseRecurringTaskHandler(store, new TestTimeProvider());
        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(1), default));
        exception.Errors.Keys.ShouldContain("status");
        store.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Paused);
    }

    [Fact]
    public async Task ResumeRecurringTask_GivenPausedTemplateWithPausedInstance_WhenResumeRequested_ThenResumesTemplateAndInstance()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(
                status: RecurringTaskStatus.Paused,
                instances: new RecurringTaskInstance(
                    1,
                    1,
                    "Team sync",
                    default,
                    default,
                    default,
                    Status: RecurringTaskInstanceStatus.Paused
                )
            )
        );
        var handler = new ResumeRecurringTaskHandler(store, new TestTimeProvider());
        var updated = await handler.Handle(new(1), default);
        updated.Status.ShouldBe(RecurringTaskStatus.Active);
        store.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Active);
        store.Templates.Single().Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Active);
    }

    [Fact]
    public async Task ResumeRecurringTask_GivenNoPausedInstance_WhenResumeRequested_ThenResumesTemplateAndLeavesInstances()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(
                status: RecurringTaskStatus.Paused,
                instances: new RecurringTaskInstance(1, 1, "Team sync", default, default, default)
            )
        );
        var handler = new ResumeRecurringTaskHandler(store, new TestTimeProvider());
        var updated = await handler.Handle(new(1), default);
        updated.Status.ShouldBe(RecurringTaskStatus.Active);
        store.Templates.Single().Instances.Single().Status.ShouldBe(RecurringTaskInstanceStatus.Active);
    }

    [Fact]
    public async Task ResumeRecurringTask_GivenActiveTemplate_WhenResumeRequested_ThenRejectsWithoutChanges()
    {
        var store = new MemoryRecurringTemplateStore(Template());
        var handler = new ResumeRecurringTaskHandler(store, new TestTimeProvider());
        var exception = await Should.ThrowAsync<ValidationException>(async () => await handler.Handle(new(1), default));
        exception.Errors.Keys.ShouldContain("status");
        store.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Active);
    }

    [Fact]
    public async Task CancelRecurringTask_GivenOpenInstances_WhenCancelRequested_ThenCancelsTemplateAndOpenInstances()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(
                instances:
                [
                    new RecurringTaskInstance(1, 1, "Team sync", default, default, default),
                    new RecurringTaskInstance(
                        2,
                        1,
                        "Team sync",
                        default,
                        default,
                        default,
                        Status: RecurringTaskInstanceStatus.Paused
                    ),
                ]
            )
        );
        var handler = new CancelRecurringTaskHandler(store, new TestTimeProvider());
        var updated = await handler.Handle(new(1), default);
        updated.Status.ShouldBe(RecurringTaskStatus.Cancelled);
        updated.CancelledAt.ShouldNotBeNull();
        store.Templates.Single().Status.ShouldBe(RecurringTaskStatus.Cancelled);
        store.Templates.Single().CancelledAt.ShouldNotBeNull();
        store.Templates.Single().Instances.ShouldAllBe(x => x.Status == RecurringTaskInstanceStatus.Cancelled);
    }

    [Fact]
    public void RecurringTaskStatuses_GivenContractValue_WhenParsed_ThenReturnsStatus()
    {
        RecurringTaskStatuses.FromContractValue("active").ShouldBe(RecurringTaskStatus.Active);
        RecurringTaskStatuses.FromContractValue("paused").ShouldBe(RecurringTaskStatus.Paused);
        RecurringTaskStatuses.FromContractValue("cancelled").ShouldBe(RecurringTaskStatus.Cancelled);
    }

    [Theory]
    [InlineData("days", RecurrenceUnit.Days)]
    [InlineData("weeks", RecurrenceUnit.Weeks)]
    [InlineData("months", RecurrenceUnit.Months)]
    public void RecurrenceUnits_GivenValidContractValue_WhenParsed_ThenReturnsUnit(
        string value,
        RecurrenceUnit expected
    )
    {
        RecurrenceUnits.TryParse(value, out var unit).ShouldBeTrue();
        unit.ShouldBe(expected);
    }

    [Fact]
    public void RecurrenceUnits_GivenInvalidContractValue_WhenParsed_ThenReturnsFalse()
    {
        RecurrenceUnits.TryParse("hourly", out var unit).ShouldBeFalse();
        unit.ShouldBe(default);
    }

    [Theory]
    [InlineData("days", RecurrenceUnit.Days)]
    [InlineData("weeks", RecurrenceUnit.Weeks)]
    [InlineData("months", RecurrenceUnit.Months)]
    public void RecurrenceUnits_GivenValidContractValue_WhenConverted_ThenReturnsUnit(
        string value,
        RecurrenceUnit expected
    )
    {
        RecurrenceUnits.FromContractValue(value).ShouldBe(expected);
    }

    [Fact]
    public void RecurrenceUnits_GivenInvalidContractValue_WhenConverted_ThenThrows()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => RecurrenceUnits.FromContractValue("hourly"));
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
        var store = new MemoryRecurringTemplateStore(Template(Id: 2, title: "Second"), Template(Id: 1, title: "First"));
        var templates = await new ListRecurringTemplatesHandler(store).Handle(new(), default);
        templates.Select(x => x.Id).ShouldBe([1, 2]);
    }

    [Fact]
    public async Task CompleteOneShotTask_GivenTask_WhenCompleteRequested_ThenDoesNotCreateRecurringInstance()
    {
        var taskStore = new MemoryStore(new TaskItem(1, "Task", default, default, default));
        var handler = new CompleteOneShotTaskHandler(taskStore, new TestTimeProvider());
        var completed = await handler.Handle(new(1), default);
        completed.Status.ShouldBe(OneShotTaskStatus.Done);
        taskStore.Tasks.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenLateEveningCompletionInHelsinki_WhenCompleteRequested_ThenNextDueUsesLocalCompletionDate()
    {
        var helsinki = TimeZoneInfo.FindSystemTimeZoneById("Europe/Helsinki");
        var now = new DateTimeOffset(2026, 8, 3, 22, 30, 0, TimeSpan.Zero);
        var store = new MemoryRecurringTemplateStore(
            Template(
                instances: new RecurringTaskInstance(
                    1,
                    1,
                    "Team sync",
                    new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.FromHours(3)),
                    default,
                    default
                )
            )
        );
        var handler = new CompleteRecurringTaskHandler(store, new TestTimeProvider(now, helsinki));
        var result = await handler.Handle(new(1), default);
        result.CompletedInstance.Status.ShouldBe(RecurringTaskInstanceStatus.Done);
        result.NextInstance.DueAt.ShouldBe(new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.FromHours(3)));
    }

    [Fact]
    public async Task CompleteRecurringTask_GivenAdvancingClock_WhenCompleteRequested_ThenEveryTimestampUsesOneInstant()
    {
        var store = new MemoryRecurringTemplateStore(
            Template(
                instances: new RecurringTaskInstance(
                    1,
                    1,
                    "Team sync",
                    new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.FromHours(3)),
                    default,
                    default
                )
            )
        );
        var handler = new CompleteRecurringTaskHandler(store, new AdvancingTimeProvider());
        var result = await handler.Handle(new(1), default);
        result.CompletedInstance.CompletedAt.ShouldBe(result.CompletedInstance.UpdatedAt);
        result.NextInstance.CreatedAt.ShouldBe(result.NextInstance.UpdatedAt);
        result.CompletedInstance.UpdatedAt.ShouldBe(result.NextInstance.CreatedAt);
    }

    [Fact]
    public void CancelTemplate_GivenTerminalInstance_WhenCancelled_ThenLeavesTerminalInstanceUnchanged()
    {
        var template = Template(
            instances:
            [
                new RecurringTaskInstance(1, 1, "Team sync", default, default, default),
                new RecurringTaskInstance(
                    2,
                    1,
                    "Team sync",
                    default,
                    default,
                    default,
                    Status: RecurringTaskInstanceStatus.Paused
                ),
                new RecurringTaskInstance(
                    3,
                    1,
                    "Team sync",
                    default,
                    default,
                    default,
                    Status: RecurringTaskInstanceStatus.Done
                ),
            ]
        );
        var updated = template.Cancel(new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero));
        updated.Instances.Single(x => x.Id == 1).Status.ShouldBe(RecurringTaskInstanceStatus.Cancelled);
        updated.Instances.Single(x => x.Id == 2).Status.ShouldBe(RecurringTaskInstanceStatus.Cancelled);
        updated.Instances.Single(x => x.Id == 3).Status.ShouldBe(RecurringTaskInstanceStatus.Done);
    }

    [Fact]
    public void CancelTemplate_GivenCancelledTemplate_WhenCancelRequested_ThenRejects()
    {
        var template = Template(status: RecurringTaskStatus.Cancelled);
        var exception = Should.Throw<ValidationException>(() =>
            template.Cancel(new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero))
        );
        exception.Errors.Keys.ShouldContain("status");
    }

    [Fact]
    public void CompleteTemplate_GivenMultipleInstances_WhenCompleted_ThenCompletesActiveAndAppendsNext()
    {
        var template = Template(
            instances:
            [
                new RecurringTaskInstance(1, 1, "Team sync", default, default, default),
                new RecurringTaskInstance(
                    2,
                    1,
                    "Team sync",
                    default,
                    default,
                    default,
                    Status: RecurringTaskInstanceStatus.Paused
                ),
            ]
        );
        var updated = template.Complete(new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);
        updated.Instances.Count.ShouldBe(3);
        updated.Instances[0].Status.ShouldBe(RecurringTaskInstanceStatus.Done);
        updated.Instances[1].Status.ShouldBe(RecurringTaskInstanceStatus.Paused);
        updated.Instances[2].Status.ShouldBe(RecurringTaskInstanceStatus.Active);
    }

    [Fact]
    public void CompleteRecurringInstance_GivenNonActiveInstance_WhenCompleteRequested_ThenRejects()
    {
        var instance = new RecurringTaskInstance(
            1,
            1,
            "Team sync",
            default,
            default,
            default,
            Status: RecurringTaskInstanceStatus.Done
        );
        var exception = Should.Throw<ValidationException>(() => instance.Complete(default));
        exception.Errors.Keys.ShouldContain("status");
    }

    [Fact]
    public void PauseRecurringInstance_GivenNonActiveInstance_WhenPauseRequested_ThenRejects()
    {
        var instance = new RecurringTaskInstance(
            1,
            1,
            "Team sync",
            default,
            default,
            default,
            Status: RecurringTaskInstanceStatus.Paused
        );
        var exception = Should.Throw<ValidationException>(() => instance.Pause(default));
        exception.Errors.Keys.ShouldContain("status");
    }

    [Fact]
    public void ResumeRecurringInstance_GivenNonPausedInstance_WhenResumeRequested_ThenRejects()
    {
        var instance = new RecurringTaskInstance(1, 1, "Team sync", default, default, default);
        var exception = Should.Throw<ValidationException>(() => instance.Resume(default));
        exception.Errors.Keys.ShouldContain("status");
    }

    [Fact]
    public void CancelRecurringInstance_GivenTerminalInstance_WhenCancelRequested_ThenRejects()
    {
        var instance = new RecurringTaskInstance(
            1,
            1,
            "Team sync",
            default,
            default,
            default,
            Status: RecurringTaskInstanceStatus.Done
        );
        var exception = Should.Throw<ValidationException>(() => instance.Cancel(default));
        exception.Errors.Keys.ShouldContain("status");
    }

    private static RecurringTaskTemplate Template(
        long Id = 1,
        string title = "Team sync",
        RecurringTaskStatus status = RecurringTaskStatus.Active,
        params RecurringTaskInstance[] instances
    ) =>
        new(
            Id,
            title,
            new DateOnly(2026, 8, 4),
            new RecurrenceRule(1, RecurrenceUnit.Weeks),
            status,
            default,
            default,
            Instances: instances
        );

    private sealed class TestTimeProvider(DateTimeOffset? utcNow = null, TimeZoneInfo? timeZone = null) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow ?? new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero);

        public override TimeZoneInfo LocalTimeZone => timeZone ?? TimeZoneInfo.Utc;
    }

    private sealed class AdvancingTimeProvider : TimeProvider
    {
        private int _calls;
        private DateTimeOffset _current = new(2026, 8, 3, 6, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _current.AddMinutes(_calls++);
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

    private sealed class MemoryRecurringTemplateStore : IRecurringTaskTemplateStore
    {
        private long _nextTemplateId = 1;
        private long _nextInstanceId = 1;

        public List<RecurringTaskTemplate> Templates { get; } = [];

        public MemoryRecurringTemplateStore(params RecurringTaskTemplate[] templates)
        {
            foreach (var template in templates)
                Templates.Add(AssignIds(template, template.Id == 0 ? _nextTemplateId++ : template.Id));
            _nextTemplateId = Templates.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1;
            _nextInstanceId = Templates.SelectMany(x => x.Instances).Select(x => x.Id).DefaultIfEmpty(0).Max() + 1;
        }

        public ValueTask<RecurringTaskTemplate> AddAsync(
            RecurringTaskTemplate recurringTemplate,
            CancellationToken cancellationToken
        )
        {
            var persisted = AssignIds(
                recurringTemplate,
                recurringTemplate.Id == 0 ? _nextTemplateId++ : recurringTemplate.Id
            );
            Templates.Add(persisted);
            return ValueTask.FromResult(persisted);
        }

        public ValueTask<RecurringTaskTemplate?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            var template = Templates.SingleOrDefault(x => x.Id == id);
            return ValueTask.FromResult(template is null ? null : OpenView(template));
        }

        public ValueTask<RecurringTaskTemplate> UpdateAsync(
            RecurringTaskTemplate recurringTemplate,
            CancellationToken cancellationToken
        )
        {
            var persisted = AssignIds(recurringTemplate, recurringTemplate.Id);
            Templates[Templates.FindIndex(x => x.Id == recurringTemplate.Id)] = persisted;
            return ValueTask.FromResult(persisted);
        }

        public ValueTask<IReadOnlyList<RecurringTaskTemplate>> GetAllAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RecurringTaskTemplate>>(Templates.OrderBy(x => x.Id).ToList());

        private static RecurringTaskTemplate OpenView(RecurringTaskTemplate template) =>
            template with
            {
                Instances = template
                    .Instances.Where(x =>
                        x.Status is RecurringTaskInstanceStatus.Active or RecurringTaskInstanceStatus.Paused
                    )
                    .ToList(),
            };

        private RecurringTaskTemplate AssignIds(RecurringTaskTemplate template, long templateId) =>
            template with
            {
                Id = templateId,
                Instances = template
                    .Instances.Select(instance =>
                        instance.Id == 0
                            ? instance with
                            {
                                Id = _nextInstanceId++,
                                RecurringTaskId = templateId,
                            }
                            : instance with
                            {
                                RecurringTaskId = templateId,
                            }
                    )
                    .ToList(),
            };
    }
}
