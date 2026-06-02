# Order Service infrastructure.
#
# Reuses the team's shared resource group, Container App Environment, and ACR
# (all created by the root Iac). This stack only owns the Order Service
# Container App itself, provisioned through the reusable container-app module.

data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_container_app_environment" "env" {
  name                = var.container_app_environment_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

# Dedicated consumer group for the Order Service's status consumer (#41).
# The namespace and the robot-output hub pre-exist on the shared simulator
# namespace (created outside this stack); we only add our own consumer group so
# our read offsets stay isolated from $Default and other features
# (e.g. readable-bot-network-dev).
resource "azurerm_eventhub_consumer_group" "order_service_status" {
  name                = var.status_consumer_group_name
  namespace_name      = var.event_hub_namespace_name
  eventhub_name       = var.status_event_hub_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

module "order_service_app" {
  source = "./modules/container-app"

  name                         = var.container_app_name
  resource_group_name          = data.azurerm_resource_group.rg.name
  container_app_environment_id = data.azurerm_container_app_environment.env.id

  # Pull from the shared ACR using admin credentials (same pattern as the
  # BotNet API and Robot Simulator apps).
  acr_login_server = data.azurerm_container_registry.acr.login_server
  acr_username     = data.azurerm_container_registry.acr.admin_username
  acr_password     = data.azurerm_container_registry.acr.admin_password

  container_name = "orderservice"
  image          = "${data.azurerm_container_registry.acr.login_server}/${var.image_name}:latest"
  target_port    = 8080

  # Secrets are referenced by env vars below.
  secrets = {
    "sql-connection-string"      = var.sql_connection_string
    "eventhub-connection-string" = var.eventhub_connection_string
  }

  env_vars = {
    "ASPNETCORE_ENVIRONMENT"        = "Production"
    "BotNetApi__BaseUrl"            = var.botnet_api_url
    "StatusConsumer__EventHubName"  = var.status_event_hub_name
    "StatusConsumer__ConsumerGroup" = azurerm_eventhub_consumer_group.order_service_status.name
  }

  secret_env_vars = {
    "ConnectionStrings__DefaultConnection" = "sql-connection-string"
    "EventHub__ConnectionString"           = "eventhub-connection-string"
    # Reuse the namespace-level Event Hub connection string — it has Listen on
    # all hubs in the namespace, incl. robot-output. If it is later scoped to a
    # Listen-only SAS, introduce a separate secret/variable for this.
    "StatusConsumer__ConnectionString" = "eventhub-connection-string"
  }

  tags = var.tags
}
