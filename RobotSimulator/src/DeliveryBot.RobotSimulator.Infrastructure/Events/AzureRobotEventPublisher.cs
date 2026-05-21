using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using DeliveryBot.RobotSimulator.Events;
using DeliveryBot.RobotSimulator.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace DeliveryBot.RobotSimulator.Infrastructure.Events;

public sealed class AzureRobotEventPublisher : IRobotEventPublisher, IAsyncDisposable
{
    private readonly EventHubProducerClient _producerClient;
    private readonly ILogger<AzureRobotEventPublisher> _logger;

    public AzureRobotEventPublisher(
        EventTransportOptions options,
        ILogger<AzureRobotEventPublisher> logger)
    {
        _logger = logger;

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                "EventTransport:ConnectionString is required when EventTransport:Mode is AzureEventHub.");
        }

        if (string.IsNullOrWhiteSpace(options.OutputEventHubName))
        {
            throw new InvalidOperationException(
                "EventTransport:OutputEventHubName is required when EventTransport:Mode is AzureEventHub.");
        }

        _producerClient = new EventHubProducerClient(
            options.ConnectionString,
            options.OutputEventHubName);
    }

    public async Task PublishAsync(
        RobotEventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(
            envelope,
            RobotEventJsonSerializerOptions.Default);

        var eventData = new EventData(Encoding.UTF8.GetBytes(json))
        {
            MessageId = envelope.EventId,
            ContentType = "application/json",
            CorrelationId = envelope.BotId
        };

        eventData.Properties["eventType"] = envelope.EventType;
        eventData.Properties["schemaVersion"] = envelope.SchemaVersion;
        eventData.Properties["source"] = envelope.Source;
        eventData.Properties["isSimulated"] = envelope.IsSimulated;

        if (!string.IsNullOrWhiteSpace(envelope.BotId))
        {
            eventData.Properties["botId"] = envelope.BotId;
        }

        if (!string.IsNullOrWhiteSpace(envelope.BotId))
        {
            await _producerClient.SendAsync(
                new[] { eventData },
                new SendEventOptions
                {
                    PartitionKey = envelope.BotId
                },
                cancellationToken);
        }
        else
        {
            await _producerClient.SendAsync(
                new[] { eventData },
                cancellationToken);
        }

        _logger.LogInformation(
            "Published robot event to Azure Event Hub. EventType={EventType} EventId={EventId} BotId={BotId}",
            envelope.EventType,
            envelope.EventId,
            envelope.BotId ?? "none");
    }

    public async ValueTask DisposeAsync()
    {
        await _producerClient.DisposeAsync();
    }
}