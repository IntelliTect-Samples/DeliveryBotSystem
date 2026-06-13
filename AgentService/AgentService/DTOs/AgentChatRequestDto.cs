namespace AgentService.DTOs;

public sealed class AgentChatRequestDto
{
    public string Message { get; set; } = "";
    public AgentChatContextDto? Context { get; set; }
    public IReadOnlyList<AgentChatMessageDto> History { get; set; } = [];
}
