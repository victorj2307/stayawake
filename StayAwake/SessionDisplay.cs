namespace StayAwake;

internal static class SessionDisplay
{
    public static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
            return string.Empty;

        if (remaining.TotalHours >= 1)
            return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";

        return $"{remaining.Minutes}m {remaining.Seconds}s";
    }
}
