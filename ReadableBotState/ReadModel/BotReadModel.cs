using System.Text.Json.Serialization;

namespace ReadableBotState.ReadModel;

public sealed class BotReadModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("botId")]
    public string BotId { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("isRemoved")]
    public bool IsRemoved { get; set; }

    [JsonPropertyName("currentLocation")]
    public GeoLocation? CurrentLocation { get; set; }

    [JsonPropertyName("powerLevel")]
    public double? PowerLevel { get; set; }

    [JsonPropertyName("externalTemperature")]
    public double? ExternalTemperature { get; set; }

    [JsonPropertyName("internalStorageTemperature")]
    public double? InternalStorageTemperature { get; set; }

    [JsonPropertyName("activeOrderId")]
    public string? ActiveOrderId { get; set; }

    [JsonPropertyName("queuedOrderCount")]
    public int QueuedOrderCount { get; set; }

    [JsonPropertyName("inventory")]
    public List<InventoryItem> Inventory { get; set; } = [];

    [JsonPropertyName("lastTelemetryEventUtc")]
    public DateTimeOffset? LastTelemetryEventUtc { get; set; }

    [JsonPropertyName("lastStatusEventUtc")]
    public DateTimeOffset? LastStatusEventUtc { get; set; }

    [JsonPropertyName("lastInventoryEventUtc")]
    public DateTimeOffset? LastInventoryEventUtc { get; set; }

    [JsonPropertyName("lastManagementEventUtc")]
    public DateTimeOffset? LastManagementEventUtc { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    [JsonPropertyName("lastProcessedEventIds")]
    public List<string> LastProcessedEventIds { get; set; } = [];
}
