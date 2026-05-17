# Azure Event Hub Testing Guide

This guide describes how to create temporary Azure Event Hub resources to validate simulator publishing and input consumption.

Use a temporary resource group so cleanup is simple.

## Prerequisites

Sign in:

```powershell
az login
```

Confirm subscription:

```powershell
az account show
```

If needed:

```powershell
az account list --output table
az account set --subscription "<subscription-id-or-name>"
```

## Create Temporary Resources

```powershell
$location = "eastus"
$rgName = "rg-deliverybot-simulator-test"
$namespaceName = "evhns-deliverybot-sim-$((Get-Random -Maximum 99999))"
$outputHubName = "robot-output"
$inputHubName = "robot-input"
$combinedHubName = "robot-events"
```

Create the resource group:

```powershell
az group create `
  --name $rgName `
  --location $location
```

Create Event Hubs namespace:

```powershell
az eventhubs namespace create `
  --resource-group $rgName `
  --name $namespaceName `
  --location $location `
  --sku Standard
```

## Option A: Output-Only Test

Create output hub:

```powershell
az eventhubs eventhub create `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name $outputHubName `
  --partition-count 2 `
  --message-retention 1
```

Create send policy:

```powershell
az eventhubs namespace authorization-rule create `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name simulator-send `
  --rights Send
```

Get connection string:

```powershell
$connectionString = az eventhubs namespace authorization-rule keys list `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name simulator-send `
  --query primaryConnectionString `
  --output tsv
```

Run locally:

```powershell
$env:EventTransport__Mode="AzureEventHub"
$env:EventTransport__ConnectionString=$connectionString
$env:EventTransport__OutputEventHubName=$outputHubName
$env:EventTransport__EnableInputConsumer="false"

dotnet run --project src/DeliveryBot.RobotSimulator.Api
```

Run in Docker:

```powershell
docker build -f src/DeliveryBot.RobotSimulator.Api/Dockerfile -t deliverybot-robot-simulator .

docker run --rm -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Development `
  -e EventTransport__Mode=AzureEventHub `
  -e EventTransport__ConnectionString="$connectionString" `
  -e EventTransport__OutputEventHubName="$outputHubName" `
  -e EventTransport__EnableInputConsumer=false `
  deliverybot-robot-simulator
```

Then submit an order through Swagger:

```text
http://localhost:8080/swagger
```

Check events in Azure Portal Data Explorer:

```text
Azure Portal
→ Event Hubs Namespace
→ Event Hubs
→ robot-output
→ Data Explorer
```

## Option B: Split Input and Output Hubs

Create input and output hubs:

```powershell
az eventhubs eventhub create `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name $inputHubName `
  --partition-count 2 `
  --message-retention 1

az eventhubs eventhub create `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name $outputHubName `
  --partition-count 2 `
  --message-retention 1
```

Create listen/send policy:

```powershell
az eventhubs namespace authorization-rule create `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name simulator-listen-send `
  --rights Listen Send
```

Get connection string:

```powershell
$connectionString = az eventhubs namespace authorization-rule keys list `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name simulator-listen-send `
  --query primaryConnectionString `
  --output tsv
```

Run simulator:

```powershell
$env:EventTransport__Mode="AzureEventHub"
$env:EventTransport__ConnectionString=$connectionString
$env:EventTransport__InputEventHubName=$inputHubName
$env:EventTransport__OutputEventHubName=$outputHubName
$env:EventTransport__ConsumerGroup='$Default'
$env:EventTransport__EnableInputConsumer="true"

dotnet run --project src/DeliveryBot.RobotSimulator.Api
```

## Option C: Same Hub for Input and Output

Create combined hub:

```powershell
az eventhubs eventhub create `
  --resource-group $rgName `
  --namespace-name $namespaceName `
  --name $combinedHubName `
  --partition-count 2 `
  --message-retention 1
```

Run simulator:

```powershell
$env:EventTransport__Mode="AzureEventHub"
$env:EventTransport__ConnectionString=$connectionString
$env:EventTransport__InputEventHubName=$combinedHubName
$env:EventTransport__OutputEventHubName=$combinedHubName
$env:EventTransport__ConsumerGroup='$Default'
$env:EventTransport__EnableInputConsumer="true"

dotnet run --project src/DeliveryBot.RobotSimulator.Api
```

Same-hub mode is safe because the input worker ignores simulator-produced events and ignores non-order-assignment events.

## Test Input Event Payload

Use Azure Portal Data Explorer or a sender tool to publish this envelope into the input hub:

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

Expected output events:

```text
RobotOrderAssignmentResponse
RobotStockUpdated
RobotStatusUpdated
RobotTelemetryUpdated
RobotDeliveryCompleted
```

## Clear Local Environment Variables

```powershell
Remove-Item Env:EventTransport__Mode -ErrorAction SilentlyContinue
Remove-Item Env:EventTransport__ConnectionString -ErrorAction SilentlyContinue
Remove-Item Env:EventTransport__InputEventHubName -ErrorAction SilentlyContinue
Remove-Item Env:EventTransport__OutputEventHubName -ErrorAction SilentlyContinue
Remove-Item Env:EventTransport__ConsumerGroup -ErrorAction SilentlyContinue
Remove-Item Env:EventTransport__EnableInputConsumer -ErrorAction SilentlyContinue
```

## Delete Temporary Resources

Delete the whole resource group:

```powershell
az group delete `
  --name $rgName `
  --yes
```

Or return immediately while Azure deletes in the background:

```powershell
az group delete `
  --name $rgName `
  --yes `
  --no-wait
```

Confirm deletion:

```powershell
az group exists --name $rgName
```

Expected after deletion completes:

```text
false
```
