// Background service that reads bot events from Azure Event Hub and advances order
// status accordingly (#41). Mirrors the consumer pattern used by the RobotSimulator
// (AzureRobotEventConsumer) but runs as an ASP.NET Core hosted service.
//
// CONFIG (StatusConsumer section) — must point at the hub the simulator PUBLISHES to
// (RobotStatusUpdated / RobotOrderAssignmentResponse / RobotDeliveryCompleted). This is
// the simulator's OUTPUT hub, NOT the "robot-input" hub we publish assignments to.
// ASSUMPTION to confirm with the simulator team: hub name + consumer group below.
// If unconfigured, the consumer logs a warning and stays idle so the API still runs.
using System.Text.Json;
using Azure.Messaging.EventHubs.Consumer;
using OrderService.Services;

namespace OrderService.Events;

public sealed class OrderStatusConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<OrderStatusConsumer> _logger;

    public OrderStatusConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<OrderStatusConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = _config["StatusConsumer:ConnectionString"];
        var hubName = _config["StatusConsumer:EventHubName"];
        var consumerGroup = _config["StatusConsumer:ConsumerGroup"]
            ?? EventHubConsumerClient.DefaultConsumerGroupName;

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(hubName))
        {
            _logger.LogWarning(
                "StatusConsumer is not configured (StatusConsumer:ConnectionString / EventHubName). " +
                "Order status will NOT auto-update from bot events.");
            return;
        }

        _logger.LogInformation(
            "Starting order-status consumer. EventHubName={EventHubName} ConsumerGroup={ConsumerGroup}",
            hubName, consumerGroup);

        await using var client = new EventHubConsumerClient(consumerGroup, connectionString, hubName);

        try
        {
            // Read only events arriving from now on; status is event-driven and forward-only.
            await foreach (var partitionEvent in client.ReadEventsAsync(
                startReadingAtEarliestEvent: false, cancellationToken: stoppingToken))
            {
                if (partitionEvent.Data is null)
                    continue;

                await HandleMessageAsync(partitionEvent.Data.EventBody.ToString(), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Order-status consumer stopped unexpectedly.");
        }
    }

    private async Task HandleMessageAsync(string body, CancellationToken ct)
    {
        RobotEventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<RobotEventEnvelope>(body, RobotEventJson.Options);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize bot event. Skipping.");
            return;
        }

        if (envelope is null || string.IsNullOrWhiteSpace(envelope.EventType))
            return;

        try
        {
            // DbContext is scoped — create a scope per message since this service is a singleton.
            using var scope = _scopeFactory.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            await orderService.ApplyStatusEventAsync(envelope, ct);
        }
        catch (Exception ex)
        {
            // Don't let one bad message kill the consumer loop.
            _logger.LogError(ex, "Failed to apply bot event. EventType={EventType}", envelope.EventType);
        }
    }
}
