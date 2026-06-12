module "admin_webapp" {
  source = "./modules/webapp"

  resource_group_name = var.resource_group_name
  location            = var.location
  app_service_plan_id = var.app_service_plan_id
  app_service_name    = var.app_service_name
  node_version        = var.node_version
  botnet_api_url      = var.botnet_api_url
  simulator_api_url   = var.simulator_api_url
  tags                = var.tags
}
