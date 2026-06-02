using System.Text.Json.Serialization;

namespace ReadableBotState.ReadModel;

public sealed record GeoLocation
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }
}
