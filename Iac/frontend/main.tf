module "frontend_webapp" {
  source = "./modules/webapp"

  resource_group_name = var.resource_group_name
  location            = var.location
  app_service_plan_id = var.app_service_plan_id
  app_service_name    = var.app_service_name
  node_version        = var.node_version
  tags                = var.tags
}
