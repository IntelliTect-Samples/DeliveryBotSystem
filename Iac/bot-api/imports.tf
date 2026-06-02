# One-time adoption of pre-existing Bot API resources into Terraform state.
#
# Both the Container App and its SQL database already exist in Azure.
# The first `terraform apply` will import them instead of recreating them.
#
# SAFE TO DELETE after the first successful apply.

locals {
  sub = "a06983f7-7384-4a09-a092-b13a3896be85"
  rg  = "ewu-deliverybotsystem-rg"
}

import {
  to = module.bot_api_app.azurerm_container_app.this
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.App/containerApps/ewu-deliverybotsystem-api"
}

import {
  to = azurerm_mssql_database.botnetapi_db
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.Sql/servers/deliverybotsystem-sql/databases/BotNetApiDb"
}
