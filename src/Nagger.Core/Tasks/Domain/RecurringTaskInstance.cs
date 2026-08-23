namespace Nagger.Core.Tasks.Domain;

public sealed record RecurringTaskInstance(
    long Id,
    long RecurringTaskId,
    string Title,
    DateTimeOffset DueAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    RecurringTaskInstanceStatus Status = RecurringTaskInstanceStatus.Active,
    DateTimeOffset? CompletedAt = null,
    DateTimeOffset? CancelledAt = null
)
{
    public RecurringTaskInstance Complete(DateTimeOffset now)
    {
        if (Status != RecurringTaskInstanceStatus.Active)
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot complete a {Status.ToContractValue()} recurring instance."],
                }
            );

        return this with
        {
            Status = RecurringTaskInstanceStatus.Done,
            UpdatedAt = now,
            CompletedAt = now,
        };
    }

    public RecurringTaskInstance Pause(DateTimeOffset now)
    {
        if (Status != RecurringTaskInstanceStatus.Active)
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot pause a {Status.ToContractValue()} recurring instance."],
                }
            );

        return this with
        {
            Status = RecurringTaskInstanceStatus.Paused,
            UpdatedAt = now,
        };
    }

    public RecurringTaskInstance Resume(DateTimeOffset now)
    {
        if (Status != RecurringTaskInstanceStatus.Paused)
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot resume a {Status.ToContractValue()} recurring instance."],
                }
            );

        return this with
        {
            Status = RecurringTaskInstanceStatus.Active,
            UpdatedAt = now,
        };
    }

    public RecurringTaskInstance Cancel(DateTimeOffset now)
    {
        if (Status is not (RecurringTaskInstanceStatus.Active or RecurringTaskInstanceStatus.Paused))
            throw new ValidationException(
                new Dictionary<string, string[]>
                {
                    ["status"] = [$"Cannot cancel a {Status.ToContractValue()} recurring instance."],
                }
            );

        return this with
        {
            Status = RecurringTaskInstanceStatus.Cancelled,
            UpdatedAt = now,
            CancelledAt = now,
        };
    }
}

public enum RecurringTaskInstanceStatus
{
    Active,
    Paused,
    Done,
    Cancelled,
}

public static class RecurringTaskInstanceStatuses
{
    public static string ToContractValue(this RecurringTaskInstanceStatus status) =>
        status switch
        {
            RecurringTaskInstanceStatus.Active => "active",
            RecurringTaskInstanceStatus.Paused => "paused",
            RecurringTaskInstanceStatus.Done => "done",
            RecurringTaskInstanceStatus.Cancelled => "cancelled",
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

    public static RecurringTaskInstanceStatus FromContractValue(string status) =>
        status switch
        {
            "active" => RecurringTaskInstanceStatus.Active,
            "paused" => RecurringTaskInstanceStatus.Paused,
            "done" => RecurringTaskInstanceStatus.Done,
            "cancelled" => RecurringTaskInstanceStatus.Cancelled,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}
