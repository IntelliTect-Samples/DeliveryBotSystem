using DeliveryBot.RobotSimulator.Events;
using Microsoft.Extensions.Logging;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public sealed class NoOpRobotEventConsumer : IRobotEventConsumer
{
    private readonly ILogger<NoOpRobotEventConsumer> _logger;

    public NoOpRobotEventConsumer(ILogger<NoOpRobotEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(
        Func<RobotEventEnvelope, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Robot event consumer is disabled for local/no-op mode.");
        return Task.CompletedTask;
    }
}