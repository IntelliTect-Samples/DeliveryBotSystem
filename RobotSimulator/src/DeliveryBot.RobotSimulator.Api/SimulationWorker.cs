using DeliveryBot.RobotSimulator.Core.Bots;
using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Events;

namespace DeliveryBot.RobotSimulator.Api;

public sealed class SimulationWorker : BackgroundService
{
    private readonly BotFleet _botFleet;
    private readonly SimulationOptions _options;
    private readonly IRobotEventPublisher _eventPublisher;
    private readonly ILogger<SimulationWorker> _logger;

    public SimulationWorker(
        BotFleet botFleet,
        SimulationOptions options,
        IRobotEventPublisher eventPublisher,
        ILogger<SimulationWorker> logger)
    {
        _botFleet = botFleet;
        _options = options;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var previousTick = DateTimeOffset.UtcNow;

        _logger.LogInformation(
            "Robot simulation worker started. Tick interval: {TickIntervalSeconds}s. Telemetry interval: {TelemetryIntervalSeconds}s.",
            _options.TickIntervalSeconds,
            _options.TelemetryIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var elapsed = now - previousTick;
            previousTick = now;

            var results = _botFleet.TickAll(now, elapsed, _options);

            foreach (var result in results)
            {
                foreach (var completedOrderId in result.Result.CompletedOrderIds)
                {
                    _logger.LogInformation(
                        "Bot {BotId} completed delivery for order {OrderId}.",
                        result.BotId,
                        completedOrderId);

                    var completionEvent = RobotEventEnvelope.Create(
                        RobotEventTypes.RobotDeliveryCompleted,
                        new
                        {
                            orderId = completedOrderId,
                            botId = result.BotId,
                            completedAtUtc = now
                        },
                        result.BotId);

                    await _eventPublisher.PublishAsync(
                        completionEvent,
                        stoppingToken);
                }

                foreach (var generatedEvent in result.Result.GeneratedEvents)
                {
                    await _eventPublisher.PublishAsync(
                        generatedEvent,
                        stoppingToken);
                }

                if (result.Result.Telemetry is not null)
                {
                    var telemetry = result.Result.Telemetry;

                    _logger.LogInformation(
                        "Telemetry | Bot={BotId} Status={Status} Lat={Latitude} Lon={Longitude} Power={PowerLevel:F2} ActiveOrder={ActiveOrderId} Queue={QueuedOrderCount}",
                        telemetry.BotId,
                        telemetry.Status,
                        telemetry.CurrentLocation.Latitude,
                        telemetry.CurrentLocation.Longitude,
                        telemetry.PowerLevel,
                        telemetry.ActiveOrderId ?? "none",
                        telemetry.QueuedOrderCount);

                    var telemetryEvent = RobotEventEnvelope.Create(
                        RobotEventTypes.RobotTelemetryUpdated,
                        telemetry,
                        telemetry.BotId);

                    await _eventPublisher.PublishAsync(
                        telemetryEvent,
                        stoppingToken);
                }
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_options.TickIntervalSeconds),
                stoppingToken);
        }
    }
}