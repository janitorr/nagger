namespace Nagger.Core.Tasks;

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
    DateTimeOffset? CancelledAt = null);

public enum OneShotTaskStatus
{
    Active,
    Paused,
    Done,
    Cancelled
}

public enum ReminderPolicy
{
    None,
    Once,
    WeeklyUntilDone
}

public static class ReminderPolicies
{
    public static bool TryParse(string? value, out ReminderPolicy policy) => value switch
    {
        "none" => Set(ReminderPolicy.None, out policy),
        "once" => Set(ReminderPolicy.Once, out policy),
        "weekly-until-done" => Set(ReminderPolicy.WeeklyUntilDone, out policy),
        _ => Set(default, out policy, false)
    };

    public static string ToContractValue(this ReminderPolicy policy) => policy switch
    {
        ReminderPolicy.None => "none",
        ReminderPolicy.Once => "once",
        ReminderPolicy.WeeklyUntilDone => "weekly-until-done",
        _ => throw new ArgumentOutOfRangeException(nameof(policy))
    };

    private static bool Set(ReminderPolicy value, out ReminderPolicy policy, bool success = true)
    {
        policy = value;
        return success;
    }
}

public static class OneShotTaskStatuses
{
    public static string ToContractValue(this OneShotTaskStatus status) => status switch
    {
        OneShotTaskStatus.Active => "active",
        OneShotTaskStatus.Paused => "paused",
        OneShotTaskStatus.Done => "done",
        OneShotTaskStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static OneShotTaskStatus FromContractValue(string status) => status switch
    {
        "active" => OneShotTaskStatus.Active,
        "paused" => OneShotTaskStatus.Paused,
        "done" => OneShotTaskStatus.Done,
        "cancelled" => OneShotTaskStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
