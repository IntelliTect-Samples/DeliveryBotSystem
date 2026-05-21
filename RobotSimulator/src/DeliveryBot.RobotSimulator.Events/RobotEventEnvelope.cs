namespace DeliveryBot.RobotSimulator.Events;

public sealed record RobotEventEnvelope
{
    public required string EventId { get; init; }
    public required string EventType { get; init; }
    public required string SchemaVersion { get; init; }
    public required DateTimeOffset TimestampUtc { get; init; }
    public string? BotId { get; init; }
    public required string Source { get; init; }
    public bool IsSimulated { get; init; } = true;
    public required object Data { get; init; }

    public static RobotEventEnvelope Create(
        string eventType,
        object data,
        string? botId = null,
        string schemaVersion = "1.0")
    {
        return new RobotEventEnvelope
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = eventType,
            SchemaVersion = schemaVersion,
            TimestampUtc = DateTimeOffset.UtcNow,
            BotId = botId,
            Source = "robot-simulator",
            IsSimulated = true,
            Data = data
        };
    }
}