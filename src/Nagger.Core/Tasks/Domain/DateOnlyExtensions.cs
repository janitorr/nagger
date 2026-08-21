namespace Nagger.Core.Tasks.Domain;

public static class DateOnlyExtensions
{
    public static DateTimeOffset ToDateTimeOffset(this DateOnly date, TimeZoneInfo timeZone)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }
}
