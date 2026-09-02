namespace Nagger.Core.Tasks.Domain;

public static class RecurrenceCalculator
{
    public static DateOnly CalculateNextDue(DateOnly completionDate, RecurrenceRule rule)
    {
        return rule.Unit switch
        {
            RecurrenceUnit.Days => completionDate.AddDays(rule.Every),
            RecurrenceUnit.Weeks => completionDate.AddDays(rule.Every * 7),
            RecurrenceUnit.Months => completionDate.AddMonths(rule.Every),
            _ => throw new ArgumentOutOfRangeException(nameof(rule)),
        };
    }
}
