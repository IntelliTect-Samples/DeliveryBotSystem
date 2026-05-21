# DeliveryBot Robot Simulator

The DeliveryBot Robot Simulator is a .NET-based simulated robot host for testing vending delivery bot workflows without physical robots. It hosts simulated bot objects, manages bot state, processes order assignments, simulates delivery movement, generates telemetry, and publishes robot-related events.

The simulator can run locally for development and testing, inside Docker for container validation, or against Azure Event Hub for integration testing.

## Current Capabilities

- Config-driven simulator startup
- In-memory simulated bot fleet
- Bot inspection, creation, update, and removal endpoints
- Delete conflict when a bot has active or queued orders
- Order assignment endpoint for local/manual testing
- Stock validation against available stock
- Stock reservation when orders are accepted or queued
- Order queueing while a bot is already delivering
- Background simulation loop
- Straight-line GPS-style delivery movement
- Telemetry generation
- Delivery completion
- Stock deduction after fulfillment
- Queued delivery handoff without incorrectly reporting the bot as available
- Local event publishing and recent event inspection
- Azure Event Hub output publishing
- Optional Azure Event Hub input consumer
- Same-hub and split-hub Event Hub configuration support
- Docker support
- Core automated tests

## Project Structure

```text
src/
  DeliveryBot.RobotSimulator.Api/
    ASP.NET Core host, HTTP endpoints, background workers

  DeliveryBot.RobotSimulator.Core/
    Bot domain model, stock logic, orders, telemetry, simulation behavior

  DeliveryBot.RobotSimulator.Events/
    Event envelope and event type definitions

  DeliveryBot.RobotSimulator.Infrastructure/
    Local event publisher, Azure Event Hub publisher/consumer, configuration wiring

tests/
  DeliveryBot.RobotSimulator.Tests/
    Unit tests for stock, orders, simulation, and bot management
```

## Run Locally

From the repository root:

```powershell
dotnet build
dotnet test
dotnet run --project src/DeliveryBot.RobotSimulator.Api
```

When running in Development mode, Swagger is available at the URL shown in the console, usually one of:

```text
http://localhost:<port>/swagger
https://localhost:<port>/swagger
```

Useful endpoints:

```http
GET    /health
GET    /bots
GET    /bots/{botId}
POST   /bots
PATCH  /bots/{botId}
DELETE /bots/{botId}
POST   /orders/assignments
GET    /events/recent
```

## Run with Docker

Build the container image:

```powershell
docker build -f src/DeliveryBot.RobotSimulator.Api/Dockerfile -t deliverybot-robot-simulator .
```

Run in local event mode:

```powershell
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e EventTransport__Mode=Local `
  -e EventTransport__EnableInputConsumer=false `
  deliverybot-robot-simulator
```

Open:

```text
http://localhost:8080/swagger
```

Health check:

```text
http://localhost:8080/health
```

## Configuration

The simulator is configured through `appsettings.json` and environment variables.

Example:

```json
{
  "Simulator": {
    "InitialBotCount": 3,
    "BotIdPrefix": "bot",
    "DefaultBotModel": "DeliveryBot-V1",
    "DefaultLatitude": 33.4255,
    "DefaultLongitude": -111.94
  },
  "Simulation": {
    "TickIntervalSeconds": 1,
    "TelemetryIntervalSeconds": 5,
    "DeliverySpeedMetersPerSecond": 8,
    "DestinationArrivalThresholdMeters": 5
  },
  "EventTransport": {
    "Mode": "Local",
    "InputEventHubName": "",
    "OutputEventHubName": "",
    "ConsumerGroup": "$Default",
    "EnableInputConsumer": false
  }
}
```

Environment variable examples:

```powershell
$env:Simulator__InitialBotCount="5"
$env:Simulator__BotIdPrefix="testbot"
$env:Simulation__TelemetryIntervalSeconds="2"
$env:EventTransport__Mode="Local"
```

Clear temporary variables:

```powershell
Remove-Item Env:Simulator__InitialBotCount -ErrorAction SilentlyContinue
Remove-Item Env:Simulator__BotIdPrefix -ErrorAction SilentlyContinue
Remove-Item Env:Simulation__TelemetryIntervalSeconds -ErrorAction SilentlyContinue
Remove-Item Env:EventTransport__Mode -ErrorAction SilentlyContinue
```

## Event Transport Modes

### Local

```json
"EventTransport": {
  "Mode": "Local",
  "EnableInputConsumer": false
}
```

Local mode stores events in memory and exposes them through:

```http
GET /events/recent
```

### Azure Event Hub Output Only

```json
"EventTransport": {
  "Mode": "AzureEventHub",
  "ConnectionString": "<do-not-commit>",
  "OutputEventHubName": "robot-output",
  "EnableInputConsumer": false
}
```

### Azure Event Hub Input and Output

```json
"EventTransport": {
  "Mode": "AzureEventHub",
  "ConnectionString": "<do-not-commit>",
  "InputEventHubName": "robot-input",
  "OutputEventHubName": "robot-output",
  "ConsumerGroup": "$Default",
  "EnableInputConsumer": true
}
```

## Same Hub vs Split Hub

The simulator supports both designs.

Separate hubs:

```text
Order Service -> robot-input -> Simulator -> robot-output -> Consumers
```

Same hub:

```text
Order Service -> robot-events -> Simulator -> robot-events -> Consumers
```

Same-hub mode is protected by filtering rules:

- Ignore events where `source == "robot-simulator"`
- Ignore events where `eventType != "RobotOrderAssignment"`

## Known Limitations

- Bot state is in memory and is reset when the app restarts.
- Event Hub input consumer currently uses a simple consumer client for testing and does not checkpoint with Blob Storage.
- Event payload `data` values are currently flexible objects rather than fully typed event records for every event type.
- There is no authentication on management endpoints yet.
- Swagger is currently enabled only in Development mode unless changed in `Program.cs`.

## Recommended Next Improvements

- Add a documented test sender for Event Hub input events.
- Add typed event data records for all event types.
- Add Event Hub consumer checkpointing for longer-running Azure deployments.
- Add CI build/test workflow.
- Add optional endpoint authentication if the simulator is exposed outside a trusted environment.
