using System.Text.Json.Serialization;
using System.Globalization;
using System.Text.Json;
using ReadableBotState.ReadModel;

namespace ReadableBotState.RobotEvents;

public sealed record BotSnapshotPayload
{
    [JsonPropertyName("bot")]
    public BotSnapshot? Bot { get; init; }
}

public sealed record BotSnapshot
{
    [JsonPropertyName("botId")]
    public string? BotId { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(BotStatusJsonConverter))]
    public string? Status { get; init; }

    [JsonPropertyName("currentLocation")]
    public GeoLocation? CurrentLocation { get; init; }

    [JsonPropertyName("powerLevel")]
    public double? PowerLevel { get; init; }

    [JsonPropertyName("externalTemperature")]
    public double? ExternalTemperature { get; init; }

    [JsonPropertyName("internalStorageTemperature")]
    public double? InternalStorageTemperature { get; init; }

    [JsonPropertyName("stock")]
    public IReadOnlyCollection<InventoryItem>? Stock { get; init; }

    [JsonPropertyName("activeOrderId")]
    public string? ActiveOrderId { get; init; }

    [JsonPropertyName("queuedOrderCount")]
    public int? QueuedOrderCount { get; init; }
}

public sealed record RobotTelemetryUpdatedPayload
{
    [JsonPropertyName("botId")]
    public string? BotId { get; init; }

    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset? TimestampUtc { get; init; }

    [JsonPropertyName("status")]
    [JsonConverter(typeof(BotStatusJsonConverter))]
    public string? Status { get; init; }

    [JsonPropertyName("currentLocation")]
    public GeoLocation? CurrentLocation { get; init; }

    [JsonPropertyName("powerLevel")]
    public double? PowerLevel { get; init; }

    [JsonPropertyName("externalTemperature")]
    public double? ExternalTemperature { get; init; }

    [JsonPropertyName("internalStorageTemperature")]
    public double? InternalStorageTemperature { get; init; }

    [JsonPropertyName("activeOrderId")]
    public string? ActiveOrderId { get; init; }

    [JsonPropertyName("queuedOrderCount")]
    public int? QueuedOrderCount { get; init; }
}

public sealed record RobotStatusUpdatedPayload
{
    [JsonPropertyName("botId")]
    public string? BotId { get; init; }

    [JsonPropertyName("previousStatus")]
    [JsonConverter(typeof(BotStatusJsonConverter))]
    public string? PreviousStatus { get; init; }

    [JsonPropertyName("currentStatus")]
    [JsonConverter(typeof(BotStatusJsonConverter))]
    public string? CurrentStatus { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("activeOrderId")]
    public string? ActiveOrderId { get; init; }

    [JsonPropertyName("previousOrderId")]
    public string? PreviousOrderId { get; init; }

    [JsonPropertyName("queuedOrderCount")]
    public int? QueuedOrderCount { get; init; }

    [JsonPropertyName("currentLocation")]
    public GeoLocation? CurrentLocation { get; init; }
}

public sealed record RobotStockUpdatedPayload
{
    [JsonPropertyName("botId")]
    public string? BotId { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("relatedOrderId")]
    public string? RelatedOrderId { get; init; }

    [JsonPropertyName("stock")]
    public IReadOnlyCollection<InventoryItem>? Stock { get; init; }
}

internal sealed class BotStatusJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number when reader.TryGetInt32(out var value) => value switch
            {
                0 => "Available",
                1 => "OnDelivery",
                2 => "Offline",
                3 => "Maintenance",
                _ => value.ToString(CultureInfo.InvariantCulture)
            },
            _ => throw new JsonException("Bot status must be a string or number.")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }
}
