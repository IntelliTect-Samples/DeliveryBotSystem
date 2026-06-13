namespace AgentService.DTOs;

public sealed class AgentChatContextDto
{
    public AgentLatestOrderDto? LatestOrder { get; set; }
    public AgentRouteDto? Route { get; set; }
    public string? LiveDataSummary { get; set; }
}
