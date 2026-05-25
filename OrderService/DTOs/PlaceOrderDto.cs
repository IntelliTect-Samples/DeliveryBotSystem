using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs;

public class PlaceOrderDto
{
    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    public string Phone { get; set; } = string.Empty;

    public string RestaurantOrStore { get; set; } = string.Empty;

    [Required]
    public string DeliveryAddress { get; set; } = string.Empty;

    [Required]
    public string OrderType { get; set; } = "Food Order";

    public string DeliveryNotes { get; set; } = string.Empty;
}
