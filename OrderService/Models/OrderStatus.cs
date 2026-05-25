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
