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
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "BotNetApi__BaseUrl"     = var.botnet_api_url
  }

  secret_env_vars = {
    "ConnectionStrings__DefaultConnection" = "sql-connection-string"
    "EventHub__ConnectionString"           = "eventhub-connection-string"
  }

  tags = var.tags
}
