# ── Shared Infrastructure ──────────────────────────────────────────────────────

module "shared_infra" {
  source                 = "./modules/shared-infra"
  resource_group_name    = var.resource_group_name
  location               = var.location
  sql_location           = var.sql_location
  sql_ad_admin_login     = var.sql_ad_admin_login
  sql_ad_admin_object_id = var.sql_ad_admin_object_id
  tenant_id              = var.tenant_id
}

# ── App Modules ────────────────────────────────────────────────────────────────

module "bot_api" {
  source                        = "./modules/bot-api"
  resource_group_name           = var.resource_group_name
  container_app_environment_id  = module.shared_infra.container_app_environment_id
  sql_server_id                 = module.shared_infra.sql_server_id
  acr_login_server              = module.shared_infra.acr_login_server
  acr_admin_username            = module.shared_infra.acr_admin_username
  acr_admin_password            = module.shared_infra.acr_admin_password
  bot_api_sql_connection_string = var.bot_api_sql_connection_string
}

module "robot_simulator" {
  source                       = "./modules/robot-simulator"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = module.shared_infra.container_app_environment_id
  acr_login_server             = module.shared_infra.acr_login_server
  acr_admin_username           = module.shared_infra.acr_admin_username
  acr_admin_password           = module.shared_infra.acr_admin_password
  eventhub_connection_string   = var.eventhub_connection_string
  robot_input_hub_name         = module.shared_infra.robot_input_hub_name
  robot_output_hub_name        = module.shared_infra.robot_output_hub_name
}

module "order_service" {
  source                       = "./modules/order-service"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = module.shared_infra.container_app_environment_id
  sql_server_id                = module.shared_infra.sql_server_id
}

module "frontend" {
  source              = "./modules/frontend"
  resource_group_name = var.resource_group_name
  location            = var.location
}

module "admin_app" {
  source              = "./modules/admin-app"
  resource_group_name = var.resource_group_name
  location            = var.location
}

