using System.Text.Json.Serialization;

namespace StayAwake;

public sealed class AppSettings
{
    /// <summary>
    /// Persisted session on/off flag. Written by <see cref="StayAwakeWorker"/> during session lifecycle;
    /// the worker is the runtime authority while a session is active.
    /// </summary>
    public bool Enabled { get; set; }
    public int MovementPixels { get; set; } = 1;
    public int IdleSeconds { get; set; } = 60;
    public bool MinimizeToTray { get; set; }

    [JsonConverter(typeof(MovementModeJsonConverter))]
    public MovementMode MovementMode { get; set; } = MovementMode.Horizontal;

    public int SessionDurationHours { get; set; }

    /// <summary>
    /// When set (e.g. 30), overrides <see cref="SessionDurationHours"/> for the default session length.
    /// Cleared when the user sets whole-hour duration via the hours field or 1h/3h presets.
    /// </summary>
    public int? SessionDurationMinutes { get; set; }
}
