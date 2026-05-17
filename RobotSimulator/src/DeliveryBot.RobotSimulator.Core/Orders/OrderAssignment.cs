using DeliveryBot.RobotSimulator.Core.Simulation;

namespace DeliveryBot.RobotSimulator.Core.Orders;

public sealed record OrderAssignment(
    string OrderId,
    string BotId,
    IReadOnlyCollection<OrderItem> Items,
    GeoLocation Destination
);