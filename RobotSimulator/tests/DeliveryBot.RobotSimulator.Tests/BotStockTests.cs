using DeliveryBot.RobotSimulator.Core.Stock;

namespace DeliveryBot.RobotSimulator.Tests;

public sealed class BotStockTests
{
    [Fact]
    public void CanReserve_ReturnsTrue_WhenEnoughStockIsAvailable()
    {
        var stockItem = new BotStockItem("water", "Water", 10);

        var canReserve = stockItem.CanReserve(3);

        Assert.True(canReserve);
    }

    [Fact]
    public void Reserve_IncreasesReservedQuantity_AndDecreasesAvailableQuantity()
    {
        var stockItem = new BotStockItem("water", "Water", 10);

        stockItem.Reserve(3);

        Assert.Equal(10, stockItem.QuantityOnHand);
        Assert.Equal(3, stockItem.QuantityReserved);
        Assert.Equal(7, stockItem.QuantityAvailable);
    }

    [Fact]
    public void Reserve_Throws_WhenQuantityExceedsAvailableStock()
    {
        var stockItem = new BotStockItem("water", "Water", 2);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            stockItem.Reserve(3));

        Assert.Contains("Cannot reserve", exception.Message);
    }

    [Fact]
    public void FulfillReserved_DecreasesReservedQuantity_AndQuantityOnHand()
    {
        var stockItem = new BotStockItem("water", "Water", 10);

        stockItem.Reserve(3);
        stockItem.FulfillReserved(2);

        Assert.Equal(8, stockItem.QuantityOnHand);
        Assert.Equal(1, stockItem.QuantityReserved);
        Assert.Equal(7, stockItem.QuantityAvailable);
    }

    [Fact]
    public void FulfillReserved_Throws_WhenQuantityExceedsReservedQuantity()
    {
        var stockItem = new BotStockItem("water", "Water", 10);

        stockItem.Reserve(2);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            stockItem.FulfillReserved(3));

        Assert.Contains("Cannot fulfill", exception.Message);
    }
}