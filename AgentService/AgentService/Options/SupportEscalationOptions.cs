namespace AgentService.Options;

public sealed class SupportEscalationOptions
{
    public const string SectionName = "ServiceBus";

    public bool Enabled { get; set; }
    public string FullyQualifiedNamespace { get; set; } = "";
    public string QueueName { get; set; } = "support-escalations";

    public bool IsConfigured() =>
        Enabled &&
        !string.IsNullOrWhiteSpace(FullyQualifiedNamespace) &&
        !string.IsNullOrWhiteSpace(QueueName);
}
