namespace MvcApp.Infrastructure.Extentions;

public static class DateTimeExtensions
{
    public static int CalculateAge(this DateTime dob)
    {
        var today = DateTime.Today;
        var age = today.Year - dob.Year;

        if (dob.Date > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }

    public static string TimeAgo(this DateTime dateTime)
    {
        var ts = DateTime.UtcNow - dateTime.ToUniversalTime();

        if (ts <= TimeSpan.Zero || ts.TotalMinutes < 1)
            return "Just now";

        if (ts.TotalMinutes < 60)
            return ts.TotalMinutes < 2 ? "1 minute ago" : $"{(int)ts.TotalMinutes} minutes ago";

        if (ts.TotalHours < 24)
            return ts.TotalHours < 2 ? "1 hour ago" : $"{(int)ts.TotalHours} hours ago";

        if (ts.TotalDays < 30)
            return ts.TotalDays < 2 ? "1 day ago" : $"{(int)ts.TotalDays} days ago";

        return dateTime.ToUniversalTime().ToString("MMM d, yyyy");
    }

    public static bool IsOnline(this DateTime dateTime, int withinMinutes = 5)
    {
        return DateTime.UtcNow - dateTime.ToUniversalTime() <= TimeSpan.FromMinutes(withinMinutes);
    }
}
