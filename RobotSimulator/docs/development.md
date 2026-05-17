# Development Guide

## Prerequisites

- .NET SDK matching the project target framework
- Docker Desktop, for container testing
- Azure CLI, only if testing Azure Event Hub integration
- Git

## Common Commands

Build:

```powershell
dotnet build
```

Run tests:

```powershell
dotnet test
```

Run the API locally:

```powershell
dotnet run --project src/DeliveryBot.RobotSimulator.Api
```

Build Docker image:

```powershell
docker build -f src/DeliveryBot.RobotSimulator.Api/Dockerfile -t deliverybot-robot-simulator .
```

Run Docker image in local mode:

```powershell
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e EventTransport__Mode=Local `
  -e EventTransport__EnableInputConsumer=false `
  deliverybot-robot-simulator
```

## Swagger

Swagger is available in Development mode:

```text
http://localhost:8080/swagger
```

If endpoints work but `/swagger` does not, confirm the app is running with:

```text
ASPNETCORE_ENVIRONMENT=Development
```

Docker example:

```powershell
docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e EventTransport__Mode=Local `
  deliverybot-robot-simulator
```

## Local Testing Flow

1. Start the app.
2. Open Swagger.
3. Call `GET /bots`.
4. Submit an order to `POST /orders/assignments`.
5. Call `GET /bots/{botId}` repeatedly to see status and location changes.
6. Call `GET /events/recent` to see generated events.

Example order:

```json
{
  "orderId": "local-test-order-001",
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

## Configuration Overrides

PowerShell environment variables:

```powershell
$env:Simulator__InitialBotCount="5"
$env:Simulator__BotIdPrefix="testbot"
$env:Simulation__TelemetryIntervalSeconds="2"
$env:EventTransport__Mode="Local"
```

Clear variables:

```powershell
Remove-Item Env:Simulator__InitialBotCount -ErrorAction SilentlyContinue
Remove-Item Env:Simulator__BotIdPrefix -ErrorAction SilentlyContinue
Remove-Item Env:Simulation__TelemetryIntervalSeconds -ErrorAction SilentlyContinue
Remove-Item Env:EventTransport__Mode -ErrorAction SilentlyContinue
```

## Test Coverage

The test project covers:

```text
Stock reservation
Stock fulfillment
Insufficient stock protection
Order acceptance
Order rejection
Order queueing
Delivery movement
Delivery completion
Stock deduction after delivery
Queued order handoff
Telemetry generation
Bot creation
Bot update
Bot removal
Delete conflict for active bots
Config-driven fleet startup
```

## Troubleshooting

### Docker cannot connect to Docker Desktop

Error example:

```text
open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified
```

Fix:

- Start Docker Desktop.
- Ensure Linux containers are enabled.
- Run `docker version` and confirm both Client and Server sections appear.

### Dockerfile cannot find `.sln`

If the repository uses `.slnx`, avoid restoring the whole solution in Docker. Restore and publish the API `.csproj` directly.

Recommended Docker approach:

```dockerfile
RUN dotnet restore ./src/DeliveryBot.RobotSimulator.Api/DeliveryBot.RobotSimulator.Api.csproj
RUN dotnet publish ./src/DeliveryBot.RobotSimulator.Api/DeliveryBot.RobotSimulator.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore
```

### Swagger unavailable in Docker

Run with:

```powershell
-e ASPNETCORE_ENVIRONMENT=Development
```

### Missing Microsoft.Extensions packages

Plain class libraries may need explicit package references for extension APIs.

Packages added during development included:

```text
Microsoft.Extensions.Logging.Abstractions
Microsoft.Extensions.Configuration.Abstractions
Microsoft.Extensions.Configuration.Binder
Azure.Messaging.EventHubs
Azure.Messaging.EventHubs.Producer
```

### Event Hub mode starts but `/events/recent` is empty

Ensure the composite publisher is registered for Azure mode. It should publish both to Azure Event Hub and to the recent local event store.

### Event Hub input consumes its own events

Same-hub mode should filter:

```text
source == robot-simulator
```

and only process:

```text
eventType == RobotOrderAssignment
```
