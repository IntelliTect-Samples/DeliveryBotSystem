using DeliveryBot.RobotSimulator.Core.Orders;
using DeliveryBot.RobotSimulator.Core.Simulation;
using DeliveryBot.RobotSimulator.Core.Stock;
using DeliveryBot.RobotSimulator.Core.Telemetry;
using DeliveryBot.RobotSimulator.Events;

namespace DeliveryBot.RobotSimulator.Core.Bots;

public sealed class SimulatedBot
{
    private readonly Dictionary<string, BotStockItem> _stock;
    private readonly Queue<OrderAssignment> _queuedOrders = new();

    private OrderAssignment? _activeOrder;
    private DateTimeOffset _lastTelemetryAt = DateTimeOffset.MinValue;

    public string BotId { get; }
    public string Model { get; private set; }
    public BotStatus Status { get; private set; }
    public GeoLocation CurrentLocation { get; private set; }
    public double PowerLevel { get; private set; }
    public double ExternalTemperature { get; private set; }
    public double InternalStorageTemperature { get; private set; }
    public bool HasActiveOrQueuedOrders => _activeOrder is not null || _queuedOrders.Count > 0;
    public string? ActiveOrderId => _activeOrder?.OrderId;
    public int QueuedOrderCount => _queuedOrders.Count;

    public SimulatedBot(
        string botId,
        string model,
        GeoLocation currentLocation,
        IEnumerable<BotStockItem> stock)
    {
        BotId = botId;
        Model = model;
        CurrentLocation = currentLocation;
        Status = BotStatus.Available;
        PowerLevel = 100;
        ExternalTemperature = 72;
        InternalStorageTemperature = 38;

        _stock = stock.ToDictionary(item => item.ItemId, StringComparer.OrdinalIgnoreCase);
    }

    public BotSnapshot ToSnapshot()
    {
        return new BotSnapshot(
            BotId,
            Model,
            Status,
            CurrentLocation,
            PowerLevel,
            ExternalTemperature,
            InternalStorageTemperature,
            _stock.Values.ToList(),
            _activeOrder?.OrderId,
            _queuedOrders.Count);
    }

    public BotSnapshot Update(UpdateBotRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Model))
        {
            Model = request.Model;
        }

        if (request.CurrentLocation is not null)
        {
            CurrentLocation = request.CurrentLocation;
        }

        if (request.PowerLevel is not null)
        {
            PowerLevel = Math.Clamp(request.PowerLevel.Value, 0, 100);
        }

        if (request.ExternalTemperature is not null)
        {
            ExternalTemperature = request.ExternalTemperature.Value;
        }

        if (request.InternalStorageTemperature is not null)
        {
            InternalStorageTemperature = request.InternalStorageTemperature.Value;
        }

        return ToSnapshot();
    }

    public BotOrderAssignmentResult AssignOrder(OrderAssignment assignment)
    {
        if (!string.Equals(assignment.BotId, BotId, StringComparison.OrdinalIgnoreCase))
        {
            return BotOrderAssignmentResult.Rejected(
                assignment.OrderId,
                BotId,
                "Order was assigned to a different bot.");
        }

        foreach (var item in assignment.Items)
        {
            if (!_stock.TryGetValue(item.ItemId, out var stockItem))
            {
                return BotOrderAssignmentResult.Rejected(
                    assignment.OrderId,
                    BotId,
                    $"Item {item.ItemId} is not stocked by this bot.");
            }

            if (!stockItem.CanReserve(item.Quantity))
            {
                return BotOrderAssignmentResult.Rejected(
                    assignment.OrderId,
                    BotId,
                    $"Insufficient available stock for item {item.ItemId}.");
            }
        }

        foreach (var item in assignment.Items)
        {
            _stock[item.ItemId].Reserve(item.Quantity);
        }

        var generatedEvents = new List<RobotEventEnvelope>
        {
            CreateStockUpdatedEvent("StockReserved", assignment.OrderId)
        };

        if (Status == BotStatus.Available)
        {
            var previousStatus = Status;

            _activeOrder = assignment;
            Status = BotStatus.OnDelivery;

            generatedEvents.Add(CreateStatusUpdatedEvent(
                previousStatus,
                Status,
                "OrderAcceptedDeliveryStarted",
                assignment.OrderId));

            return BotOrderAssignmentResult.Accepted(
                assignment.OrderId,
                BotId,
                "Order accepted and delivery started.",
                generatedEvents);
        }

        _queuedOrders.Enqueue(assignment);

        return BotOrderAssignmentResult.Queued(
            assignment.OrderId,
            BotId,
            "Order accepted and queued.",
            generatedEvents);
    }

    public BotSimulationTickResult Tick(
        DateTimeOffset now,
        TimeSpan elapsed,
        SimulationOptions options)
    {
        var completedOrderIds = new List<string>();
        var generatedEvents = new List<RobotEventEnvelope>();

        if (Status == BotStatus.OnDelivery && _activeOrder is not null)
        {
            MoveTowardDestination(elapsed, options);

            if (HasReachedDestination(options))
            {
                var completedOrderId = CompleteActiveOrder();
                completedOrderIds.Add(completedOrderId);

                generatedEvents.Add(CreateStockUpdatedEvent(
                    "StockFulfilled",
                    completedOrderId));

                var statusBeforeQueueCheck = Status;
                var previousActiveOrderId = completedOrderId;

                var nextOrderStarted = StartNextQueuedOrderIfAvailable();

                if (nextOrderStarted)
                {
                    generatedEvents.Add(CreateStatusUpdatedEvent(
                        statusBeforeQueueCheck,
                        Status,
                        "QueuedOrderStarted",
                        _activeOrder?.OrderId,
                        previousActiveOrderId));
                }
                else
                {
                    generatedEvents.Add(CreateStatusUpdatedEvent(
                        BotStatus.OnDelivery,
                        Status,
                        "DeliveryCompletedNoQueuedOrders",
                        null,
                        previousActiveOrderId));
                }
            }
        }

        SlowlyDrainPower(elapsed);

        var telemetry = ShouldGenerateTelemetry(now, options)
            ? GenerateTelemetry(now)
            : null;

        return new BotSimulationTickResult(
            telemetry,
            completedOrderIds,
            generatedEvents);
    }

    private void MoveTowardDestination(TimeSpan elapsed, SimulationOptions options)
    {
        if (_activeOrder is null)
        {
            return;
        }

        var movementDistance =
            options.DeliverySpeedMetersPerSecond * elapsed.TotalSeconds;

        CurrentLocation = GeoMath.MoveToward(
            CurrentLocation,
            _activeOrder.Destination,
            movementDistance);
    }

    private bool HasReachedDestination(SimulationOptions options)
    {
        if (_activeOrder is null)
        {
            return false;
        }

        var distance = GeoMath.DistanceMeters(
            CurrentLocation,
            _activeOrder.Destination);

        return distance <= options.DestinationArrivalThresholdMeters;
    }

    private string CompleteActiveOrder()
    {
        if (_activeOrder is null)
        {
            throw new InvalidOperationException("Cannot complete delivery because there is no active order.");
        }

        foreach (var item in _activeOrder.Items)
        {
            _stock[item.ItemId].FulfillReserved(item.Quantity);
        }

        var completedOrderId = _activeOrder.OrderId;
        _activeOrder = null;

        return completedOrderId;
    }

    private bool StartNextQueuedOrderIfAvailable()
    {
        if (_queuedOrders.Count > 0)
        {
            _activeOrder = _queuedOrders.Dequeue();
            Status = BotStatus.OnDelivery;
            return true;
        }

        Status = BotStatus.Available;
        return false;
    }

    private void SlowlyDrainPower(TimeSpan elapsed)
    {
        var drainAmount = Status == BotStatus.OnDelivery
            ? 0.02 * elapsed.TotalSeconds
            : 0.005 * elapsed.TotalSeconds;

        PowerLevel = Math.Max(0, PowerLevel - drainAmount);
    }

    private bool ShouldGenerateTelemetry(
        DateTimeOffset now,
        SimulationOptions options)
    {
        var elapsedSinceTelemetry = now - _lastTelemetryAt;

        return elapsedSinceTelemetry.TotalSeconds >= options.TelemetryIntervalSeconds;
    }

    private BotTelemetrySnapshot GenerateTelemetry(DateTimeOffset now)
    {
        _lastTelemetryAt = now;

        return new BotTelemetrySnapshot(
            BotId,
            now,
            Status,
            CurrentLocation,
            PowerLevel,
            ExternalTemperature,
            InternalStorageTemperature,
            _activeOrder?.OrderId,
            _queuedOrders.Count);
    }

    private RobotEventEnvelope CreateStockUpdatedEvent(
        string reason,
        string? relatedOrderId)
    {
        return RobotEventEnvelope.Create(
            RobotEventTypes.RobotStockUpdated,
            new
            {
                botId = BotId,
                reason,
                relatedOrderId,
                stock = _stock.Values.Select(item => new
                {
                    item.ItemId,
                    item.ItemName,
                    item.QuantityOnHand,
                    item.QuantityReserved,
                    item.QuantityAvailable
                }).ToList()
            },
            BotId);
    }

    private RobotEventEnvelope CreateStatusUpdatedEvent(
        BotStatus previousStatus,
        BotStatus currentStatus,
        string reason,
        string? activeOrderId,
        string? previousOrderId = null)
    {
        return RobotEventEnvelope.Create(
            RobotEventTypes.RobotStatusUpdated,
            new
            {
                botId = BotId,
                previousStatus = previousStatus.ToString(),
                currentStatus = currentStatus.ToString(),
                reason,
                activeOrderId,
                previousOrderId,
                queuedOrderCount = _queuedOrders.Count,
                currentLocation = CurrentLocation
            },
            BotId);
    }
}