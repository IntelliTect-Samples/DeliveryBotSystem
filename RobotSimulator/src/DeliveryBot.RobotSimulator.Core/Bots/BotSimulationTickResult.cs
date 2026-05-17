using DeliveryBot.RobotSimulator.Core.Telemetry;
using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed record BotSimulationTickResult(
    BotTelemetrySnapshot? Telemetry,
    IReadOnlyCollection<string> CompletedOrderIds,
    IReadOnlyCollection<RobotEventEnvelope> GeneratedEvents
)
{
    public static BotSimulationTickResult Empty { get; } =
        new(null, Array.Empty<string>(), Array.Empty<RobotEventEnvelope>());
}