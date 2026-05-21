namespace DeliveryBot.RobotSimulator.Infrastructure.Configuration;

public sealed class EventTransportOptions
{
    public string Mode { get; init; } = EventTransportModes.Local;

    public string? ConnectionString { get; init; }
    public string? FullyQualifiedNamespace { get; init; }

    public string? InputEventHubName { get; init; }
    public string? OutputEventHubName { get; init; }

    public string ConsumerGroup { get; init; } = "$Default";

    public bool EnableInputConsumer { get; init; } = false;
}