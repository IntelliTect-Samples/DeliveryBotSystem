using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public interface IRobotEventPublisher
{
    Task PublishAsync(RobotEventEnvelope envelope, CancellationToken cancellationToken = default);
}