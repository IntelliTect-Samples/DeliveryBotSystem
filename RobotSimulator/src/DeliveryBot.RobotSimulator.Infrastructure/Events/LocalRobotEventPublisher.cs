using DeliveryBot.RobotSimulator.Events;
using Microsoft.Extensions.Logging;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public sealed class LocalRobotEventPublisher : IRobotEventPublisher
{
    private readonly RecentRobotEventStore _eventStore;
    private readonly ILogger<LocalRobotEventPublisher> _logger;

    public LocalRobotEventPublisher(
        RecentRobotEventStore eventStore,
        ILogger<LocalRobotEventPublisher> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    public Task PublishAsync(
        RobotEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        _eventStore.Add(envelope);

        _logger.LogInformation(
            "Published local robot event. EventType={EventType} EventId={EventId} BotId={BotId}",
            envelope.EventType,
            envelope.EventId,
            envelope.BotId ?? "none");

        return Task.CompletedTask;
    }
}