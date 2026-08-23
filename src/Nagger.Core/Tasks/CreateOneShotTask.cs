using System.Globalization;
using Mediator;
using Nagger.Core.Tasks.Domain;

namespace Nagger.Core.Tasks;

public sealed record CreateOneShotTaskCommand(string? Title, string? DueAt) : ICommand<TaskItem>
{
    public (string Title, DateTimeOffset DueAt) Parse(DateTimeOffset now)
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

        if (dueAt != default && dueAt < now)
            errors["dueAt"] = ["Due timestamp cannot be in the past."];

        if (errors.Count > 0)
            throw new ValidationException(errors);

        return (Title!.Trim(), dueAt);
    }
}

public sealed class CreateOneShotTaskHandler(ITaskStore store, TimeProvider timeProvider)
    : ICommandHandler<CreateOneShotTaskCommand, TaskItem>
{
    public async ValueTask<TaskItem> Handle(CreateOneShotTaskCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var (title, dueAt) = command.Parse(now);
        return await store.AddAsync(new TaskItem(0, title, dueAt, now, now), cancellationToken);
    }
}
