using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Core.Stock;
using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Tests;

public sealed class OrderAssignmentTests
{
    [Fact]
    public void AssignOrder_AcceptsOrder_WhenBotIsAvailableAndStockExists()
    {
        var bot = CreateBot();

        var assignment = CreateAssignment("order-001", "bot-001", "water", 2);

        var result = bot.AssignOrder(assignment);
        var snapshot = bot.ToSnapshot();

        Assert.Equal("Accepted", result.Result);
        Assert.Equal("order-001", result.OrderId);
        Assert.Equal(BotStatus.OnDelivery, snapshot.Status);
        Assert.Equal("order-001", snapshot.ActiveOrderId);
        Assert.Contains(result.GeneratedEvents, e =>
            e.EventType == RobotEventTypes.RobotStockUpdated);
        Assert.Contains(result.GeneratedEvents, e =>
            e.EventType == RobotEventTypes.RobotStatusUpdated);
    }

    [Fact]
    public void AssignOrder_QueuesOrder_WhenBotIsAlreadyDelivering()
    {
        var bot = CreateBot();

        var firstAssignment = CreateAssignment("order-001", "bot-001", "water", 1);
        var secondAssignment = CreateAssignment("order-002", "bot-001", "soda", 1);

        var firstResult = bot.AssignOrder(firstAssignment);
        var secondResult = bot.AssignOrder(secondAssignment);

        var snapshot = bot.ToSnapshot();

        Assert.Equal("Accepted", firstResult.Result);
        Assert.Equal("Queued", secondResult.Result);
        Assert.Equal(BotStatus.OnDelivery, snapshot.Status);
        Assert.Equal("order-001", snapshot.ActiveOrderId);
        Assert.Equal(1, snapshot.QueuedOrderCount);
        Assert.Contains(secondResult.GeneratedEvents, e =>
            e.EventType == RobotEventTypes.RobotStockUpdated);
        Assert.DoesNotContain(secondResult.GeneratedEvents, e =>
            e.EventType == RobotEventTypes.RobotStatusUpdated);
    }

    [Fact]
    public void AssignOrder_RejectsOrder_WhenItemIsNotStocked()
    {
        var bot = CreateBot();

        var assignment = CreateAssignment("order-001", "bot-001", "coffee", 1);

        var result = bot.AssignOrder(assignment);
        var snapshot = bot.ToSnapshot();

        Assert.Equal("Rejected", result.Result);
        Assert.Contains("not stocked", result.Message);
        Assert.Equal(BotStatus.Available, snapshot.Status);
        Assert.Null(snapshot.ActiveOrderId);
        Assert.Empty(result.GeneratedEvents);
    }

    [Fact]
    public void AssignOrder_RejectsOrder_WhenStockIsInsufficient()
    {
        var bot = CreateBot();

        var assignment = CreateAssignment("order-001", "bot-001", "water", 999);

        var result = bot.AssignOrder(assignment);
        var snapshot = bot.ToSnapshot();

        Assert.Equal("Rejected", result.Result);
        Assert.Contains("Insufficient", result.Message);
        Assert.Equal(BotStatus.Available, snapshot.Status);
        Assert.Null(snapshot.ActiveOrderId);
        Assert.Empty(result.GeneratedEvents);
    }

    [Fact]
    public void AssignOrder_RejectsOrder_WhenAssignedToDifferentBot()
    {
        var bot = CreateBot();

        var assignment = CreateAssignment("order-001", "bot-999", "water", 1);

        var result = bot.AssignOrder(assignment);

        Assert.Equal("Rejected", result.Result);
        Assert.Contains("different bot", result.Message);
        Assert.Empty(result.GeneratedEvents);
    }

    private static SimulatedBot CreateBot()
    {
        return new SimulatedBot(
            "bot-001",
            "DeliveryBot-Test",
            new GeoLocation(33.4255, -111.9400),
            new[]
            {
                new BotStockItem("water", "Water", 10),
                new BotStockItem("soda", "Soda", 10)
            });
    }

    private static OrderAssignment CreateAssignment(
        string orderId,
        string botId,
        string itemId,
        int quantity)
    {
        return new OrderAssignment(
            orderId,
            botId,
            new[]
            {
                new OrderItem(itemId, quantity)
            },
            new GeoLocation(33.4260, -111.9395));
    }
}