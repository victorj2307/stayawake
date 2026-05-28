namespace StayAwake;

/// <summary>Valid ranges for numeric settings (UI, persistence, and runtime).</summary>
public static class SettingLimits
{
    public const int IdleSecondsMin = 10;
    public const int IdleSecondsMax = 3600;

    public const int MovementPixelsMin = 1;
    public const int MovementPixelsMax = 10;

    public const int SessionDurationHoursMin = 0;
    public const int SessionDurationHoursMax = 99;

    public const int IdleSecondsStep = 10;
    public const int MovementPixelsStep = 1;
    public const int SessionDurationHoursStep = 1;

    public const string IdleSecondsRangeHint = "10–3600 seconds.";
    public const string MovementPixelsRangeHint = "1–10 pixels.";
    public const string SessionDurationHoursRangeHint = "0–99 hours, 0 = unlimited.";

    public static int ClampIdleSeconds(int value) =>
        Math.Clamp(value, IdleSecondsMin, IdleSecondsMax);

    public static int ClampMovementPixels(int value) =>
        Math.Clamp(value, MovementPixelsMin, MovementPixelsMax);

    public static int ClampSessionDurationHours(int value) =>
        Math.Clamp(value, SessionDurationHoursMin, SessionDurationHoursMax);

    /// <summary>Clamps numeric fields in place. Returns true if any value changed.</summary>
    public static bool Normalize(AppSettings settings)
    {
        var idle = ClampIdleSeconds(settings.IdleSeconds);
        var movement = ClampMovementPixels(settings.MovementPixels);
        var hours = ClampSessionDurationHours(settings.SessionDurationHours);

        var changed = idle != settings.IdleSeconds
            || movement != settings.MovementPixels
            || hours != settings.SessionDurationHours;

        settings.IdleSeconds = idle;
        settings.MovementPixels = movement;
        settings.SessionDurationHours = hours;

        return changed;
    }
}
