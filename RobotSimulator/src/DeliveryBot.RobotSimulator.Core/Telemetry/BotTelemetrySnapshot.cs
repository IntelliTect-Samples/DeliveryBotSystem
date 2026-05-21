using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Simulation;

namespace DeliveryBot.RobotSimulator.Core.Telemetry;

public sealed record BotTelemetrySnapshot(
    string BotId,
    DateTimeOffset TimestampUtc,
    BotStatus Status,
    GeoLocation CurrentLocation,
    double PowerLevel,
    double ExternalTemperature,
    double InternalStorageTemperature,
    string? ActiveOrderId,
    int QueuedOrderCount
);