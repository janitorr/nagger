namespace Nagger.Core.Tasks.Domain;

public static class RecurrenceCalculator
{
    public static DateOnly CalculateNextDue(DateOnly completionDate, RecurrenceRule rule)
    {
        return rule.Unit switch
        {
            RecurrenceUnit.Days => completionDate.AddDays(rule.Every),
            RecurrenceUnit.Weeks => completionDate.AddDays(rule.Every * 7),
            RecurrenceUnit.Months => AddMonthsWithEdgeCaseHandling(completionDate, rule.Every),
            _ => throw new ArgumentOutOfRangeException(nameof(rule)),
        };
    }

    private static DateOnly AddMonthsWithEdgeCaseHandling(DateOnly date, int monthsToAdd)
    {
        var year = date.Year;
        var month = date.Month + monthsToAdd;

        // Handle year rollover
        while (month > 12)
        {
            month -= 12;
            year++;
        }

        var day = date.Day;
        var daysInMonth = DateTime.DaysInMonth(year, month);

        // Handle edge case where day doesn't exist in target month
        if (day > daysInMonth)
        {
            day = daysInMonth;
        }

        return new DateOnly(year, month, day);
    }
}
