using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Core.Stock;
using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Tests;

public sealed class DeliverySimulationTests
{
    [Fact]
    public void Tick_MovesBotTowardDestination_WhenBotIsOnDelivery()
    {
        var bot = CreateBot();

        var assignment = CreateAssignment(
            "order-001",
            "bot-001",
            new GeoLocation(33.4260, -111.9395));

        bot.AssignOrder(assignment);

        var before = bot.ToSnapshot().CurrentLocation;

        bot.Tick(
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(1),
            new SimulationOptions
            {
                DeliverySpeedMetersPerSecond = 8,
                DestinationArrivalThresholdMeters = 1,
                TelemetryIntervalSeconds = 999
            });

        var after = bot.ToSnapshot().CurrentLocation;

        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Tick_CompletesDelivery_WhenBotReachesDestination()
    {
        var bot = CreateBot();

        var destination = new GeoLocation(33.42551, -111.93999);

        var assignment = CreateAssignment(
            "order-001",
            "bot-001",
            destination);

        bot.AssignOrder(assignment);

        var result = bot.Tick(
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(10),
            new SimulationOptions
            {
                DeliverySpeedMetersPerSecond = 100,
                DestinationArrivalThresholdMeters = 5,
                TelemetryIntervalSeconds = 999
            });

        var snapshot = bot.ToSnapshot();

        Assert.Contains("order-001", result.CompletedOrderIds);
        Assert.Equal(BotStatus.Available, snapshot.Status);
        Assert.Null(snapshot.ActiveOrderId);
        Assert.Contains(result.GeneratedEvents, e =>
            e.EventType == RobotEventTypes.RobotStockUpdated);
        Assert.Contains(result.GeneratedEvents, e =>
            e.EventType == RobotEventTypes.RobotStatusUpdated);
    }

    [Fact]
    public void Tick_DeductsStock_WhenDeliveryCompletes()
    {
        var bot = CreateBot();

        var assignment = CreateAssignment(
            "order-001",
            "bot-001",
            new GeoLocation(33.42551, -111.93999));

        bot.AssignOrder(assignment);

        bot.Tick(
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(10),
            new SimulationOptions
            {
                DeliverySpeedMetersPerSecond = 100,
                DestinationArrivalThresholdMeters = 5,
                TelemetryIntervalSeconds = 999
            });

        var water = bot.ToSnapshot()
            .Stock
            .Single(item => item.ItemId == "water");

        Assert.Equal(8, water.QuantityOnHand);
        Assert.Equal(0, water.QuantityReserved);
        Assert.Equal(8, water.QuantityAvailable);
    }

    [Fact]
    public void Tick_StartsQueuedOrderWithoutBecomingAvailable_WhenQueuedOrderExists()
    {
        var bot = CreateBot();

        var firstAssignment = CreateAssignment(
            "order-001",
            "bot-001",
            new GeoLocation(33.42551, -111.93999));

        var secondAssignment = CreateAssignment(
            "order-002",
            "bot-001",
            new GeoLocation(33.42552, -111.93998));

        bot.AssignOrder(firstAssignment);
        bot.AssignOrder(secondAssignment);

        var result = bot.Tick(
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(10),
            new SimulationOptions
            {
                DeliverySpeedMetersPerSecond = 100,
                DestinationArrivalThresholdMeters = 5,
                TelemetryIntervalSeconds = 999
            });

        var snapshot = bot.ToSnapshot();

        Assert.Contains("order-001", result.CompletedOrderIds);
        Assert.Equal(BotStatus.OnDelivery, snapshot.Status);
        Assert.Equal("order-002", snapshot.ActiveOrderId);
        Assert.Equal(0, snapshot.QueuedOrderCount);

        Assert.Contains(result.GeneratedEvents, e =>
            e.EventType == RobotEventTypes.RobotStatusUpdated &&
            e.Data.ToString()!.Contains("QueuedOrderStarted"));
    }

    [Fact]
    public void Tick_GeneratesTelemetry_WhenTelemetryIntervalHasElapsed()
    {
        var bot = CreateBot();

        var result = bot.Tick(
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(1),
            new SimulationOptions
            {
                TelemetryIntervalSeconds = 1
            });

        Assert.NotNull(result.Telemetry);
        Assert.Equal("bot-001", result.Telemetry.BotId);
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
        GeoLocation destination)
    {
        return new OrderAssignment(
            orderId,
            botId,
            new[]
            {
                new OrderItem("water", 2)
            },
            destination);
    }
}