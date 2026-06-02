using System.Text.Json;
using ReadableBotState.Projection;
using ReadableBotState.RobotEvents;
using Xunit;

namespace ReadableBotState.Tests.Projection;

public class BotReadModelProjectorTests
{
    private readonly BotReadModelProjector _projector = new();

    [Fact]
    public void Apply_TelemetryEvent_CreatesReadableBotDocument()
    {
        var envelope = CreateEnvelope(
            RobotEventTypes.RobotTelemetryUpdated,
            "event-1",
            "bot-001",
            DateTimeOffset.Parse("2026-05-15T20:15:00Z"),
            new
            {
                botId = "bot-001",
                timestampUtc = "2026-05-15T20:15:00Z",
                status = "Available",
                currentLocation = new
                {
                    latitude = 33.4255,
                    longitude = -111.94
                },
                powerLevel = 99.8,
                externalTemperature = 72,
                internalStorageTemperature = 38,
                activeOrderId = (string?)null,
                queuedOrderCount = 0
            });

        var result = _projector.Apply(null, envelope);

        Assert.True(result.ShouldPersist);
        Assert.NotNull(result.Document);
        Assert.Equal("bot-001", result.Document.BotId);
        Assert.Equal("bot-001", result.Document.Id);
        Assert.Equal("Available", result.Document.Status);
        Assert.True(result.Document.IsAvailable);
        Assert.Equal(99.8, result.Document.PowerLevel);
        Assert.Equal(DateTimeOffset.Parse("2026-05-15T20:15:00Z"), result.Document.LastTelemetryEventUtc);
    }

    [Fact]
    public void Apply_TelemetryEvent_AcceptsPayloadAlias()
    {
        var json = """
            {
              "eventId": "event-payload",
              "eventType": "RobotTelemetryUpdated",
              "schemaVersion": "1.0",
              "timestampUtc": "2026-05-15T20:15:00Z",
              "botId": "bot-001",
              "source": "robot-simulator",
              "isSimulated": true,
              "payload": {
                "botId": "bot-001",
                "timestampUtc": "2026-05-15T20:15:00Z",
                "status": "Available",
                "powerLevel": 88
              }
            }
            """;

        var envelope = JsonSerializer.Deserialize<RobotEventEnvelope>(
            json,
            RobotEventJson.SerializerOptions);

        var result = _projector.Apply(null, envelope!);

        Assert.True(result.ShouldPersist);
        Assert.Equal("bot-001", result.Document!.BotId);
        Assert.Equal(88, result.Document.PowerLevel);
    }


    [Fact]
    public void Apply_TelemetryEvent_AcceptsSimulatorNumericStatus()
    {
        var envelope = CreateEnvelope(
            RobotEventTypes.RobotTelemetryUpdated,
            "event-numeric-status",
            "bot-001",
            DateTimeOffset.Parse("2026-05-15T20:15:00Z"),
            new
            {
                botId = "bot-001",
                timestampUtc = "2026-05-15T20:15:00Z",
                status = 0,
                powerLevel = 99.8
            });

        var result = _projector.Apply(null, envelope);

        Assert.True(result.ShouldPersist);
        Assert.Equal("Available", result.Document!.Status);
        Assert.True(result.Document.IsAvailable);
    }

    [Fact]
    public void Apply_OlderTelemetryEvent_DoesNotOverwriteNewerTelemetry()
    {
        var newer = CreateEnvelope(
            RobotEventTypes.RobotTelemetryUpdated,
            "event-new",
            "bot-001",
            DateTimeOffset.Parse("2026-05-15T20:20:00Z"),
            new
            {
                botId = "bot-001",
                timestampUtc = "2026-05-15T20:20:00Z",
                status = "Available",
                powerLevel = 90
            });
        var older = CreateEnvelope(
            RobotEventTypes.RobotTelemetryUpdated,
            "event-old",
            "bot-001",
            DateTimeOffset.Parse("2026-05-15T20:10:00Z"),
            new
            {
                botId = "bot-001",
                timestampUtc = "2026-05-15T20:10:00Z",
                status = "Available",
                powerLevel = 10
            });

        var first = _projector.Apply(null, newer);
        var second = _projector.Apply(first.Document, older);

        Assert.False(second.ShouldPersist);
        Assert.Equal(90, second.Document?.PowerLevel);
        Assert.Equal(DateTimeOffset.Parse("2026-05-15T20:20:00Z"), second.Document?.LastTelemetryEventUtc);
    }

    [Fact]
    public void Apply_DuplicateEvent_DoesNotPersistAgain()
    {
        var envelope = CreateEnvelope(
            RobotEventTypes.RobotStatusUpdated,
            "event-1",
            "bot-001",
            DateTimeOffset.Parse("2026-05-15T20:15:00Z"),
            new
            {
                botId = "bot-001",
                previousStatus = "Available",
                currentStatus = "OnDelivery",
                activeOrderId = "order-001",
                queuedOrderCount = 0
            });

        var first = _projector.Apply(null, envelope);
        var second = _projector.Apply(first.Document, envelope);

        Assert.True(first.ShouldPersist);
        Assert.False(second.ShouldPersist);
        Assert.Equal("OnDelivery", second.Document?.Status);
        Assert.Contains("event-1", second.Document!.LastProcessedEventIds);
    }

    [Fact]
    public void Apply_StockEvent_UpdatesInventory()
    {
        var envelope = CreateEnvelope(
            RobotEventTypes.RobotStockUpdated,
            "event-stock",
            "bot-001",
            DateTimeOffset.Parse("2026-05-15T20:15:00Z"),
            new
            {
                botId = "bot-001",
                reason = "StockReserved",
                relatedOrderId = "order-001",
                stock = new[]
                {
                    new
                    {
                        itemId = "water",
                        itemName = "Water",
                        quantityOnHand = 20,
                        quantityReserved = 1,
                        quantityAvailable = 19
                    }
                }
            });

        var result = _projector.Apply(null, envelope);

        Assert.True(result.ShouldPersist);
        var item = Assert.Single(result.Document!.Inventory);
        Assert.Equal("water", item.ItemId);
        Assert.Equal(20, item.QuantityOnHand);
        Assert.Equal(1, item.QuantityReserved);
        Assert.Equal(19, item.QuantityAvailable);
    }

    private static RobotEventEnvelope CreateEnvelope(
        string eventType,
        string eventId,
        string botId,
        DateTimeOffset timestampUtc,
        object data)
    {
        var dataElement = JsonSerializer.SerializeToElement(
            data,
            RobotEventJson.SerializerOptions);

        return new RobotEventEnvelope
        {
            EventId = eventId,
            EventType = eventType,
            SchemaVersion = "1.0",
            TimestampUtc = timestampUtc,
            BotId = botId,
            Source = "robot-simulator",
            IsSimulated = true,
            Data = dataElement
        };
    }
}
