using AgentService.DTOs;

namespace AgentService.Services;

public interface IAgentService
{
    Task<AgentChatResponseDto> ChatAsync(AgentChatRequestDto request, CancellationToken cancellationToken = default);
}
