namespace AgentService.Models;

public sealed class LiveOrderSnapshot
{
    public string? Id { get; set; }
    public string? CustomerId { get; set; }
    public string? AssignedBotId { get; set; }
    public string? Status { get; set; }
    public string? DeliveryAddress { get; set; }
    public List<LiveOrderItemSnapshot> Items { get; set; } = [];
}

public sealed class LiveOrderItemSnapshot
{
    public string? ItemId { get; set; }
    public int Quantity { get; set; }
}
