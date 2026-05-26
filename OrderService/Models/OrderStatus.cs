// Defines the possible states an order can be in.
// Status moves forward as the bot picks up and delivers the order.
namespace OrderService.Models;

public enum OrderStatus
{
    Pending,
    Assigned,
    InTransit,
    Delivered,
    Cancelled,
    Failed
}
