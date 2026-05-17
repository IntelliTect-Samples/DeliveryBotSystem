using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Core.Stock;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed record BotSnapshot(
    string BotId,
    string Model,
    BotStatus Status,
    GeoLocation CurrentLocation,
    double PowerLevel,
    double ExternalTemperature,
    double InternalStorageTemperature,
    IReadOnlyCollection<BotStockItem> Stock,
    string? ActiveOrderId,
    int QueuedOrderCount
);