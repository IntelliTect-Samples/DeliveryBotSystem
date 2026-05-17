using System.ComponentModel.DataAnnotations;

namespace BotNetApi.Models;

public class Bot
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public StockLevel StockLevel { get; set; }

    /// <summary>Battery percentage from 0 to 100.</summary>
    public int BatteryLevel { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public DateTime LastUpdated { get; set; }

    public bool IsOnline { get; set; }

    public bool IsServicingCustomer { get; set; }
}
