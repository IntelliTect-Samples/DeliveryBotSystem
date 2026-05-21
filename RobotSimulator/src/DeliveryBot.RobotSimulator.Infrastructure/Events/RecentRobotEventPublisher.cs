using DeliveryBot.RobotSimulator.Events;
using Microsoft.Extensions.Logging;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public sealed class RecentRobotEventPublisher : IRobotEventPublisher
{
    private readonly RecentRobotEventStore _eventStore;
    private readonly ILogger<RecentRobotEventPublisher> _logger;

    public RecentRobotEventPublisher(
        RecentRobotEventStore eventStore,
        ILogger<RecentRobotEventPublisher> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    public Task PublishAsync(
        RobotEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        _eventStore.Add(envelope);

        _logger.LogDebug(
            "Stored recent robot event. EventType={EventType} EventId={EventId} BotId={BotId}",
            envelope.EventType,
            envelope.EventId,
            envelope.BotId ?? "none");

        return Task.CompletedTask;
    }
}