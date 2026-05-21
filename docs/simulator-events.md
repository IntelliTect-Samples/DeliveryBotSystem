# Events Reference

The simulator uses a shared event envelope for all robot-related events.

## Event Envelope

```json
{
  "eventId": "6d98914c59d54f7ca7156a8fda61c24d",
  "eventType": "RobotTelemetryUpdated",
  "schemaVersion": "1.0",
  "timestampUtc": "2026-05-15T20:15:00Z",
  "botId": "bot-001",
  "source": "robot-simulator",
  "isSimulated": true,
  "data": {}
}
```

Fields:

| Field | Description |
|---|---|
| `eventId` | Unique event ID |
| `eventType` | Event type name |
| `schemaVersion` | Event schema version |
| `timestampUtc` | Event creation timestamp |
| `botId` | Target or source bot ID, where applicable |
| `source` | Producing system, such as `robot-simulator` |
| `isSimulated` | Indicates event came from simulated data |
| `data` | Event-specific payload |

## Event Types

Current event types:

```text
RobotTelemetryUpdated
RobotStatusUpdated
RobotStockUpdated
RobotOrderAssignment
RobotOrderAssignmentResponse
RobotDeliveryCompleted
BotCreated
BotUpdated
BotRemoved
```

## RobotOrderAssignment

Input event consumed by the simulator when Event Hub input is enabled.

Example:

```json
{
  "eventId": "manual-test-001",
  "eventType": "RobotOrderAssignment",
  "schemaVersion": "1.0",
  "timestampUtc": "2026-05-15T00:00:00Z",
  "botId": "bot-001",
  "source": "order-service-test",
  "isSimulated": true,
  "data": {
    "orderId": "eventhub-input-order-001",
    "botId": "bot-001",
    "items": [
      {
        "itemId": "water",
        "quantity": 1
      }
    ],
    "destination": {
      "latitude": 33.426,
      "longitude": -111.9395
    }
  }
}
```

Consumer filtering rules:

```text
Ignore source == robot-simulator
Ignore eventType != RobotOrderAssignment
```

## RobotOrderAssignmentResponse

Published when an order is accepted, queued, rejected, or rejected because the bot does not exist.

Accepted example:

```json
{
  "eventType": "RobotOrderAssignmentResponse",
  "botId": "bot-001",
  "data": {
    "orderId": "order-001",
    "botId": "bot-001",
    "result": "Accepted",
    "message": "Order accepted and delivery started."
  }
}
```

Queued example:

```json
{
  "eventType": "RobotOrderAssignmentResponse",
  "botId": "bot-001",
  "data": {
    "orderId": "order-002",
    "botId": "bot-001",
    "result": "Queued",
    "message": "Order accepted and queued."
  }
}
```

Rejected example:

```json
{
  "eventType": "RobotOrderAssignmentResponse",
  "botId": "bot-001",
  "data": {
    "orderId": "order-003",
    "botId": "bot-001",
    "result": "Rejected",
    "message": "Insufficient available stock for item water."
  }
}
```

Missing bot example:

```json
{
  "eventType": "RobotOrderAssignmentResponse",
  "botId": "bot-999",
  "data": {
    "orderId": "order-missing-bot",
    "botId": "bot-999",
    "result": "Rejected",
    "reason": "BotNotFound"
  }
}
```

## RobotStockUpdated

Published when stock is reserved or fulfilled.

Reservation example:

```json
{
  "eventType": "RobotStockUpdated",
  "botId": "bot-001",
  "data": {
    "botId": "bot-001",
    "reason": "StockReserved",
    "relatedOrderId": "order-001",
    "stock": [
      {
        "itemId": "water",
        "itemName": "Water",
        "quantityOnHand": 20,
        "quantityReserved": 1,
        "quantityAvailable": 19
      }
    ]
  }
}
```

Fulfillment example:

```json
{
  "eventType": "RobotStockUpdated",
  "botId": "bot-001",
  "data": {
    "botId": "bot-001",
    "reason": "StockFulfilled",
    "relatedOrderId": "order-001",
    "stock": [
      {
        "itemId": "water",
        "itemName": "Water",
        "quantityOnHand": 19,
        "quantityReserved": 0,
        "quantityAvailable": 19
      }
    ]
  }
}
```

## RobotStatusUpdated

Published when bot status or active delivery state changes.

Delivery started example:

```json
{
  "eventType": "RobotStatusUpdated",
  "botId": "bot-001",
  "data": {
    "botId": "bot-001",
    "previousStatus": "Available",
    "currentStatus": "OnDelivery",
    "reason": "OrderAcceptedDeliveryStarted",
    "activeOrderId": "order-001",
    "previousOrderId": null,
    "queuedOrderCount": 0,
    "currentLocation": {
      "latitude": 33.4255,
      "longitude": -111.94
    }
  }
}
```

Queued order handoff example:

```json
{
  "eventType": "RobotStatusUpdated",
  "botId": "bot-001",
  "data": {
    "botId": "bot-001",
    "previousStatus": "OnDelivery",
    "currentStatus": "OnDelivery",
    "reason": "QueuedOrderStarted",
    "activeOrderId": "order-002",
    "previousOrderId": "order-001",
    "queuedOrderCount": 0
  }
}
```

Delivery completed with no queue example:

```json
{
  "eventType": "RobotStatusUpdated",
  "botId": "bot-001",
  "data": {
    "botId": "bot-001",
    "previousStatus": "OnDelivery",
    "currentStatus": "Available",
    "reason": "DeliveryCompletedNoQueuedOrders",
    "activeOrderId": null,
    "previousOrderId": "order-001",
    "queuedOrderCount": 0
  }
}
```

## RobotTelemetryUpdated

Published periodically for each bot.

Example:

```json
{
  "eventType": "RobotTelemetryUpdated",
  "botId": "bot-001",
  "data": {
    "botId": "bot-001",
    "timestampUtc": "2026-05-15T20:15:00Z",
    "status": "OnDelivery",
    "currentLocation": {
      "latitude": 33.4257,
      "longitude": -111.9398
    },
    "powerLevel": 99.8,
    "externalTemperature": 72,
    "internalStorageTemperature": 38,
    "activeOrderId": "order-001",
    "queuedOrderCount": 0
  }
}
```

## RobotDeliveryCompleted

Published when the active delivery completes.

Example:

```json
{
  "eventType": "RobotDeliveryCompleted",
  "botId": "bot-001",
  "data": {
    "orderId": "order-001",
    "botId": "bot-001",
    "completedAtUtc": "2026-05-15T20:20:00Z"
  }
}
```

## BotCreated

Published when a bot is added through the API.

```json
{
  "eventType": "BotCreated",
  "botId": "bot-900",
  "data": {
    "bot": {
      "botId": "bot-900",
      "model": "DeliveryBot-Test",
      "status": "Available"
    }
  }
}
```

## BotUpdated

Published when a bot is updated through the API.

```json
{
  "eventType": "BotUpdated",
  "botId": "bot-900",
  "data": {
    "bot": {
      "botId": "bot-900",
      "model": "DeliveryBot-Test-Updated"
    }
  }
}
```

## BotRemoved

Published when a bot is removed through the API.

```json
{
  "eventType": "BotRemoved",
  "botId": "bot-900",
  "data": {
    "bot": {
      "botId": "bot-900"
    }
  }
}
```
