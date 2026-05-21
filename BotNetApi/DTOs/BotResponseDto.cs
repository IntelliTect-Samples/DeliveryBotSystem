namespace BotNetApi.DTOs;

public class BotResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BatteryLevel { get; set; }
    public DateTime LastUpdated { get; set; }
    public bool IsOnline { get; set; }
    public bool IsServicingCustomer { get; set; }
}
