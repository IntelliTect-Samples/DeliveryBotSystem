namespace AgentService.DTOs;

public sealed class AgentChatResponseDto
{
    public string Reply { get; set; } = "";
    public string Source { get; set; } = "azure-openai";
    public string? Model { get; set; }
}
