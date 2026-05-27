using System.Text.Json;
using System.Text.Json.Serialization;

namespace StayAwake;

/// <summary>
/// Reads movement mode strings from settings.json with fallback to Horizontal for unknown values.
/// Writes PascalCase enum names to match existing settings files.
/// </summary>
public sealed class MovementModeJsonConverter : JsonConverter<MovementMode>
{
    public override MovementMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return MovementMode.Horizontal;

        var value = reader.GetString();
        return Enum.TryParse(value, ignoreCase: true, out MovementMode mode)
            ? mode
            : MovementMode.Horizontal;
    }

    public override void Write(Utf8JsonWriter writer, MovementMode value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
