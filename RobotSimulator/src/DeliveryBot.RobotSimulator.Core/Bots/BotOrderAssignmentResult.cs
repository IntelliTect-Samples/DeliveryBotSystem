using System.Text.Json.Serialization;
using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed record BotOrderAssignmentResult(
    string OrderId,
    string BotId,
    string Result,
    string Message)
{
    [JsonIgnore]
    public IReadOnlyCollection<RobotEventEnvelope> GeneratedEvents { get; init; } =
        Array.Empty<RobotEventEnvelope>();

    public static BotOrderAssignmentResult Accepted(
        string orderId,
        string botId,
        string message,
        IReadOnlyCollection<RobotEventEnvelope>? generatedEvents = null)
    {
        return new BotOrderAssignmentResult(orderId, botId, "Accepted", message)
        {
            GeneratedEvents = generatedEvents ?? Array.Empty<RobotEventEnvelope>()
        };
    }

    public static BotOrderAssignmentResult Queued(
        string orderId,
        string botId,
        string message,
        IReadOnlyCollection<RobotEventEnvelope>? generatedEvents = null)
    {
        return new BotOrderAssignmentResult(orderId, botId, "Queued", message)
        {
            GeneratedEvents = generatedEvents ?? Array.Empty<RobotEventEnvelope>()
        };
    }

    public static BotOrderAssignmentResult Rejected(
        string orderId,
        string botId,
        string message,
        IReadOnlyCollection<RobotEventEnvelope>? generatedEvents = null)
    {
        return new BotOrderAssignmentResult(orderId, botId, "Rejected", message)
        {
            GeneratedEvents = generatedEvents ?? Array.Empty<RobotEventEnvelope>()
        };
    }
}