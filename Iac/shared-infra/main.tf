data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

# ── Azure Container Registry ────────────────────────────────────────────────

resource "azurerm_container_registry" "acr" {
  name                = "DeliverybotCR"
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.location
  sku                 = "Standard"
  admin_enabled       = true
}

# ── Log Analytics Workspace ─────────────────────────────────────────────────

resource "azurerm_log_analytics_workspace" "logs" {
  name                = "workspaceewudeliverybotsystemrg8609"
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# ── Container Apps Managed Environment ─────────────────────────────────────

resource "azurerm_container_app_environment" "env" {
  name                       = "managedEnvironment-ewudeliverybots-aa2f"
  resource_group_name        = data.azurerm_resource_group.rg.name
  location                   = var.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id
}

# ── Event Hub Namespace ─────────────────────────────────────────────────────

resource "azurerm_eventhub_namespace" "simulator" {
  name                = "DeliverybotSimulator-EVHNS"
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = var.location
  sku                 = "Standard"
  capacity            = 1
}

resource "azurerm_eventhub" "robot_input" {
  name              = "robot-input"
  namespace_id      = azurerm_eventhub_namespace.simulator.id
  partition_count   = 2
  message_retention = 1
}

resource "azurerm_eventhub" "robot_output" {
  name              = "robot-output"
  namespace_id      = azurerm_eventhub_namespace.simulator.id
  partition_count   = 2
  message_retention = 1
}

# ── SQL Server ──────────────────────────────────────────────────────────────
#
# Azure AD-only authentication — no SQL login password.
# Each service stack owns its own database on this server.

resource "azurerm_mssql_server" "sql" {
  name                = "deliverybotsystem-sql"
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

# Allow Azure services (Container Apps) to reach the SQL server.
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
