// Pure translation from simulator bot events to order status changes.
// Kept free of database/IO so it can be unit-tested in isolation.
//
// Mapping is derived from docs/simulator-events.md. ASSUMPTIONS flagged for the
// simulator team to confirm:
//   1. The simulator echoes back the SAME orderId we sent in RobotOrderAssignment
//      (we send Order.Id as a GUID string) in its response/status events.
//   2. Event/data field names match the doc examples (camelCase).
using OrderService.Models;

namespace OrderService.Events;

public static class OrderStatusMapping
{
    // A status change to apply: which order, and the new status.
    public readonly record struct StatusChange(string OrderId, OrderStatus Status);

    // Translate one envelope into zero or more status changes.
    public static IEnumerable<StatusChange> Map(RobotEventEnvelope evt)
    {
        var data = evt.Data;
        if (data is null)
            yield break;

        switch (evt.EventType)
        {
            // Bot accepted/queued/rejected the assignment we sent it.
            case "RobotOrderAssignmentResponse":
                if (!string.IsNullOrWhiteSpace(data.OrderId) && !string.IsNullOrWhiteSpace(data.Result))
                {
                    var mapped = data.Result switch
                    {
                        "Accepted" => OrderStatus.InTransit, // doc: "Order accepted and delivery started."
                        "Queued"   => OrderStatus.Assigned,  // accepted but waiting behind another order
                        "Rejected" => OrderStatus.Failed,
                        _          => (OrderStatus?)null
                    };
                    if (mapped is { } s)
                        yield return new StatusChange(data.OrderId, s);
                }
                break;

            // Bot status / active-delivery state changed.
            case "RobotStatusUpdated":
                // A completed delivery references the finished order via previousOrderId.
                if (data.Reason is { } reason &&
                    reason.StartsWith("DeliveryCompleted", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(data.PreviousOrderId))
                {
                    yield return new StatusChange(data.PreviousOrderId, OrderStatus.Delivered);
                }
                // The order the bot is actively delivering is in transit.
                if (string.Equals(data.CurrentStatus, "OnDelivery", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(data.ActiveOrderId))
                {
                    yield return new StatusChange(data.ActiveOrderId, OrderStatus.InTransit);
                }
                break;

            // Explicit delivery-completed event.
            case "RobotDeliveryCompleted":
                if (!string.IsNullOrWhiteSpace(data.OrderId))
                    yield return new StatusChange(data.OrderId, OrderStatus.Delivered);
                break;
        }
    }

    // Forward-only guard so out-of-order or duplicate events can't regress an order.
    // Pending < Assigned < InTransit < terminal (Delivered/Cancelled/Failed).
    public static bool IsForward(OrderStatus current, OrderStatus next) =>
        Rank(next) > Rank(current);

    private static int Rank(OrderStatus status) => status switch
    {
        OrderStatus.Pending   => 0,
        OrderStatus.Assigned  => 1,
        OrderStatus.InTransit => 2,
        // Terminal states share the top rank — once terminal, status no longer moves.
        OrderStatus.Delivered => 3,
        OrderStatus.Cancelled => 3,
        OrderStatus.Failed    => 3,
        _ => 0
    };
}
