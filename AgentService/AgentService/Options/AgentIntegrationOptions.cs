namespace AgentService.Options;

public sealed class AgentIntegrationOptions
{
    public const string SectionName = "Integrations";

    public string OrderServiceBaseUrl { get; set; } = "";
    public string SimulatorBaseUrl { get; set; } = "";
}
