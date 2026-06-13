namespace AgentService.DTOs;

public sealed class AgentLatestOrderDto
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? AssignedBotId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? ItemsSummary { get; set; }
}
