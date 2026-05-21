using System.ComponentModel.DataAnnotations;

namespace BotNetApi.DTOs;

public class UpdateBotDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100, ErrorMessage = "BatteryLevel must be between 0 and 100.")]
    public int BatteryLevel { get; set; }

    public bool IsOnline { get; set; }

    public bool IsServicingCustomer { get; set; }
}
