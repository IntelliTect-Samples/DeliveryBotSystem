data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

locals {
  log_analytics_workspace_name                  = coalesce(var.log_analytics_workspace_name, "${var.app_service_plan_name}-logs")
  existing_container_app_environment_rg_name    = coalesce(var.existing_container_app_environment_resource_group_name, data.azurerm_resource_group.rg.name)
  existing_app_service_plan_resource_group_name = coalesce(var.existing_app_service_plan_resource_group_name, data.azurerm_resource_group.rg.name)
}

resource "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.location
  sku                 = "Standard"
  admin_enabled       = true
}

resource "azurerm_log_analytics_workspace" "logs" {
  name                = local.log_analytics_workspace_name
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_app_environment" "env" {
  count                      = var.create_container_app_environment ? 1 : 0
  name                       = var.container_app_environment_name
  resource_group_name        = data.azurerm_resource_group.rg.name
  location                   = var.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id
}

data "azurerm_container_app_environment" "existing_env" {
  count               = var.create_container_app_environment ? 0 : 1
  name                = var.container_app_environment_name
  resource_group_name = local.existing_container_app_environment_rg_name
}

resource "azurerm_eventhub_namespace" "simulator" {
  name                = var.eventhub_namespace_name
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.eventhub_location
  sku                 = "Standard"
  capacity            = 1
}

resource "azurerm_eventhub" "robot_input" {
  name              = "robot-input"
  namespace_id      = azurerm_eventhub_namespace.simulator.id
  partition_count   = var.robot_input_partition_count
  message_retention = var.robot_input_message_retention

  lifecycle {
    ignore_changes = [partition_count, message_retention]
  }
}

resource "azurerm_eventhub" "robot_output" {
  name              = "robot-output"
  namespace_id      = azurerm_eventhub_namespace.simulator.id
  partition_count   = var.robot_output_partition_count
  message_retention = var.robot_output_message_retention

  lifecycle {
    ignore_changes = [partition_count, message_retention]
  }
}

resource "azurerm_service_plan" "shared" {
  count               = var.create_app_service_plan ? 1 : 0
  name                = var.app_service_plan_name
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.location
  os_type             = "Linux"
  sku_name            = var.app_service_plan_sku_name
}

data "azurerm_service_plan" "existing_shared" {
  count               = var.create_app_service_plan ? 0 : 1
  name                = var.app_service_plan_name
  resource_group_name = local.existing_app_service_plan_resource_group_name
}

resource "azurerm_mssql_server" "sql" {
  name                = var.sql_server_name
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.sql_location
  version             = "12.0"

  azuread_administrator {
    login_username              = var.sql_ad_admin_login
    object_id                   = var.sql_ad_admin_object_id
    tenant_id                   = var.tenant_id
    azuread_authentication_only = true
  }
}

resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}