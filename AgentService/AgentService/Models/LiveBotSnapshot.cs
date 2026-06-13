namespace AgentService.Models;

public sealed class LiveBotSnapshot
{
    public string? BotId { get; set; }
    public string? Status { get; set; }
    public double? PowerLevel { get; set; }
    public LiveBotLocationSnapshot? CurrentLocation { get; set; }
    public string? ActiveOrderId { get; set; }
    public int? QueuedOrderCount { get; set; }
}

public sealed class LiveBotLocationSnapshot
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
