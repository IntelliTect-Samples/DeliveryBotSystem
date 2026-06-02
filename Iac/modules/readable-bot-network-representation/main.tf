locals {
  eventhub_resource_group_name = coalesce(var.eventhub_resource_group_name, var.resource_group_name)
  prefix_slug                  = trim(replace(lower(var.name_prefix), "/[^a-z0-9-]/", "-"), "-")
  compact_prefix               = substr(replace(lower("${var.name_prefix}${var.environment}"), "/[^a-z0-9]/", ""), 0, 14)
  suffix                       = random_string.suffix.result

  cosmos_account_name             = coalesce(var.cosmos_account_name, substr("${local.prefix_slug}-${var.environment}-rbnr-${local.suffix}", 0, 44))
  function_app_name               = coalesce(var.function_app_name, substr("${local.prefix_slug}-${var.environment}-rbnr-func-${local.suffix}", 0, 60))
  service_plan_name               = coalesce(var.service_plan_name, substr("${local.prefix_slug}-${var.environment}-rbnr-plan-${local.suffix}", 0, 60))
  storage_account_name            = coalesce(var.storage_account_name, substr("${local.compact_prefix}rbnr${local.suffix}", 0, 24))
  log_analytics_workspace_name    = coalesce(var.log_analytics_workspace_name, substr("${local.prefix_slug}-${var.environment}-rbnr-law-${local.suffix}", 0, 63))
  application_insights_name       = coalesce(var.application_insights_name, substr("${local.prefix_slug}-${var.environment}-rbnr-ai-${local.suffix}", 0, 255))
  eventhub_fully_qualified_domain = "${data.azurerm_eventhub_namespace.robot_events.name}.servicebus.windows.net"

  common_tags = merge(
    {
      workload    = "readable-bot-network-representation"
      environment = var.environment
      managed_by  = "terraform"
    },
    var.tags
  )
}

resource "random_string" "suffix" {
  length  = 6
  lower   = true
  numeric = true
  special = false
  upper   = false
}

data "azurerm_eventhub_namespace" "robot_events" {
  name                = var.eventhub_namespace_name
  resource_group_name = local.eventhub_resource_group_name
}

data "azurerm_eventhub" "robot_output" {
  name                = var.robot_output_eventhub_name
  namespace_name      = data.azurerm_eventhub_namespace.robot_events.name
  resource_group_name = local.eventhub_resource_group_name
}

resource "azurerm_eventhub_consumer_group" "read_model" {
  count = var.create_eventhub_consumer_group ? 1 : 0

  name                = var.eventhub_consumer_group_name
  namespace_name      = data.azurerm_eventhub_namespace.robot_events.name
  eventhub_name       = data.azurerm_eventhub.robot_output.name
  resource_group_name = local.eventhub_resource_group_name
  user_metadata       = "Readable Bot Network Representation projection"
}

resource "azurerm_cosmosdb_account" "read_model" {
  name                = local.cosmos_account_name
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"
  free_tier_enabled   = var.cosmos_free_tier_enabled

  consistency_policy {
    consistency_level = var.cosmos_consistency_level
  }

  geo_location {
    location          = var.location
    failover_priority = 0
  }

  dynamic "capabilities" {
    for_each = var.cosmos_enable_serverless ? [1] : []

    content {
      name = "EnableServerless"
    }
  }

  tags = local.common_tags
}

resource "azurerm_cosmosdb_sql_database" "read_model" {
  name                = var.cosmos_database_name
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.read_model.name
  throughput          = var.cosmos_enable_serverless ? null : var.cosmos_database_throughput
}

resource "azurerm_cosmosdb_sql_container" "bots" {
  name                = var.cosmos_container_name
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.read_model.name
  database_name       = azurerm_cosmosdb_sql_database.read_model.name

  partition_key_paths   = var.cosmos_partition_key_paths
  partition_key_version = 2
}

resource "azurerm_cosmosdb_sql_container" "diagnostics" {
  name                = var.cosmos_diagnostics_container_name
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.read_model.name
  database_name       = azurerm_cosmosdb_sql_database.read_model.name

  partition_key_paths = [var.cosmos_diagnostics_partition_key_path]
}

resource "azurerm_storage_account" "function" {
  name                            = local.storage_account_name
  location                        = var.location
  resource_group_name             = var.resource_group_name
  account_tier                    = "Standard"
  account_replication_type        = var.storage_account_replication_type
  min_tls_version                 = "TLS1_2"
  allow_nested_items_to_be_public = false

  tags = local.common_tags
}

resource "azurerm_log_analytics_workspace" "read_model" {
  name                = local.log_analytics_workspace_name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = var.log_retention_in_days

  tags = local.common_tags
}

resource "azurerm_application_insights" "read_model" {
  name                = local.application_insights_name
  location            = var.location
  resource_group_name = var.resource_group_name
  workspace_id        = azurerm_log_analytics_workspace.read_model.id
  application_type    = "web"

  tags = local.common_tags
}

resource "azurerm_service_plan" "function" {
  name                = local.service_plan_name
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.service_plan_sku_name

  tags = local.common_tags
}

resource "azurerm_linux_function_app" "read_model" {
  name                        = local.function_app_name
  location                    = var.location
  resource_group_name         = var.resource_group_name
  service_plan_id             = azurerm_service_plan.function.id
  storage_account_name        = azurerm_storage_account.function.name
  storage_account_access_key  = azurerm_storage_account.function.primary_access_key
  functions_extension_version = var.functions_extension_version
  https_only                  = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                              = var.service_plan_sku_name == "Y1" ? null : true
    application_insights_connection_string = azurerm_application_insights.read_model.connection_string
    ftps_state                             = "Disabled"
    minimum_tls_version                    = "1.2"

    application_stack {
      dotnet_version              = var.function_dotnet_version
      use_dotnet_isolated_runtime = true
    }
  }

  app_settings = merge(
    {
      FUNCTIONS_WORKER_RUNTIME = var.functions_worker_runtime
      AzureWebJobsFeatureFlags = "EnableWorkerIndexing"

      ReadableBotNetwork__CosmosAccountEndpoint            = azurerm_cosmosdb_account.read_model.endpoint
      ReadableBotNetwork__CosmosDatabaseName               = azurerm_cosmosdb_sql_database.read_model.name
      ReadableBotNetwork__BotsContainerName                = azurerm_cosmosdb_sql_container.bots.name
      ReadableBotNetwork__DiagnosticsContainerName         = azurerm_cosmosdb_sql_container.diagnostics.name
      ReadableBotNetwork__CosmosPartitionKey               = var.cosmos_partition_key_paths[0]
      ReadableBotNetwork__DiagnosticsPartitionKey          = var.cosmos_diagnostics_partition_key_path
      RobotOutputEventHubName                              = data.azurerm_eventhub.robot_output.name
      RobotOutputEventHubConsumerGroup                     = var.eventhub_consumer_group_name
      RobotOutputEventHubIdentity__fullyQualifiedNamespace = local.eventhub_fully_qualified_domain
      RobotOutputEventHubIdentity__credential              = "managedidentity"
    },
    var.additional_app_settings
  )

  tags = local.common_tags

  lifecycle {
    ignore_changes = [
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
      tags["hidden-link: /app-insights-resource-id"]
    ]
  }
}

resource "azurerm_role_assignment" "function_eventhub_receiver" {
  count = var.assign_eventhub_receiver_role ? 1 : 0

  scope                = data.azurerm_eventhub.robot_output.id
  role_definition_name = "Azure Event Hubs Data Receiver"
  principal_id         = azurerm_linux_function_app.read_model.identity[0].principal_id
}

resource "azurerm_cosmosdb_sql_role_assignment" "function_data_contributor" {
  count = var.assign_cosmos_data_contributor_role ? 1 : 0

  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.read_model.name
  role_definition_id  = "${azurerm_cosmosdb_account.read_model.id}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002"
  principal_id        = azurerm_linux_function_app.read_model.identity[0].principal_id
  scope               = azurerm_cosmosdb_account.read_model.id
}
