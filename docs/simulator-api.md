# API Reference

The simulator exposes HTTP endpoints for local/manual testing and bot management. These endpoints are useful even when Event Hub mode is enabled.

## Health

```http
GET /health
```

Example response:

```json
{
  "status": "healthy",
  "service": "robot-simulator"
}
```

## Get All Bots

```http
GET /bots
```

Returns all current bot snapshots.

Example response shape:

```json
[
  {
    "botId": "bot-001",
    "model": "DeliveryBot-V1",
    "status": "Available",
    "currentLocation": {
      "latitude": 33.4255,
      "longitude": -111.94
    },
    "powerLevel": 99.95,
    "externalTemperature": 72,
    "internalStorageTemperature": 38,
    "stock": [],
    "activeOrderId": null,
    "queuedOrderCount": 0
  }
]
```

## Get Bot By ID

```http
GET /bots/{botId}
```

Example:

```http
GET /bots/bot-001
```

Responses:

```text
200 OK
404 Not Found
```

## Create Bot

```http
POST /bots
Content-Type: application/json
```

Example request:

```json
{
  "botId": "bot-900",
  "model": "DeliveryBot-Test",
  "currentLocation": {
    "latitude": 33.427,
    "longitude": -111.938
  }
}
```

Responses:

```text
201 Created
400 Bad Request
409 Conflict
```

Published event:

```text
BotCreated
```

## Update Bot

```http
PATCH /bots/{botId}
Content-Type: application/json
```

Example request:

```json
{
  "model": "DeliveryBot-Test-Updated",
  "powerLevel": 88,
  "externalTemperature": 75,
  "internalStorageTemperature": 39,
  "currentLocation": {
    "latitude": 33.428,
    "longitude": -111.937
  }
}
```

Responses:

```text
200 OK
404 Not Found
```

Published event:

```text
BotUpdated
```

## Delete Bot

```http
DELETE /bots/{botId}
```

A bot can only be deleted if it has no active order and no queued orders.

Responses:

```text
204 No Content
404 Not Found
409 Conflict
```

Example conflict response:

```json
{
  "message": "Bot bot-901 cannot be removed because it has active or queued orders.",
  "reason": "BotHasActiveOrQueuedOrders"
}
```

Published event:

```text
BotRemoved
```

## Assign Order

```http
POST /orders/assignments
Content-Type: application/json
```

Example request:

```json
{
  "orderId": "order-001",
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
```

Expected accepted response:

```json
{
  "orderId": "order-001",
  "botId": "bot-001",
  "result": "Accepted",
  "message": "Order accepted and delivery started."
}
```

Expected queued response:

```json
{
  "orderId": "order-002",
  "botId": "bot-001",
  "result": "Queued",
  "message": "Order accepted and queued."
}
```

Expected insufficient stock response:

```json
{
  "orderId": "order-003",
  "botId": "bot-001",
  "result": "Rejected",
  "message": "Insufficient available stock for item water."
}
```

Expected missing bot HTTP response:

```text
404 Not Found
```

Example missing bot body:

```json
{
  "orderId": "order-missing-bot",
  "botId": "bot-999",
  "result": "Rejected",
  "reason": "BotNotFound"
}
```

Possible published events:

```text
RobotOrderAssignmentResponse
RobotStockUpdated
RobotStatusUpdated
```

## Get Recent Events

```http
GET /events/recent
```

Optional query parameter:

```http
GET /events/recent?count=25
```

Returns recent locally stored robot event envelopes.

In Local mode, this is the main event inspection endpoint.

In Azure Event Hub mode, this works if the composite publisher is enabled, which publishes to Azure and stores a recent local copy.
