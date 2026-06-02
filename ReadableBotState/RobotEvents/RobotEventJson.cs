using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReadableBotState.RobotEvents;

public static class RobotEventJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
