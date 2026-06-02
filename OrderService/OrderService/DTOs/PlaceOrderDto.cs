// Defines the shape of the request body when a customer places an order.
// Matches the fields in the customer web app order form.
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

    // The catalog item being ordered — mirrors the simulator's bot stock:
    // water | soda | chips | sandwich (item id or display name, case-insensitive).
    [Required]
    public string OrderType { get; set; } = "water";

    public string DeliveryNotes { get; set; } = string.Empty;
}
