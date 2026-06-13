namespace AgentService.Services;

public sealed class NoOpSupportEscalationPublisher : ISupportEscalationPublisher
{
    public Task PublishAsync(SupportEscalationRecord escalation, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
