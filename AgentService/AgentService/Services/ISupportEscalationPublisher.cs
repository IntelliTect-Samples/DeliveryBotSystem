namespace AgentService.Services;

public interface ISupportEscalationPublisher
{
    Task PublishAsync(SupportEscalationRecord escalation, CancellationToken cancellationToken = default);
}
