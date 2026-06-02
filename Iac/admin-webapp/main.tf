# Root configuration for the Admin & Maintenance App infrastructure.
#
# Composes the reusable ./modules/webapp module. Backend + provider config
# live in providers.tf; inputs and their defaults live in variables.tf.

module "admin_webapp" {
  source = "./modules/webapp"

  resource_group_name   = var.resource_group_name
  app_service_plan_name = var.app_service_plan_name
  app_service_name      = var.app_service_name
  node_version          = var.node_version
  botnet_api_url        = var.botnet_api_url
  simulator_api_url     = var.simulator_api_url
  tags                  = var.tags
}

# The App Service was originally declared at the root before the module
# refactor. Tell Terraform it simply moved addresses so the existing live
# resource is preserved instead of destroyed and recreated.
moved {
  from = azurerm_linux_web_app.admin
  to   = module.admin_webapp.azurerm_linux_web_app.admin
}
