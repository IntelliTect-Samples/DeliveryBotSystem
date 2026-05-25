using System.ComponentModel.DataAnnotations;

namespace OrderService.Models;

public class OrderItem
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
