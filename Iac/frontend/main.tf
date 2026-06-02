# Customer Frontend infrastructure.
#
# Reuses the team's shared resource group and App Service Plan.
# This stack only owns the customer-facing Web App (WA-DeliveryBot-dev).

module "frontend_webapp" {
  source = "./modules/webapp"

  resource_group_name   = var.resource_group_name
  app_service_plan_name = var.app_service_plan_name
  app_service_name      = var.app_service_name
  node_version          = var.node_version
  tags                  = var.tags
}
