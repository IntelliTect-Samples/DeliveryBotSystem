using AgentService.DTOs;

namespace AgentService.Services;

public sealed class AgentChatTranscriptRecord
{
    public DateTimeOffset ArchivedAtUtc { get; init; }
    public string? RelatedOrderId { get; init; }
    public AgentChatRequestDto Request { get; init; } = new();
    public AgentChatResponseDto Response { get; init; } = new();
}
