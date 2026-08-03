using System.Globalization;
using Mediator;

namespace Nagger.Core.Tasks;

public sealed record CreateOneShotTaskCommand(string? Title, string? DueAt, string? ReminderPolicy) : ICommand<TaskItem>;

public sealed class CreateOneShotTaskHandler(ITaskStore store, IClock clock)
    : ICommandHandler<CreateOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CreateOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(command.Title))
            errors["title"] = ["Title is required."];

        var hasOffset = command.DueAt is not null &&
            (command.DueAt.EndsWith("Z", StringComparison.OrdinalIgnoreCase) ||
             System.Text.RegularExpressions.Regex.IsMatch(command.DueAt, "[+-]\\d{2}:\\d{2}$"));
        var dueAt = default(DateTimeOffset);
        if (!hasOffset || !DateTimeOffset.TryParse(command.DueAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dueAt))
            errors["due_at"] = ["Due timestamp must be an ISO-8601 value with an explicit UTC offset."];

        if (!ReminderPolicies.TryParse(command.ReminderPolicy, out var reminderPolicy))
            errors["reminder_policy"] = ["Reminder policy must be none, once, or weekly-until-done."];

        if (errors.Count > 0)
            throw new ValidationException(errors);

        var now = clock.UtcNow;
        return await store.AddAsync(new TaskItem(0, command.Title!.Trim(), dueAt, reminderPolicy, now, now), cancellationToken);
    }
}
