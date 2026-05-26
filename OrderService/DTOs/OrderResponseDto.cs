// Defines the shape of the response sent back to the customer after placing an order.
// Includes the order ID, assigned bot, status, and geocoded destination coordinates.
namespace OrderService.DTOs;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string? AssignedBotId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public GpsLocationDto? Destination { get; set; }
    public List<OrderItemDto> Items { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class OrderItemDto
{
    public string ItemId { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class GpsLocationDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
