using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string CustomerId { get; set; } = string.Empty;

    public string? AssignedBotId { get; set; }

    [MaxLength(500)]
    public string DeliveryAddress { get; set; } = string.Empty;

    public double DestinationLatitude { get; set; }
    public double DestinationLongitude { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = [];
}
