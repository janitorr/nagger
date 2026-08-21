namespace Nagger.Core.Tasks.Domain;

public sealed record TaskItem(
    long Id,
    string Title,
    DateTimeOffset DueAt,
    ReminderPolicy ReminderPolicy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastReminderAt = null,
    OneShotTaskStatus Status = OneShotTaskStatus.Active,
    DateTimeOffset? CompletedAt = null,
    DateTimeOffset? CancelledAt = null
)
{
    public TaskItem Complete(DateTimeOffset now)
    {
        if (Status != OneShotTaskStatus.Active)
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot complete a {Status.ToContractValue()} task."],
                }
            );

        return this with
        {
            Status = OneShotTaskStatus.Done,
            UpdatedAt = now,
            CompletedAt = now,
        };
    }

    public TaskItem Pause(DateTimeOffset now)
    {
        if (Status != OneShotTaskStatus.Active)
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot pause a {Status.ToContractValue()} task."],
                }
            );

        return this with
        {
            Status = OneShotTaskStatus.Paused,
            UpdatedAt = now,
        };
    }

    public TaskItem Resume(DateTimeOffset now)
    {
        if (Status != OneShotTaskStatus.Paused)
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot resume a {Status.ToContractValue()} task."],
                }
            );

        return this with
        {
            Status = OneShotTaskStatus.Active,
            UpdatedAt = now,
        };
    }

    public TaskItem Cancel(DateTimeOffset now)
    {
        if (Status is not (OneShotTaskStatus.Active or OneShotTaskStatus.Paused))
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot cancel a {Status.ToContractValue()} task."],
                }
            );

        return this with
        {
            Status = OneShotTaskStatus.Cancelled,
            UpdatedAt = now,
            CancelledAt = now,
        };
    }
}

public enum OneShotTaskStatus
{
    Active,
    Paused,
    Done,
    Cancelled,
}

public static class OneShotTaskStatuses
{
    public static string ToContractValue(this OneShotTaskStatus status) =>
        status switch
        {
            OneShotTaskStatus.Active => "active",
            OneShotTaskStatus.Paused => "paused",
            OneShotTaskStatus.Done => "done",
            OneShotTaskStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

    public static OneShotTaskStatus FromContractValue(string status) =>
        status switch
        {
            "active" => OneShotTaskStatus.Active,
            "paused" => OneShotTaskStatus.Paused,
            "done" => OneShotTaskStatus.Done,
            "cancelled" => OneShotTaskStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}
