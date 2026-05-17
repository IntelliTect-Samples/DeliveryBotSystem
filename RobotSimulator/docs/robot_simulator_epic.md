# Epic: Robot Simulator

**As a development team,**  
**we want a .NET-based robot simulator that hosts simulated vending delivery bot objects, manages bot device data, routes robot-related events, processes bot-specific order assignments, simulates delivery behavior, and publishes bot-generated data,**  
**so that the team can test Azure-based order fulfillment and robot data workflows without physical robots.**

The simulated bots should be treated as mobile vending machines that travel to order destinations, complete deliveries, update stock, and wait for new work.

---

## 1. Simulated Bot Fleet

**As a developer,**  
**I want the simulator to create and host simulated bot objects,**  
**so that each bot can independently manage its own state, orders, stock, telemetry, and delivery behavior.**

### Acceptance Criteria

**Given** the simulator is configured with an initial bot count  
**When** the simulator starts  
**Then** it creates that number of simulated bot objects.

**Given** each bot object is created  
**When** its state is initialized  
**Then** it has a unique bot ID, model metadata, current location, power level, temperature values, stock inventory, current status, and order queue.

**Given** multiple bots are active  
**When** the simulator is running  
**Then** each bot independently maintains its own location, stock, reserved stock, power level, active order, queued orders, and status.

---

## 2. Bot Device Management

**As someone responsible for managing bots,**  
**I want to add, remove, view, and update bots through simulator endpoints or events,**  
**so that the bot network can be inspected, maintained, adjusted, and expanded.**

### Acceptance Criteria

**Given** a bot manager wants to add a new bot  
**When** a valid bot creation request is received  
**Then** the simulator creates a new bot object and adds it to the bot network.

**Given** a bot manager wants to view bot information  
**When** all bots or a specific bot are requested  
**Then** the simulator returns current bot information including status, location, stock, reserved stock, power level, active order, and queued order count.

**Given** a bot manager wants to update a bot  
**When** valid editable properties are provided  
**Then** the simulator updates the bot object and future simulation behavior uses the updated values.

**Given** a bot manager wants to remove a bot  
**When** the bot has no active or queued orders  
**Then** the simulator removes or deactivates the bot.

**Given** a bot has an active or queued order  
**When** a remove request is received  
**Then** the simulator rejects the removal request with a conflict response.

---

## 3. Robot Event Routing

**As an external system,**  
**I want robot-related events to flow through Event Hub,**  
**so that the simulator, Order Service, Robot Data Ingestion System, and other services can communicate through a shared event stream.**

### Acceptance Criteria

**Given** the Order Service creates an order assignment  
**When** the event is published  
**Then** it is sent through Event Hub and includes the target bot ID.

**Given** the simulator receives a robot-related input event  
**When** the event targets a specific bot  
**Then** the simulator routes the event to the correct bot object.

**Given** a bot object generates telemetry, status updates, stock updates, order responses, or delivery completion events  
**When** the simulator receives those outputs from the bot  
**Then** the simulator publishes them to Event Hub.

**Given** robot-related events are published  
**When** the Robot Data Ingestion System consumes them  
**Then** it can process telemetry, stock updates, status updates, order responses, and delivery completion events.

---

## 4. Order Assignment Routing

**As the Order Service,**  
**I want to submit order assignment events for specific bots,**  
**so that the simulator can route each order to the selected bot object for validation and delivery processing.**

### Acceptance Criteria

**Given** the Order Service has selected a bot for an order  
**When** it publishes an order assignment event  
**Then** the event includes bot ID, order ID, requested items, and fake GPS destination.

**Given** the simulator receives an order assignment event  
**When** the specified bot exists  
**Then** the simulator routes the order assignment to that bot object.

**Given** the simulator receives an order assignment event  
**When** the specified bot does not exist  
**Then** the simulator publishes a `BotNotFound` order response event.

**Given** a bot object receives an order assignment  
**When** the bot validates the order  
**Then** the bot determines whether it has enough available, unreserved stock to fulfill the order.

**Given** the bot returns an order response  
**When** the simulator receives that response  
**Then** the simulator routes the response back through Event Hub for the Order Service and other consumers.

---

## 5. Stock Validation and Reservation

**As the Order Service,**  
**I want each bot to validate and reserve stock when accepting an order,**  
**so that active and queued orders do not overcommit the bot’s inventory.**

### Acceptance Criteria

**Given** a bot receives an order assignment  
**When** it checks stock availability  
**Then** it validates the order against available stock, not total stock.

**Given** the bot has enough available stock  
**When** the order is accepted  
**Then** the bot immediately reserves the requested item quantities.

**Given** the bot does not have enough available stock  
**When** the order is validated  
**Then** the bot rejects the order with an insufficient-stock response.

**Given** stock is reserved for an accepted order  
**When** the reservation is created  
**Then** the bot generates a stock update event.

**Given** a queued order has reserved stock  
**When** earlier deliveries are completed  
**Then** the queued order keeps its reservation until it is delivered or cancelled.

**Given** an order is completed  
**When** delivery is finalized  
**Then** the reserved stock is converted into fulfilled stock and deducted from the bot’s onboard inventory.

---

## 6. Delivery Simulation

**As the Order Service,**  
**I want an assigned bot to simulate travel to a fake GPS destination and complete the order,**  
**so that the delivery lifecycle can be tested without a physical robot.**

### Acceptance Criteria

**Given** a bot accepts an order and is available  
**When** delivery begins  
**Then** the bot status changes to `OnDelivery`.

**Given** a bot accepts an order while already delivering  
**When** the order is accepted  
**Then** the bot queues the order internally.

**Given** a bot is on delivery  
**When** telemetry is generated  
**Then** the bot’s GPS location moves in a straight-line path toward the fake destination.

**Given** the bot reaches the destination  
**When** the delivery simulation completes  
**Then** the bot marks the order as complete.

**Given** the completed order had reserved stock  
**When** the order is finalized  
**Then** the bot deducts the delivered quantities from onboard stock.

**Given** the bot has queued orders  
**When** the current order is completed  
**Then** the bot begins the next queued order and remains `OnDelivery`.

**Given** the bot has no queued orders  
**When** the current order is completed  
**Then** the bot status changes to `Available`.

---

## 7. Bot Telemetry Generation

**As the Robot Data Ingestion System,**  
**I want simulated bot telemetry events to be published through Event Hub,**  
**so that robot data can be processed, stored, and visualized by external services.**

### Acceptance Criteria

**Given** the simulator is running  
**When** each bot object reaches the telemetry interval  
**Then** the bot object generates telemetry data.

**Given** a bot generates telemetry  
**When** the simulator receives the telemetry output  
**Then** the simulator publishes the telemetry event through Event Hub.

**Given** a telemetry event is published  
**When** the Robot Data Ingestion System receives it  
**Then** the event includes bot ID, timestamp, event ID, schema version, status, GPS location, power level, external temperature, internal storage temperature, stock data, reserved stock data, and simulated-data indicator.

**Given** a bot is idle  
**When** telemetry is generated  
**Then** the telemetry reflects the bot’s current idle state.

**Given** a bot is on delivery  
**When** telemetry is generated  
**Then** the telemetry reflects simulated movement toward the destination.

---

## 8. Bot Status, Stock, and Order Update Events

**As the Robot Data Ingestion System,**  
**I want robot-related update events to be published when bot state changes,**  
**so that external systems can track bot status, stock availability, order responses, and delivery completion.**

### Acceptance Criteria

**Given** a bot status changes  
**When** the change occurs  
**Then** the bot generates a status update event.

**Given** a bot reserves stock  
**When** an order is accepted or queued  
**Then** the bot generates a stock update event.

**Given** a bot rejects an order  
**When** validation fails  
**Then** the bot generates an order rejected event with a reason.

**Given** a bot completes an order  
**When** delivery is finalized  
**Then** the bot generates an order completion event.

**Given** the bot completes an order and immediately starts a queued order  
**When** status events are generated  
**Then** the bot should not incorrectly report itself as `Available`.

---

## 9. Event Schema Definition

**As a development team member,**  
**I want robot-related events to follow documented schemas,**  
**so that the simulator, Order Service, Robot Data Ingestion System, and other services can exchange data consistently.**

### Acceptance Criteria

**Given** an event is published by or to the simulator  
**When** the event is created  
**Then** it includes an event ID, event type, schema version, timestamp, and bot ID where applicable.

**Given** a telemetry event is published  
**When** the event is consumed  
**Then** it follows the documented telemetry schema.

**Given** an order assignment event is received  
**When** the simulator processes it  
**Then** it follows the documented order assignment schema.

**Given** an order response event is published  
**When** the Order Service consumes it  
**Then** it identifies whether the order was accepted, queued, rejected, or completed.

**Given** a stock update event is published  
**When** the event is consumed  
**Then** stock includes item ID, item name, quantity on hand, quantity reserved, and quantity available.

---

## 10. Simulator Deployment and Configuration

**As a DevOps team member,**  
**I want the simulator to be deployable as a continuously running .NET service in Azure,**  
**so that it can integrate with Azure resources and support repeatable project demonstrations.**

### Acceptance Criteria

**Given** the simulator is deployed  
**When** the application starts  
**Then** it initializes the configured bot network and begins simulation.

**Given** the simulator is running  
**When** bot objects generate events  
**Then** the simulator routes those events to the configured Azure Event Hub.

**Given** environment-specific configuration is needed  
**When** the simulator is deployed  
**Then** settings such as bot count, Event Hub connection information, and secrets are managed outside source code.

**Given** code changes are made  
**When** the CI/CD pipeline runs  
**Then** the simulator can be built, tested, and deployed through the team’s deployment process.

**Given** tests are executed  
**When** the test suite runs  
**Then** key behaviors such as order routing, stock validation, stock reservation, queue handling, telemetry generation, and delivery completion are verified.
