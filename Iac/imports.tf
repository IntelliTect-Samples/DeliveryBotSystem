# ---------------------------------------------------------------------------
# One-time import of all pre-existing Azure resources into Terraform state.
#
# Import blocks MUST live in the root module — Terraform does not allow them
# inside child modules. All addresses below are fully-qualified from root.
#
# SAFE TO DELETE after the first successful apply that shows these resources
# as "already imported" (no changes planned for them).
# ---------------------------------------------------------------------------

locals {
  import_sub = "a06983f7-7384-4a09-a092-b13a3896be85"
  import_rg  = "ewu-deliverybotsystem-rg"
}

# ── Shared Infrastructure ──────────────────────────────────────────────────────

import {
  to = module.shared_infra.azurerm_container_registry.acr
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.ContainerRegistry/registries/DeliverybotCR"
}

import {
  to = module.shared_infra.azurerm_log_analytics_workspace.logs
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.OperationalInsights/workspaces/workspaceewudeliverybotsystemrg8609"
}

import {
  to = module.shared_infra.azurerm_container_app_environment.env
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.App/managedEnvironments/managedEnvironment-ewudeliverybots-aa2f"
}

import {
  to = module.shared_infra.azurerm_eventhub_namespace.simulator
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.EventHub/namespaces/DeliverybotSimulator-EVHNS"
}

import {
  to = module.shared_infra.azurerm_eventhub.robot_input
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.EventHub/namespaces/DeliverybotSimulator-EVHNS/eventhubs/robot-input"
}

import {
  to = module.shared_infra.azurerm_eventhub.robot_output
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.EventHub/namespaces/DeliverybotSimulator-EVHNS/eventhubs/robot-output"
}

import {
  to = module.shared_infra.azurerm_mssql_server.sql
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.Sql/servers/deliverybotsystem-sql"
}

import {
  to = module.shared_infra.azurerm_mssql_firewall_rule.allow_azure_services
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.Sql/servers/deliverybotsystem-sql/firewallRules/AllowAzureServices"
}

# ── Bot API ────────────────────────────────────────────────────────────────────

import {
  to = module.bot_api.module.bot_api_app.azurerm_container_app.this
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.App/containerApps/ewu-deliverybotsystem-api"
}

import {
  to = module.bot_api.azurerm_mssql_database.botnetapi_db
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.Sql/servers/deliverybotsystem-sql/databases/BotNetApiDb"
}

# ── Order Service ──────────────────────────────────────────────────────────────

import {
  to = module.order_service.module.order_service_app.azurerm_container_app.this
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.App/containerApps/deliverybot-order-service"
}

# ── Customer Frontend ──────────────────────────────────────────────────────────

import {
  to = module.frontend.module.frontend_webapp.azurerm_linux_web_app.frontend
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.Web/sites/WA-DeliveryBot-dev"
}

# ── Admin Web App ──────────────────────────────────────────────────────────────

import {
  to = module.admin_webapp.module.admin_webapp.azurerm_linux_web_app.admin
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.Web/sites/WA-DeliveryBot-Admin-dev"
}

# ── Robot Simulator ────────────────────────────────────────────────────────────

import {
  to = module.simulator.azurerm_container_app.simulator
  id = "/subscriptions/${local.import_sub}/resourceGroups/${local.import_rg}/providers/Microsoft.App/containerApps/deliverybot-robot-simulator"
}
