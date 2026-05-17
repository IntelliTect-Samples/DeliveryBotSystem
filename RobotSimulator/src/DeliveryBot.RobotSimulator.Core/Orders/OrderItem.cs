namespace DeliveryBot.RobotSimulator.Core.Orders;

public sealed record OrderItem(
    string ItemId,
    int Quantity
);