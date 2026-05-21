using System.Text.Json;
using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Configuration;
using DeliveryBot.RobotSimulator.Infrastructure.Events;

namespace DeliveryBot.RobotSimulator.Api;

public sealed class EventHubOrderAssignmentWorker : BackgroundService
{
    private readonly EventTransportOptions _options;
    private readonly IRobotEventConsumer _eventConsumer;
    private readonly OrderAssignmentHandler _orderAssignmentHandler;
    private readonly ILogger<EventHubOrderAssignmentWorker> _logger;

    public EventHubOrderAssignmentWorker(
        EventTransportOptions options,
        IRobotEventConsumer eventConsumer,
        OrderAssignmentHandler orderAssignmentHandler,
        ILogger<EventHubOrderAssignmentWorker> logger)
    {
        _options = options;
        _eventConsumer = eventConsumer;
        _orderAssignmentHandler = orderAssignmentHandler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableInputConsumer)
        {
            _logger.LogInformation("Event Hub order assignment input consumer is disabled.");
            return;
        }

        _logger.LogInformation(
            "Event Hub order assignment input consumer is enabled. InputEventHubName={InputEventHubName}",
            _options.InputEventHubName);

        await _eventConsumer.StartAsync(
            HandleEnvelopeAsync,
            stoppingToken);
    }

    private async Task HandleEnvelopeAsync(
        RobotEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (string.Equals(envelope.Source, "robot-simulator", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Ignoring event produced by this simulator. EventType={EventType} EventId={EventId}",
                envelope.EventType,
                envelope.EventId);

            return;
        }

        if (!string.Equals(envelope.EventType, RobotEventTypes.RobotOrderAssignment, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Ignoring non-order-assignment event. EventType={EventType} EventId={EventId}",
                envelope.EventType,
                envelope.EventId);

            return;
        }

        OrderAssignment? assignment;

        try
        {
            assignment = JsonSerializer.Deserialize<OrderAssignment>(
                JsonSerializer.Serialize(envelope.Data, RobotEventJsonSerializerOptions.Default),
                RobotEventJsonSerializerOptions.Default);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to deserialize RobotOrderAssignment event data. EventId={EventId}",
                envelope.EventId);

            return;
        }

        if (assignment is null)
        {
            _logger.LogWarning(
                "RobotOrderAssignment event data was empty. EventId={EventId}",
                envelope.EventId);

            return;
        }

        await _orderAssignmentHandler.HandleAsync(
            assignment,
            cancellationToken);
    }
}