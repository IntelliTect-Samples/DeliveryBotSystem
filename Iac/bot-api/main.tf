# Bot API infrastructure.
#
# Reuses the team's shared resource group, Container App Environment, ACR, and
# SQL server (all managed by Iac/shared-infra). This stack owns the Bot API
# Container App and its SQL database.

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

data "azurerm_mssql_server" "sql" {
  name                = var.sql_server_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

# ── SQL Database ─────────────────────────────────────────────────────────────
#
# Serverless General Purpose — auto-pauses when idle, scales vCores on demand.
# The Container App's managed identity is granted db_owner out-of-band via the
# deploy workflow (EF Core migrate runs on startup using Managed Identity auth).

resource "azurerm_mssql_database" "botnetapi_db" {
  name      = "BotNetApiDb"
  server_id = data.azurerm_mssql_server.sql.id
  sku_name  = "GP_S_Gen5_2"

  max_size_gb                 = 32
  min_capacity                = 0.5
  auto_pause_delay_in_minutes = 60
  zone_redundant              = false
}

# ── Container App ─────────────────────────────────────────────────────────────

module "bot_api_app" {
  source = "./modules/container-app"

  name                         = var.container_app_name
  resource_group_name          = data.azurerm_resource_group.rg.name
  container_app_environment_id = data.azurerm_container_app_environment.env.id

  acr_login_server = data.azurerm_container_registry.acr.login_server
  acr_username     = data.azurerm_container_registry.acr.admin_username
  acr_password     = data.azurerm_container_registry.acr.admin_password

  container_name = "botnetapi"
  image          = "${data.azurerm_container_registry.acr.login_server}/${var.image_name}:latest"
  target_port    = 8080

  secrets = {
    "sql-connection-string" = var.sql_connection_string
  }

  env_vars = {
    "ASPNETCORE_ENVIRONMENT" = "Production"
  }

  secret_env_vars = {
    "ConnectionStrings__DefaultConnection" = "sql-connection-string"
  }

  min_replicas = 0
  max_replicas = 3

  tags = var.tags
}
