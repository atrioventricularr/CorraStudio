namespace CorraStudio.Infrastructure.Helpers;

public static class DateTimeHelper
{
    public static DateTime GetCurrentJakartaTime()
    {
        var jakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jakartaTimeZone);
    }

    public static DateTime ToJakartaTime(this DateTime utcDateTime)
    {
        var jakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, jakartaTimeZone);
    }

    public static DateTime ToUtcFromJakarta(this DateTime jakartaDateTime)
    {
        var jakartaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeToUtc(jakartaDateTime, jakartaTimeZone);
    }

    public static string GetReadableDateDifference(DateTime fromDate, DateTime toDate)
    {
        var diff = toDate - fromDate;
        
        if (diff.TotalSeconds < 60)
            return $"{diff.Seconds} seconds";
        if (diff.TotalMinutes < 60)
            return $"{diff.Minutes} minutes";
        if (diff.TotalHours < 24)
            return $"{diff.Hours} hours";
        if (diff.TotalDays < 7)
            return $"{diff.Days} days";
        if (diff.TotalDays < 30)
            return $"{diff.Days / 7} weeks";
        if (diff.TotalDays < 365)
            return $"{diff.Days / 30} months";
        
        return $"{diff.Days / 365} years";
    }
}
