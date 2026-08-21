using System.Globalization;
using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record CreateRecurringTaskCommand(
    string? Title,
    string? StartDate,
    RecurrenceRuleInput? Recurrence,
    string? ReminderPolicy) : ICommand<RecurringTaskTemplate>;

public sealed record RecurrenceRuleInput(int? Every, string? Unit);

public sealed class CreateRecurringTaskHandler(IRecurringTaskTemplateStore templateStore, IRecurringTaskInstanceStore instanceStore, IClock clock)
    : ICommandHandler<CreateRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(CreateRecurringTaskCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Title))
            errors["title"] = ["Title is required."];

        var startDate = default(DateOnly);
        if (command.StartDate is null || !DateOnly.TryParseExact(command.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
            errors["startDate"] = ["Start date must be in YYYY-MM-DD format."];

        var every = command.Recurrence?.Every;
        if (every is null || every <= 0)
            errors["recurrence.every"] = ["Recurrence every must be a positive integer."];

        var unit = default(RecurrenceUnit);
        if (!TryParseUnit(command.Recurrence?.Unit, out unit))
            errors["recurrence.unit"] = ["Recurrence unit must be days, weeks, or months."];

        var reminderPolicy = default(ReminderPolicy);
        if (!ReminderPolicies.TryParse(command.ReminderPolicy, out reminderPolicy))
            errors["reminderPolicy"] = ["Reminder policy must be none, once, or weekly-until-done."];

        if (startDate != default && startDate < Today())
            errors["startDate"] = ["Start date cannot be in the past."];

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var now = clock.UtcNow;
        var createdTemplate = await templateStore.AddAsync(new RecurringTaskTemplate(
            Id: 0,
            Title: command.Title!.Trim(),
            StartDate: startDate,
            Recurrence: new RecurrenceRule(every!.Value, unit),
            ReminderPolicy: reminderPolicy,
            Status: RecurringTaskStatus.Active,
            CreatedAt: now,
            UpdatedAt: now), cancellationToken);

        var firstInstance = new RecurringTaskInstance(
            Id: 0,
            RecurringTaskId: createdTemplate.Id,
            Title: createdTemplate.Title,
            DueAt: createdTemplate.StartDate.ToDateTimeOffset(clock.TimeZone),
            ReminderPolicy: createdTemplate.ReminderPolicy,
            CreatedAt: now,
            UpdatedAt: now,
            Status: RecurringTaskInstanceStatus.Active);

        await instanceStore.AddAsync(firstInstance, cancellationToken);

        return createdTemplate;
    }

    private DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.UtcNow.UtcDateTime, clock.TimeZone));

    private static bool TryParseUnit(string? value, out RecurrenceUnit unit) => value switch
    {
        "days" => Set(RecurrenceUnit.Days, out unit),
        "weeks" => Set(RecurrenceUnit.Weeks, out unit),
        "months" => Set(RecurrenceUnit.Months, out unit),
        _ => Set(default, out unit, false)
    };

    private static bool Set(RecurrenceUnit value, out RecurrenceUnit unit, bool success = true)
    {
        unit = value;
        return success;
    }
}
