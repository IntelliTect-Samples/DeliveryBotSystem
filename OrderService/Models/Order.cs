using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Stored as "CustomerName:Phone" since there's no login system
    [Required]
    [MaxLength(100)]
    public string CustomerId { get; set; } = string.Empty;

    // Set after round-robin selection from BotNetApi — null if no bot was available
    public string? AssignedBotId { get; set; }

    // Original address typed by the customer
    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    // GPS coordinates geocoded from DeliveryAddress — sent to the robot simulator
    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = [];
}
