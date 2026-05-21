using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Core.Simulation;

namespace DeliveryBot.RobotSimulator.Tests;

public sealed class BotManagementTests
{
    [Fact]
    public void AddBot_AddsBotToFleet()
    {
        var fleet = new BotFleet();

        fleet.AddBot(
            "bot-100",
            "DeliveryBot-Test",
            new GeoLocation(33.4255, -111.9400));

        var bot = fleet.Get("bot-100");

        Assert.NotNull(bot);
        Assert.Equal("bot-100", bot.BotId);
        Assert.Equal("DeliveryBot-Test", bot.Model);
    }

    [Fact]
    public void AddBot_Throws_WhenBotAlreadyExists()
    {
        var fleet = new BotFleet();

        fleet.AddBot(
            "bot-100",
            "DeliveryBot-Test",
            new GeoLocation(33.4255, -111.9400));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            fleet.AddBot(
                "bot-100",
                "DeliveryBot-Test",
                new GeoLocation(33.4255, -111.9400)));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void UpdateBot_UpdatesEditableProperties()
    {
        var fleet = new BotFleet();

        fleet.AddBot(
            "bot-100",
            "DeliveryBot-Test",
            new GeoLocation(33.4255, -111.9400));

        var updated = fleet.UpdateBot(
            "bot-100",
            new UpdateBotRequest
            {
                Model = "DeliveryBot-Updated",
                PowerLevel = 55,
                ExternalTemperature = 80,
                InternalStorageTemperature = 40,
                CurrentLocation = new GeoLocation(33.5, -111.9)
            });

        Assert.NotNull(updated);
        Assert.Equal("DeliveryBot-Updated", updated.Model);
        Assert.Equal(55, updated.PowerLevel);
        Assert.Equal(80, updated.ExternalTemperature);
        Assert.Equal(40, updated.InternalStorageTemperature);
        Assert.Equal(33.5, updated.CurrentLocation.Latitude);
        Assert.Equal(-111.9, updated.CurrentLocation.Longitude);
    }

    [Fact]
    public void RemoveBot_RemovesBot_WhenBotHasNoActiveOrQueuedOrders()
    {
        var fleet = new BotFleet();

        fleet.AddBot(
            "bot-100",
            "DeliveryBot-Test",
            new GeoLocation(33.4255, -111.9400));

        var removed = fleet.RemoveBot(
            "bot-100",
            out var removedBot,
            out var reason);

        Assert.True(removed);
        Assert.NotNull(removedBot);
        Assert.Null(reason);
        Assert.Null(fleet.Get("bot-100"));
    }

    [Fact]
    public void RemoveBot_ReturnsFalse_WhenBotDoesNotExist()
    {
        var fleet = new BotFleet();

        var removed = fleet.RemoveBot(
            "bot-404",
            out var removedBot,
            out var reason);

        Assert.False(removed);
        Assert.Null(removedBot);
        Assert.Equal("BotNotFound", reason);
    }

    [Fact]
    public void RemoveBot_ReturnsFalse_WhenBotHasActiveOrder()
    {
        var fleet = new BotFleet();

        fleet.AddBot(
            "bot-100",
            "DeliveryBot-Test",
            new GeoLocation(33.4255, -111.9400));

        Assert.True(fleet.TryGetBot("bot-100", out var bot));

        bot.AssignOrder(new OrderAssignment(
            "order-001",
            "bot-100",
            new[]
            {
                new OrderItem("water", 1)
            },
            new GeoLocation(33.426, -111.9395)));

        var removed = fleet.RemoveBot(
            "bot-100",
            out var removedBot,
            out var reason);

        Assert.False(removed);
        Assert.Null(removedBot);
        Assert.Equal("BotHasActiveOrQueuedOrders", reason);
    }

    [Fact]
    public void InitializeDefaultFleet_CreatesConfiguredBotCount()
    {
        var fleet = new BotFleet();

        fleet.InitializeDefaultFleet(new SimulatorOptions
        {
            InitialBotCount = 5,
            BotIdPrefix = "testbot",
            DefaultBotModel = "DeliveryBot-Test",
            DefaultLatitude = 33.1,
            DefaultLongitude = -111.1
        });

        var bots = fleet.GetAll();

        Assert.Equal(5, bots.Count);
        Assert.Contains(bots, bot => bot.BotId == "testbot-001");
        Assert.Contains(bots, bot => bot.BotId == "testbot-005");
        Assert.All(bots, bot =>
        {
            Assert.Equal("DeliveryBot-Test", bot.Model);
            Assert.Equal(33.1, bot.CurrentLocation.Latitude);
            Assert.Equal(-111.1, bot.CurrentLocation.Longitude);
        });
    }
}