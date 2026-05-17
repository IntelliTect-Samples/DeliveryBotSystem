using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public interface IRobotEventConsumer
{
    Task StartAsync(
        Func<RobotEventEnvelope, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}