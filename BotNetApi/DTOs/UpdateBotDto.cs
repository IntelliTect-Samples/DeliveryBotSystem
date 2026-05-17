using System.ComponentModel.DataAnnotations;
using BotNetApi.Models;

namespace BotNetApi.DTOs;

public class UpdateBotDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public StockLevel StockLevel { get; set; }

    [Range(0, 100, ErrorMessage = "BatteryLevel must be between 0 and 100.")]
    public int BatteryLevel { get; set; }

    [Range(-90.0, 90.0, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Range(-180.0, 180.0, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    public bool IsOnline { get; set; }

    public bool IsServicingCustomer { get; set; }
}
