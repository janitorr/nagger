using System.Globalization;
using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record CreateRecurringTaskCommand(string? Title, string? StartDate, RecurrenceRuleInput? Recurrence)
    : ICommand<RecurringTaskTemplate>
{
    public (string Title, DateOnly StartDate, RecurrenceRule Recurrence) Parse(DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Title))
            errors["title"] = ["Title is required."];

        var startDate = default(DateOnly);
        if (
            StartDate is null
            || !DateOnly.TryParseExact(
                StartDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out startDate
            )
        )
            errors["startDate"] = ["Start date must be in YYYY-MM-DD format."];

        var every = Recurrence?.Every;
        if (every is null || every <= 0)
            errors["recurrence.every"] = ["Recurrence every must be a positive integer."];

        var unit = default(RecurrenceUnit);
        if (!RecurrenceUnits.TryParse(Recurrence?.Unit, out unit))
            errors["recurrence.unit"] = ["Recurrence unit must be days, weeks, or months."];

        if (startDate != default && startDate < today)
            errors["startDate"] = ["Start date cannot be in the past."];

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return (Title!.Trim(), startDate, new RecurrenceRule(every!.Value, unit));
    }
}

public sealed record RecurrenceRuleInput(int? Every, string? Unit);

public sealed class CreateRecurringTaskHandler(
    IRecurringTaskTemplateStore templateStore,
    IRecurringTaskInstanceStore instanceStore,
    TimeProvider timeProvider
) : ICommandHandler<CreateRecurringTaskCommand, RecurringTaskTemplate>
{
    public async ValueTask<RecurringTaskTemplate> Handle(
        CreateRecurringTaskCommand command,
        CancellationToken cancellationToken
    )
    {
        var (title, startDate, recurrence) = command.Parse(Today());

        var now = timeProvider.GetUtcNow();
        var createdTemplate = await templateStore.AddAsync(
            new RecurringTaskTemplate(
                Id: 0,
                Title: title,
                StartDate: startDate,
                Recurrence: recurrence,
                Status: RecurringTaskStatus.Active,
                CreatedAt: now,
                UpdatedAt: now
            ),
            cancellationToken
        );

        var firstInstance = new RecurringTaskInstance(
            Id: 0,
            RecurringTaskId: createdTemplate.Id,
            Title: createdTemplate.Title,
            DueAt: createdTemplate.StartDate.ToDateTimeOffset(timeProvider.LocalTimeZone),
            CreatedAt: now,
            UpdatedAt: now,
            Status: RecurringTaskInstanceStatus.Active
        );

        await instanceStore.AddAsync(firstInstance, cancellationToken);

        return createdTemplate;
    }

    private DateOnly Today() =>
        DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(timeProvider.GetUtcNow().UtcDateTime, timeProvider.LocalTimeZone)
        );
}
