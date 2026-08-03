namespace Nagger.Core.Tasks;

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
