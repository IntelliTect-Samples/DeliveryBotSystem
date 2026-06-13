using AgentService.DTOs;

namespace AgentService.Services;

public sealed class SupportEscalationRecord
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? RelatedOrderId { get; set; }
    public string Reason { get; set; } = "";
    public AgentChatRequestDto Request { get; set; } = new();
    public AgentChatResponseDto Response { get; set; } = new();
}
