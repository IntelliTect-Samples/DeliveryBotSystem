using AgentService.DTOs;

namespace AgentService.Services;

public sealed class NoOpAgentGroundingService : IAgentGroundingService
{
    public Task EnrichAsync(AgentChatRequestDto request, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
