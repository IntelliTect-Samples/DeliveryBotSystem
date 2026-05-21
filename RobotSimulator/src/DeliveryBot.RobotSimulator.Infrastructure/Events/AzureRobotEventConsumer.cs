using System.Text.Json;
using Azure.Messaging.EventHubs.Consumer;
using DeliveryBot.RobotSimulator.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public sealed class AzureRobotEventConsumer : IRobotEventConsumer, IAsyncDisposable
{
    private readonly EventHubConsumerClient _consumerClient;
    private readonly EventTransportOptions _options;
    private readonly ILogger<AzureRobotEventConsumer> _logger;

    public AzureRobotEventConsumer(
        EventTransportOptions options,
        ILogger<AzureRobotEventConsumer> logger)
    {
        _options = options;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "EventTransport:ConnectionString is required when Azure Event Hub input consumer is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.InputEventHubName))
        {
            throw new InvalidOperationException(
                "EventTransport:InputEventHubName is required when Azure Event Hub input consumer is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.ConsumerGroup))
        {
            throw new InvalidOperationException(
                "EventTransport:ConsumerGroup is required when Azure Event Hub input consumer is enabled.");
        }

        _consumerClient = new EventHubConsumerClient(
            options.ConsumerGroup,
            options.ConnectionString,
            options.InputEventHubName);
    }

    public async Task StartAsync(
        Func<RobotEventEnvelope, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting Azure Event Hub consumer. InputEventHubName={InputEventHubName} ConsumerGroup={ConsumerGroup}",
            _options.InputEventHubName,
            _options.ConsumerGroup);

        await foreach (var partitionEvent in _consumerClient.ReadEventsAsync(cancellationToken))
        {
            if (partitionEvent.Data is null)
            {
                continue;
            }

            RobotEventEnvelope? envelope;

            try
            {
                envelope = JsonSerializer.Deserialize<RobotEventEnvelope>(
                    partitionEvent.Data.EventBody.ToString(),
                    RobotEventJsonSerializerOptions.Default);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to deserialize Event Hub message as RobotEventEnvelope. Partition={PartitionId} Offset={Offset}",
                    partitionEvent.Partition.PartitionId,
                    partitionEvent.Data.OffsetString);

                continue;
            }

            if (envelope is null)
            {
                continue;
            }

            await handler(envelope, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _consumerClient.DisposeAsync();
    }
}