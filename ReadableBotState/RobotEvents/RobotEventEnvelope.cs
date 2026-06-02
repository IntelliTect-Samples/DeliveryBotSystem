using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReadableBotState.RobotEvents;

public sealed record RobotEventEnvelope
{
    [JsonPropertyName("eventId")]
    public string? EventId { get; init; }

    [JsonPropertyName("eventType")]
    public string? EventType { get; init; }

    [JsonPropertyName("schemaVersion")]
    public string? SchemaVersion { get; init; }

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset? TimestampUtc { get; init; }

    [JsonPropertyName("botId")]
    public string? BotId { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("isSimulated")]
    public bool IsSimulated { get; init; }

    [JsonPropertyName("data")]
    public JsonElement Data { get; init; }

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }

    [JsonIgnore]
    public JsonElement EffectiveData =>
        Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? Payload
            : Data;
}
