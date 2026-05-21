namespace DeliveryBot.RobotSimulator.Core.Stock;

public sealed class BotStockItem
{
    public string ItemId { get; }
    public string ItemName { get; }
    public int QuantityOnHand { get; private set; }
    public int QuantityReserved { get; private set; }

    public int QuantityAvailable => QuantityOnHand - QuantityReserved;

    public BotStockItem(string itemId, string itemName, int quantityOnHand)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            throw new ArgumentException("Item ID is required.", nameof(itemId));
        }

        if (string.IsNullOrWhiteSpace(itemName))
        {
            throw new ArgumentException("Item name is required.", nameof(itemName));
        }

        if (quantityOnHand < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityOnHand), "Quantity on hand cannot be negative.");
        }

        ItemId = itemId;
        ItemName = itemName;
        QuantityOnHand = quantityOnHand;
        QuantityReserved = 0;
    }

    public bool CanReserve(int quantity)
    {
        return quantity > 0 && QuantityAvailable >= quantity;
    }

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
        {
            throw new InvalidOperationException(
                $"Cannot reserve {quantity} units of {ItemId}. Available quantity is {QuantityAvailable}.");
        }

        QuantityReserved += quantity;
    }

    public void FulfillReserved(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity));
        }

        if (QuantityReserved < quantity)
        {
            throw new InvalidOperationException(
                $"Cannot fulfill {quantity} units of {ItemId}. Reserved quantity is {QuantityReserved}.");
        }

        QuantityReserved -= quantity;
        QuantityOnHand -= quantity;
    }
}