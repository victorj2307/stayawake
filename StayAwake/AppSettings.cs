using System.Text.Json.Serialization;

namespace StayAwake;

public sealed class AppSettings
{
    public bool Enabled { get; set; }
    public int MovementPixels { get; set; } = 1;
    public int IdleSeconds { get; set; } = 60;
    public bool MinimizeToTray { get; set; }

    [JsonConverter(typeof(MovementModeJsonConverter))]
    public MovementMode MovementMode { get; set; } = MovementMode.Horizontal;

    public int SessionDurationHours { get; set; }
}
