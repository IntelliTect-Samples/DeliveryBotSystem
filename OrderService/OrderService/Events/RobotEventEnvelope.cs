// Shape of the events the simulator publishes about a bot/delivery.
// Mirrors the shared envelope documented in docs/simulator-events.md.
// We only model the fields the Order Service needs to advance order status.
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderService.Events;

public sealed class RobotEventEnvelope
{
    public string EventType { get; set; } = string.Empty;

    // Producing system, e.g. "robot-simulator". We ignore events we published ourselves.
    public string? Source { get; set; }

    // Event-specific payload. Fields used vary by EventType.
    public RobotEventData? Data { get; set; }
}

// Union of the data fields we read across the event types we consume.
// Any given event populates only the subset relevant to it; the rest stay null.
public sealed class RobotEventData
{
    // RobotOrderAssignmentResponse / RobotDeliveryCompleted
    public string? OrderId { get; set; }

    // RobotStatusUpdated
    public string? ActiveOrderId { get; set; }
    public string? PreviousOrderId { get; set; }

    // RobotOrderAssignmentResponse: "Accepted" | "Queued" | "Rejected"
    public string? Result { get; set; }

    // RobotStatusUpdated: e.g. "OnDelivery" | "Available"
    public string? CurrentStatus { get; set; }

    // RobotStatusUpdated: e.g. "OrderAcceptedDeliveryStarted" | "DeliveryCompletedNoQueuedOrders"
    public string? Reason { get; set; }
}

public static class RobotEventJson
{
    // Simulator emits camelCase; be lenient about casing and ignore unknown fields.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
