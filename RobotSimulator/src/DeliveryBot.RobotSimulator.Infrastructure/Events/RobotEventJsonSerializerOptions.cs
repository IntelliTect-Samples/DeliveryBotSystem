using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public static class RobotEventJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}