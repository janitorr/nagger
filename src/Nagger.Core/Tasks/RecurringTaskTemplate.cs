namespace Nagger.Core.Tasks;

public sealed record RecurringTaskTemplate(
    long Id,
    string Title,
    DateOnly StartDate,
    RecurrenceRule Recurrence,
    ReminderPolicy ReminderPolicy,
    RecurringTaskStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CancelledAt = null)
{
    public RecurringTaskTemplate Pause(DateTimeOffset now)
    {
        if (Status != RecurringTaskStatus.Active)
            throw new ValidationException(new Dictionary<string, string[]> { ["status"] = [$"Cannot pause a {Status.ToContractValue()} recurring task."] });

        return this with { Status = RecurringTaskStatus.Paused, UpdatedAt = now };
    }

    public RecurringTaskTemplate Resume(DateTimeOffset now)
    {
        if (Status != RecurringTaskStatus.Paused)
            throw new ValidationException(new Dictionary<string, string[]> { ["status"] = [$"Cannot resume a {Status.ToContractValue()} recurring task."] });

        return this with { Status = RecurringTaskStatus.Active, UpdatedAt = now };
    }

    public RecurringTaskTemplate Cancel(DateTimeOffset now)
    {
        if (Status == RecurringTaskStatus.Cancelled)
            throw new ValidationException(new Dictionary<string, string[]> { ["status"] = [$"Cannot cancel a {Status.ToContractValue()} recurring task."] });

        return this with
        {
            Status = RecurringTaskStatus.Cancelled,
            UpdatedAt = now,
            CancelledAt = now
        };
    }
}

public sealed record RecurrenceRule(int Every, RecurrenceUnit Unit);

public enum RecurrenceUnit
{
    Days,
    Weeks,
    Months
}

public static class RecurrenceUnits
{
    public static string ToContractValue(this RecurrenceUnit unit) => unit switch
    {
        RecurrenceUnit.Days => "days",
        RecurrenceUnit.Weeks => "weeks",
        RecurrenceUnit.Months => "months",
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };
}

public enum RecurringTaskStatus
{
    Active,
    Paused,
    Cancelled
}

public static class RecurringTaskStatuses
{
    public static string ToContractValue(this RecurringTaskStatus status) => status switch
    {
        RecurringTaskStatus.Active => "active",
        RecurringTaskStatus.Paused => "paused",
        RecurringTaskStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static RecurringTaskStatus FromContractValue(string status) => status switch
    {
        "active" => RecurringTaskStatus.Active,
        "paused" => RecurringTaskStatus.Paused,
        "cancelled" => RecurringTaskStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}

public sealed class RecurringTaskNotFoundException : Exception
{
    public RecurringTaskNotFoundException(long id)
        : base($"Recurring task template with id {id} not found.")
    {
    }
}