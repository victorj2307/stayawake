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
}
