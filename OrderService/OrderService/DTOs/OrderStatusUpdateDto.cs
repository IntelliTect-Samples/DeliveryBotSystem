// Read model for an AI-generated order status update (#43).
namespace OrderService.DTOs;

public class OrderStatusUpdateDto
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
