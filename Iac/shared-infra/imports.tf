# One-time adoption of pre-existing shared resources into Terraform state.
#
# All resources below already exist in Azure. The first `terraform apply` will
# import them rather than try to create duplicates.
#
# SAFE TO DELETE after the first successful apply that shows "0 to add, 0 to
# destroy" for these addresses.

locals {
  sub = "a06983f7-7384-4a09-a092-b13a3896be85"
  rg  = "ewu-deliverybotsystem-rg"
}

import {
  to = azurerm_container_registry.acr
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.ContainerRegistry/registries/DeliverybotCR"
}

import {
  to = azurerm_log_analytics_workspace.logs
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.OperationalInsights/workspaces/workspaceewudeliverybotsystemrg8609"
}

import {
  to = azurerm_container_app_environment.env
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.App/managedEnvironments/managedEnvironment-ewudeliverybots-aa2f"
}

import {
  to = azurerm_eventhub_namespace.simulator
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.EventHub/namespaces/DeliverybotSimulator-EVHNS"
}

import {
  to = azurerm_eventhub.robot_input
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.EventHub/namespaces/DeliverybotSimulator-EVHNS/eventhubs/robot-input"
}

import {
  to = azurerm_eventhub.robot_output
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.EventHub/namespaces/DeliverybotSimulator-EVHNS/eventhubs/robot-output"
}

import {
  to = azurerm_mssql_server.sql
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.Sql/servers/deliverybotsystem-sql"
}

import {
  to = azurerm_mssql_firewall_rule.allow_azure_services
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.Sql/servers/deliverybotsystem-sql/firewallRules/AllowAzureServices"
}
