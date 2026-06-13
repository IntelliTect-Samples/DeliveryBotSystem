# Universal DeliveryBot infrastructure — root composition file.
#
# This is the single top-level configuration that wires together all
# per-service modules. Each subdirectory of Iac/ is a Terraform module that
# owns the resources for one service; this file injects the shared variables
# into each one and composes them into a single apply.
#
# Dependency ordering:
#   shared-infra   → no dependencies on other modules
#   bot-api        → depends on shared SQL server (data source inside module)
#   order-service  → depends on shared CAE + ACR (data sources inside module)
#   simulator      → depends on shared CAE + ACR + Event Hub NS
#   admin-webapp   → no Azure dependencies on other modules (App Service Plan
#                    is looked up by name via data source)
#   frontend       → same pattern as admin-webapp
#
# Terraform resolves the apply order automatically from data source / output
# references. No explicit depends_on is needed here.

# ── Shared infrastructure ──────────────────────────────────────────────────────
# Owns: ACR, Log Analytics workspace, Container App Environment,
#       Event Hub namespace + hubs, SQL server + firewall rule.

module "shared_infra" {
  source = "./shared-infra"

  resource_group_name    = var.resource_group_name
  location               = var.location
  sql_location           = var.sql_location
  sql_ad_admin_login     = var.sql_ad_admin_login
  sql_ad_admin_object_id = var.sql_ad_admin_object_id
  tenant_id              = var.tenant_id
}

# ── Admin Web App ──────────────────────────────────────────────────────────────
# Owns: the WA-DeliveryBot-Admin-dev App Service.

module "admin_webapp" {
  source = "./admin-webapp"

  resource_group_name   = var.resource_group_name
  app_service_plan_name = var.app_service_plan_name
  app_service_name      = var.admin_app_service_name
  node_version          = var.node_version
  botnet_api_url        = var.botnet_api_url
  simulator_api_url     = var.simulator_api_url
}

# ── Order Service ──────────────────────────────────────────────────────────────
# Owns: the deliverybot-order-service Container App.

module "order_service" {
  source = "./order-service"

  resource_group_name            = var.resource_group_name
  container_app_environment_name = var.container_app_environment_name
  acr_name                       = var.acr_name
  container_app_name             = var.order_service_container_app_name
  sql_connection_string          = var.order_service_sql_connection_string
  eventhub_connection_string     = var.eventhub_connection_string
  botnet_api_url                 = var.botnet_api_url
  robot_simulator_url            = var.simulator_api_url
}

# ── Bot API ────────────────────────────────────────────────────────────────────
# Owns: the ewu-deliverybotsystem-api Container App and its SQL database.

module "bot_api" {
  source = "./bot-api"

  resource_group_name            = var.resource_group_name
  container_app_environment_name = var.container_app_environment_name
  acr_name                       = var.acr_name
  sql_server_name                = var.bot_api_sql_server_name
  container_app_name             = var.bot_api_container_app_name
  sql_connection_string          = var.bot_api_sql_connection_string
}

# ── Customer Frontend ──────────────────────────────────────────────────────────
# Owns: the WA-DeliveryBot-dev App Service.

module "frontend" {
  source = "./frontend"

  resource_group_name   = var.resource_group_name
  app_service_plan_name = var.app_service_plan_name
  app_service_name      = var.customer_frontend_app_service_name
  node_version          = var.node_version
}

# ── Robot Simulator ────────────────────────────────────────────────────────────
# Owns: the deliverybot-robot-simulator Container App.
# Note: simulator/variables.tf uses container_app_env_name (not
# container_app_environment_name) for historical reasons; mapped here.

module "simulator" {
  source = "./simulator"

  resource_group_name        = var.resource_group_name
  location                   = var.location
  container_app_env_name     = var.container_app_environment_name
  acr_name                   = var.acr_name
  event_hub_namespace_name   = var.eventhub_namespace_name
  eventhub_connection_string = var.eventhub_connection_string
  container_app_name         = var.simulator_container_app_name
}
