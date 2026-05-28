# Readable Bot Network Representation Terraform Module

Creates the Azure resources needed for the bot network read model:

- Cosmos DB account, SQL database, and `bots` container
- Linux Azure Function App host for the Event Hub projection
- Function App storage account
- Consumption App Service plan
- Log Analytics workspace and Application Insights
- Optional Event Hub consumer group for `robot-output`
- Managed identity role assignments for Event Hub read access and Cosmos data writes

The module assumes the Event Hub namespace and `robot-output` Event Hub already exist. This keeps the module ready for integration into the future project-wide IaC root.

## Example

```hcl
module "readable_bot_network_representation" {
  source = "./modules/readable-bot-network-representation"

  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  name_prefix = "deliverybot"
  environment = "dev"

  eventhub_namespace_name     = azurerm_eventhub_namespace.robot_events.name
  robot_output_eventhub_name  = azurerm_eventhub.robot_output.name
  eventhub_consumer_group_name = "readable-bot-network"

  tags = {
    project = "DeliveryBotSystem"
  }
}
```

## Cosmos Document Contract

The `bots` container is partitioned by `/botId`. Function code should also use the same value for the document `id` so each bot has one current read-model document.

Recommended document shape:

```json
{
  "id": "bot-001",
  "botId": "bot-001",
  "status": "Available",
  "isAvailable": true,
  "isRemoved": false,
  "currentLocation": {
    "latitude": 33.4255,
    "longitude": -111.94
  },
  "powerLevel": 99.8,
  "externalTemperature": 72,
  "internalStorageTemperature": 38,
  "activeOrderId": null,
  "queuedOrderCount": 0,
  "inventory": [
    {
      "itemId": "water",
      "itemName": "Water",
      "quantityOnHand": 20,
      "quantityReserved": 1,
      "quantityAvailable": 19
    }
  ],
  "lastTelemetryEventUtc": "2026-05-15T20:15:00Z",
  "lastStatusEventUtc": "2026-05-15T20:15:00Z",
  "lastInventoryEventUtc": "2026-05-15T20:15:00Z",
  "updatedAtUtc": "2026-05-15T20:15:00Z"
}
```

## Function App Settings

The module configures identity-friendly settings for the future Function App code:

- `RobotOutputEventHub__fullyQualifiedNamespace`
- `RobotOutputEventHub__eventHubName`
- `RobotOutputEventHub__consumerGroup`
- `ReadableBotNetwork__CosmosAccountEndpoint`
- `ReadableBotNetwork__CosmosDatabaseName`
- `ReadableBotNetwork__CosmosContainerName`
- `ReadableBotNetwork__CosmosPartitionKey`

Deployment of the Function App code is intentionally out of scope for this module. CI can publish the compiled Function App to the `function_app_name` output.
