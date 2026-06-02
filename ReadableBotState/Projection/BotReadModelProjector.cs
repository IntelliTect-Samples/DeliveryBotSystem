using System.Text.Json;
using ReadableBotState.ReadModel;
using ReadableBotState.RobotEvents;

namespace ReadableBotState.Projection;

public sealed class BotReadModelProjector
{
    private const int MaxTrackedEventIds = 50;

    public ProjectionResult Apply(BotReadModel? current, RobotEventEnvelope envelope)
    {
        var validationError = ValidateEnvelope(envelope);
        if (validationError is not null)
        {
            return new ProjectionResult(false, current, validationError);
        }

        var botId = envelope.BotId!.Trim();
        var eventId = envelope.EventId!.Trim();
        var eventType = envelope.EventType!.Trim();
        var eventTimestamp = envelope.TimestampUtc!.Value;
        var document = current ?? CreateDocument(botId, eventTimestamp);

        if (document.LastProcessedEventIds.Contains(eventId, StringComparer.OrdinalIgnoreCase))
        {
            return new ProjectionResult(false, document, $"Duplicate event ignored: {eventId}");
        }

        var changed = eventType switch
        {
            RobotEventTypes.BotCreated => ApplyBotSnapshot(document, envelope, eventTimestamp, isRemoved: false),
            RobotEventTypes.BotUpdated => ApplyBotSnapshot(document, envelope, eventTimestamp, isRemoved: document.IsRemoved),
            RobotEventTypes.BotRemoved => ApplyBotRemoved(document, envelope, eventTimestamp),
            RobotEventTypes.RobotTelemetryUpdated => ApplyTelemetry(document, envelope, eventTimestamp),
            RobotEventTypes.RobotStatusUpdated => ApplyStatus(document, envelope, eventTimestamp),
            RobotEventTypes.RobotStockUpdated => ApplyStock(document, envelope, eventTimestamp),
            _ => false
        };

        if (!changed)
        {
            return new ProjectionResult(false, document, $"Event ignored: {eventType}");
        }

        TrackProcessedEvent(document, eventId);
        document.UpdatedAtUtc = eventTimestamp;
        return new ProjectionResult(true, document, $"Event applied: {eventType}");
    }

    private static string? ValidateEnvelope(RobotEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.EventId))
        {
            return "Event rejected: missing eventId.";
        }

        if (string.IsNullOrWhiteSpace(envelope.EventType))
        {
            return "Event rejected: missing eventType.";
        }

        if (string.IsNullOrWhiteSpace(envelope.SchemaVersion))
        {
            return "Event rejected: missing schemaVersion.";
        }

        if (envelope.TimestampUtc is null)
        {
            return "Event rejected: missing timestampUtc.";
        }

        if (string.IsNullOrWhiteSpace(envelope.BotId))
        {
            return "Event rejected: missing botId.";
        }

        if (envelope.EffectiveData.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return "Event rejected: missing data payload.";
        }

        return null;
    }

    private static BotReadModel CreateDocument(string botId, DateTimeOffset timestampUtc)
    {
        return new BotReadModel
        {
            Id = botId,
            BotId = botId,
            UpdatedAtUtc = timestampUtc
        };
    }

    private static bool ApplyBotSnapshot(
        BotReadModel document,
        RobotEventEnvelope envelope,
        DateTimeOffset eventTimestamp,
        bool isRemoved)
    {
        if (IsOlderOrEqual(eventTimestamp, document.LastManagementEventUtc))
        {
            return false;
        }

        var payload = Deserialize<BotSnapshotPayload>(envelope);
        var bot = payload?.Bot;
        if (bot is null)
        {
            return false;
        }

        document.Model = bot.Model ?? document.Model;
        document.Status = bot.Status ?? document.Status;
        document.CurrentLocation = bot.CurrentLocation ?? document.CurrentLocation;
        document.PowerLevel = bot.PowerLevel ?? document.PowerLevel;
        document.ExternalTemperature = bot.ExternalTemperature ?? document.ExternalTemperature;
        document.InternalStorageTemperature = bot.InternalStorageTemperature ?? document.InternalStorageTemperature;
        document.ActiveOrderId = bot.ActiveOrderId;
        document.QueuedOrderCount = bot.QueuedOrderCount ?? document.QueuedOrderCount;
        document.IsRemoved = isRemoved;

        if (bot.Stock is not null)
        {
            document.Inventory = bot.Stock.ToList();
            document.LastInventoryEventUtc = eventTimestamp;
        }

        document.LastManagementEventUtc = eventTimestamp;
        RefreshAvailability(document);
        return true;
    }

    private static bool ApplyBotRemoved(
        BotReadModel document,
        RobotEventEnvelope envelope,
        DateTimeOffset eventTimestamp)
    {
        if (IsOlderOrEqual(eventTimestamp, document.LastManagementEventUtc))
        {
            return false;
        }

        document.IsRemoved = true;
        document.IsAvailable = false;
        document.LastManagementEventUtc = eventTimestamp;
        return true;
    }

    private static bool ApplyTelemetry(
        BotReadModel document,
        RobotEventEnvelope envelope,
        DateTimeOffset eventTimestamp)
    {
        var payload = Deserialize<RobotTelemetryUpdatedPayload>(envelope);
        if (payload is null)
        {
            return false;
        }

        var telemetryTimestamp = payload.TimestampUtc ?? eventTimestamp;
        if (IsOlderOrEqual(telemetryTimestamp, document.LastTelemetryEventUtc))
        {
            return false;
        }

        document.Status = payload.Status ?? document.Status;
        document.CurrentLocation = payload.CurrentLocation ?? document.CurrentLocation;
        document.PowerLevel = payload.PowerLevel ?? document.PowerLevel;
        document.ExternalTemperature = payload.ExternalTemperature ?? document.ExternalTemperature;
        document.InternalStorageTemperature = payload.InternalStorageTemperature ?? document.InternalStorageTemperature;
        document.ActiveOrderId = payload.ActiveOrderId;
        document.QueuedOrderCount = payload.QueuedOrderCount ?? document.QueuedOrderCount;
        document.LastTelemetryEventUtc = telemetryTimestamp;

        RefreshAvailability(document);
        return true;
    }

    private static bool ApplyStatus(
        BotReadModel document,
        RobotEventEnvelope envelope,
        DateTimeOffset eventTimestamp)
    {
        if (IsOlderOrEqual(eventTimestamp, document.LastStatusEventUtc))
        {
            return false;
        }

        var payload = Deserialize<RobotStatusUpdatedPayload>(envelope);
        if (payload is null)
        {
            return false;
        }

        document.Status = payload.CurrentStatus ?? document.Status;
        document.ActiveOrderId = payload.ActiveOrderId;
        document.QueuedOrderCount = payload.QueuedOrderCount ?? document.QueuedOrderCount;
        document.CurrentLocation = payload.CurrentLocation ?? document.CurrentLocation;
        document.LastStatusEventUtc = eventTimestamp;

        RefreshAvailability(document);
        return true;
    }

    private static bool ApplyStock(
        BotReadModel document,
        RobotEventEnvelope envelope,
        DateTimeOffset eventTimestamp)
    {
        if (IsOlderOrEqual(eventTimestamp, document.LastInventoryEventUtc))
        {
            return false;
        }

        var payload = Deserialize<RobotStockUpdatedPayload>(envelope);
        if (payload?.Stock is null)
        {
            return false;
        }

        document.Inventory = payload.Stock.ToList();
        document.LastInventoryEventUtc = eventTimestamp;
        return true;
    }

    private static T? Deserialize<T>(RobotEventEnvelope envelope)
    {
        try
        {
            return envelope.EffectiveData.Deserialize<T>(RobotEventJson.SerializerOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private static bool IsOlderOrEqual(DateTimeOffset incoming, DateTimeOffset? current)
    {
        return current is not null && incoming <= current.Value;
    }

    private static void RefreshAvailability(BotReadModel document)
    {
        document.IsAvailable = !document.IsRemoved &&
                               string.Equals(document.Status, "Available", StringComparison.OrdinalIgnoreCase);
    }

    private static void TrackProcessedEvent(BotReadModel document, string eventId)
    {
        document.LastProcessedEventIds.RemoveAll(id => string.Equals(id, eventId, StringComparison.OrdinalIgnoreCase));
        document.LastProcessedEventIds.Add(eventId);

        if (document.LastProcessedEventIds.Count <= MaxTrackedEventIds)
        {
            return;
        }

        document.LastProcessedEventIds = document.LastProcessedEventIds
            .Skip(document.LastProcessedEventIds.Count - MaxTrackedEventIds)
            .ToList();
    }
}
