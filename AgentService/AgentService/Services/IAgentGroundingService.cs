using AgentService.DTOs;

namespace AgentService.Services;

public interface IAgentGroundingService
{
    Task EnrichAsync(AgentChatRequestDto request, CancellationToken cancellationToken = default);
}
