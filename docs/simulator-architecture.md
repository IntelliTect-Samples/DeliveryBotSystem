# Architecture

## Overview

The DeliveryBot Robot Simulator is a .NET containerized simulator that hosts simulated vending delivery bot objects. It is designed to test robot-related workflows without physical devices.

The simulator owns bot behavior. Event Hub is treated as a transport mechanism, not as the owner of robot logic.

## High-Level Responsibilities

```text
Order Service
  - Creates orders
  - Selects target bot
  - Publishes order assignment events

Event Hub
  - Transports robot-related events

Simulator
  - Hosts simulated bot objects
  - Creates initial bot network
  - Manages bot add/update/remove/view workflows
  - Receives robot-related input events
  - Routes order assignments to target bots
  - Publishes bot-generated events

Bot Object
  - Maintains individual bot state
  - Validates stock availability
  - Reserves stock
  - Rejects insufficient-stock orders
  - Queues accepted orders while delivering
  - Simulates movement
  - Completes deliveries
  - Deducts fulfilled stock
  - Generates telemetry, stock, status, response, and completion events

Robot Data Ingestion System
  - Consumes telemetry, stock, status, order response, and completion events
```

## Runtime Modes

### Local Mode

```text
HTTP order assignment
      |
      v
Simulator
      |
      v
Recent in-memory event store
```

Use local mode for rapid development and unit/integration testing without Azure resources.

### Docker Local Mode

```text
Host machine
   |
   | http://localhost:8080
   v
Docker container
   |
   v
Simulator running in Local event mode
```

Use Docker local mode to confirm the simulator runs as a deployable container.

### Azure Event Hub Output Mode

```text
HTTP order assignment
      |
      v
Simulator
      |
      v
Azure Event Hub output
```

Use this mode to validate that simulator-generated events can be published to Azure Event Hub.

### Azure Event Hub Input and Output Mode

```text
Order Service or test sender
      |
      v
Azure Event Hub input
      |
      v
Simulator input consumer
      |
      v
Bot object
      |
      v
Azure Event Hub output
```

Use this mode for the full event-driven flow.

## Component Diagram

```text
+---------------------------------------------+
| DeliveryBot.RobotSimulator.Api              |
|---------------------------------------------|
| Minimal API endpoints                       |
| SimulationWorker                            |
| EventHubOrderAssignmentWorker               |
| OrderAssignmentHandler                      |
+----------------------+----------------------+
                       |
                       v
+---------------------------------------------+
| DeliveryBot.RobotSimulator.Core             |
|---------------------------------------------|
| BotFleet                                    |
| SimulatedBot                                |
| Stock models                                |
| Order models                                |
| Simulation options and GeoMath              |
| Telemetry models                            |
+----------------------+----------------------+
                       |
                       v
+---------------------------------------------+
| DeliveryBot.RobotSimulator.Events           |
|---------------------------------------------|
| RobotEventEnvelope                          |
| RobotEventTypes                             |
+----------------------+----------------------+
                       |
                       v
+---------------------------------------------+
| DeliveryBot.RobotSimulator.Infrastructure   |
|---------------------------------------------|
| IRobotEventPublisher                        |
| LocalRobotEventPublisher                    |
| AzureRobotEventPublisher                    |
| CompositeRobotEventPublisher                |
| RecentRobotEventStore                       |
| IRobotEventConsumer                         |
| AzureRobotEventConsumer                     |
| NoOpRobotEventConsumer                      |
| Event transport configuration               |
+---------------------------------------------+
```

## Bot Lifecycle

```text
Available
   |
   | accepted order
   v
OnDelivery
   |
   | delivery complete, no queued orders
   v
Available
```

With queued orders:

```text
OnDelivery order-001
   |
   | order-002 accepted while busy
   v
order-002 queued
   |
   | order-001 completes
   v
OnDelivery order-002
```

The bot should not publish an incorrect `Available` status between queued deliveries.

## Stock Lifecycle

```text
QuantityOnHand
QuantityReserved
QuantityAvailable = QuantityOnHand - QuantityReserved
```

When an order is accepted or queued:

```text
Validate requested items against QuantityAvailable
Reserve stock immediately
Publish RobotStockUpdated with reason StockReserved
```

When delivery completes:

```text
Fulfill reserved stock
Deduct QuantityOnHand
Reduce QuantityReserved
Publish RobotStockUpdated with reason StockFulfilled
Publish RobotDeliveryCompleted
```

## Event Flow: HTTP Assignment

```text
POST /orders/assignments
        |
        v
OrderAssignmentHandler
        |
        v
BotFleet.TryGetBot
        |
        +--> missing bot -> RobotOrderAssignmentResponse rejected
        |
        v
SimulatedBot.AssignOrder
        |
        +--> Accepted -> response, stock update, status update
        +--> Queued   -> response, stock update
        +--> Rejected -> response
```

## Event Flow: Event Hub Assignment

```text
AzureRobotEventConsumer
        |
        v
EventHubOrderAssignmentWorker
        |
        | filters:
        | - ignore source == robot-simulator
        | - ignore eventType != RobotOrderAssignment
        v
OrderAssignmentHandler
        |
        v
Same bot routing logic as HTTP endpoint
```

## Same Hub vs Split Hub

The simulator does not assume whether order input and telemetry/status output share the same Event Hub.

### Split Hub

```text
robot-input  -> Simulator -> robot-output
```

### Same Hub

```text
robot-events -> Simulator -> robot-events
```

Same-hub mode is safe because the input worker ignores events emitted by the simulator itself and ignores all event types except `RobotOrderAssignment`.

## Deployment Shape

The intended deployment shape is a continuously running .NET container, such as Azure Container Apps.

Recommended Azure resources:

```text
Azure Container App
Azure Container Registry
Azure Event Hubs Namespace
Input Event Hub, optional
Output Event Hub
Log Analytics Workspace
Managed Identity or Key Vault-backed secrets, future improvement
```

## Design Decisions

### Modular Monolith

The simulator is one deployable app with separate internal projects. This keeps the implementation simple while preserving clean boundaries.

### In-Memory Bot State

Bot state is currently in memory. This is appropriate for simulation and class-project testing. Persistence can be added later if needed.

### Event Abstraction

Bot behavior publishes through `IRobotEventPublisher`. This makes it easy to switch between local event storage and Azure Event Hub.

### Optional Input Consumer

Event Hub input consumption is controlled by `EventTransport:EnableInputConsumer`. This allows publishing-only tests without requiring an input hub.
