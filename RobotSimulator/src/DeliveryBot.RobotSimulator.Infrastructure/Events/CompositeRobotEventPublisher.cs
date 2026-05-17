using DeliveryBot.RobotSimulator.Events;
using Microsoft.Extensions.Logging;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public sealed class CompositeRobotEventPublisher : IRobotEventPublisher
{
    private readonly IReadOnlyCollection<IRobotEventPublisher> _publishers;
    private readonly ILogger<CompositeRobotEventPublisher> _logger;

    public CompositeRobotEventPublisher(
        IEnumerable<IRobotEventPublisher> publishers,
        ILogger<CompositeRobotEventPublisher> logger)
    {
        _publishers = publishers.ToList();
        _logger = logger;
    }

    public async Task PublishAsync(
        RobotEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        foreach (var publisher in _publishers)
        {
            try
            {
                await publisher.PublishAsync(envelope, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Robot event publisher failed. Publisher={PublisherType} EventType={EventType} EventId={EventId}",
                    publisher.GetType().Name,
                    envelope.EventType,
                    envelope.EventId);

                throw;
            }
        }
    }
}