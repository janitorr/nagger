using System.Globalization;
using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record CreateOneShotTaskCommand(string? Title, string? DueAt, string? ReminderPolicy)
    : ICommand<TaskItem>
{
    public (string Title, DateTimeOffset DueAt, ReminderPolicy ReminderPolicy) Parse()
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(Title))
            errors["title"] = ["Title is required."];

        var dueAt = default(DateTimeOffset);
        if (
            DueAt is null
            || !DateTimeOffset.TryParseExact(
                DueAt,
                [
                    "yyyy-MM-dd'T'HH:mm:sszzz",
                    "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz",
                    "yyyy-MM-dd'T'HH:mm:ss'Z'",
                    "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'",
                ],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out dueAt
            )
        )
            errors["dueAt"] = ["Due timestamp must be an ISO-8601 value with an explicit UTC offset."];

        if (!ReminderPolicies.TryParse(ReminderPolicy, out var reminderPolicy))
            errors["reminderPolicy"] = ["Reminder policy must be none, once, or weekly-until-done."];

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return (Title!.Trim(), dueAt, reminderPolicy);
    }
}

public sealed class CreateOneShotTaskHandler(ITaskStore store, IClock clock)
    : ICommandHandler<CreateOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CreateOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var (title, dueAt, reminderPolicy) = command.Parse();

        var now = clock.UtcNow;
        return await store.AddAsync(
            new TaskItem(0, title, dueAt, reminderPolicy, now, now),
            cancellationToken
        );
    }
}
