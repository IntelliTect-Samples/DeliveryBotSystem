using System.Text.Json.Serialization;

namespace ReadableBotState.ReadModel;

public sealed record InventoryItem
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; init; } = string.Empty;

    [JsonPropertyName("itemName")]
    public string ItemName { get; init; } = string.Empty;

    [JsonPropertyName("quantityOnHand")]
    public int QuantityOnHand { get; init; }

    [JsonPropertyName("quantityReserved")]
    public int QuantityReserved { get; init; }

    [JsonPropertyName("quantityAvailable")]
    public int QuantityAvailable { get; init; }
}
