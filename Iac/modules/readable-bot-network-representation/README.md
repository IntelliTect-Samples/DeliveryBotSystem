# Readable Bot Network Representation Terraform Module

Creates the Azure resources needed for the bot network read model:

- Cosmos DB account, SQL database, and `bots` container
- Cosmos DB `function-diagnostics` container for rejected or failed event records
- Linux Azure Function App host for the Event Hub projection
- Function App storage account
- Consumption App Service plan
- Log Analytics workspace and Application Insights
- Optional Event Hub consumer group for `robot-output`
- Managed identity role assignments for Event Hub read access and Cosmos data writes

The module assumes the Event Hub namespace and `robot-output` Event Hub already exist. This keeps the module ready for integration into the future project-wide IaC root.

## Integration Requirements

The project-wide Terraform root that calls this module is responsible for:

- Configuring the `azurerm` provider and Terraform backend.
- Passing the shared resource group name and location.
- Passing the existing simulator Event Hub namespace name and `robot-output` Event Hub name.
- Publishing the compiled `ReadBotsFunction` code after the Function App exists.

The Azure subscription must have the `Microsoft.DocumentDB` resource provider registered before Cosmos DB resources can be created:

```powershell
az provider register --namespace Microsoft.DocumentDB
```

The Terraform identity needs permission to create the resources in the target resource group. If the module is allowed to assign access automatically, it also needs permission to create:

- `Azure Event Hubs Data Receiver` for the Function App identity on the `robot-output` Event Hub scope.
- `Cosmos DB Built-in Data Contributor` for the Function App identity on the Cosmos DB account.

If the project-wide Terraform identity cannot create role assignments, set these module variables to `false` and have an owner assign the access outside this module:

```hcl
assign_eventhub_receiver_role       = false
assign_cosmos_data_contributor_role = false
```

The Function App identity still needs both permissions for the deployed Function App to process Event Hub messages and update Cosmos DB.

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

## Project Dev Values

These are the values used for the current class development environment:

```hcl
resource_group_name          = "ewu-deliverybotsystem-rg"
location                     = "westus2"
eventhub_namespace_name      = "DeliverybotSimulator-EVHNS"
robot_output_eventhub_name   = "robot-output"
eventhub_consumer_group_name = "readable-bot-network-dev"
cosmos_database_name         = "bot-network"
cosmos_container_name        = "bots"
```

Use these only when wiring the module into the project-wide IaC for the shared dev environment.

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

The module configures the identity-based settings expected by `ReadBotsFunction`:

- `RobotOutputEventHubName`
- `RobotOutputEventHubConsumerGroup`
- `RobotOutputEventHubIdentity__fullyQualifiedNamespace`
- `RobotOutputEventHubIdentity__credential`
- `ReadableBotNetwork__CosmosAccountEndpoint`
- `ReadableBotNetwork__CosmosDatabaseName`
- `ReadableBotNetwork__BotsContainerName`
- `ReadableBotNetwork__DiagnosticsContainerName`
- `ReadableBotNetwork__CosmosPartitionKey`
- `ReadableBotNetwork__DiagnosticsPartitionKey`
- `AzureWebJobsFeatureFlags`

Deployment of the Function App code is intentionally out of scope for this module. CI can publish the compiled Function App to the `function_app_name` output.

## Useful Outputs

The project-wide IaC or deployment workflow can use these outputs when wiring other components:

- `function_app_name`
- `function_app_principal_id`
- `cosmos_account_name`
- `cosmos_account_endpoint`
- `cosmos_database_name`
- `cosmos_container_name`
- `cosmos_diagnostics_container_name`
- `eventhub_fully_qualified_namespace`
- `robot_output_eventhub_name`
- `eventhub_consumer_group_name`
