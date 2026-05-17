# Robot Simulator Responsibility Breakdown

This document summarizes the major system responsibilities for the DeliveryBot robot simulator project.

---

## Responsibility Breakdown

| Responsibility | Owner |
|---|---|
| Create orders | Order Service |
| Select target bot for an order | Order Service |
| Publish order assignment event | Order Service |
| Transport robot-related events | Event Hub |
| Host simulated bot objects | Simulator |
| Create initial bot network | Simulator |
| Add, remove, view, and update bots | Simulator |
| Receive robot-related input events | Simulator |
| Route order assignment to selected bot object | Simulator |
| Handle missing bot routing errors | Simulator |
| Publish bot-generated events | Simulator |
| Maintain individual bot state | Bot object |
| Validate order stock availability | Bot object |
| Reserve stock for accepted orders | Bot object |
| Reject orders with insufficient stock | Bot object |
| Queue accepted orders when already delivering | Bot object |
| Generate stock update after reservation | Bot object |
| Simulate movement to fake GPS destination | Bot object |
| Complete deliveries | Bot object |
| Deduct fulfilled stock after delivery | Bot object |
| Generate telemetry data | Bot object |
| Generate status update events | Bot object |
| Generate order response events | Bot object |
| Generate delivery completion events | Bot object |
| Consume telemetry events | Robot Data Ingestion System |
| Consume stock update events | Robot Data Ingestion System |
| Consume status update events | Robot Data Ingestion System |
| Consume order response and delivery completion events | Order Service and/or Robot Data Ingestion System |

---

## Suggested Event Types

```text
RobotTelemetryUpdated
RobotStatusUpdated
RobotStockUpdated
RobotOrderAssignmentReceived
RobotOrderAssignmentResponse
RobotOrderAccepted
RobotOrderQueued
RobotOrderRejected
RobotDeliveryCompleted
BotCreated
BotUpdated
BotRemoved
```

For a simpler class-project implementation, this can be reduced to:

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

---

## Final Architecture Summary

The simulator should be described as a **.NET-based simulated bot host and event router**. It hosts literal bot objects, exposes or consumes inputs for bot management and order assignment, and routes bot-generated outputs through Event Hub.

The bot objects own the realistic behavior: stock validation, stock reservation, order queues, movement, delivery completion, status changes, and telemetry generation.

External services such as the Order Service and Robot Data Ingestion System interact with the simulator through robot-related events rather than owning the bot behavior themselves.
