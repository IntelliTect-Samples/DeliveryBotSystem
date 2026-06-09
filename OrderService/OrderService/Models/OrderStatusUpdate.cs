// An AI-generated, customer-friendly message describing an order status change (#43).
// One row is written by the DeliveryConciergeService each time an order advances
// (e.g. Assigned -> InTransit) in response to a bot event from the simulator.
using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class OrderStatusUpdate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // The order this update belongs to.
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    // The status the order moved TO when this update was generated.
    public OrderStatus Status { get; set; }

    // Friendly message produced by Azure AI Foundry (gpt-4o-mini).
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
