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

# ── Observability (final feature: Azure Monitor) ────────────────────────────
# Dedicated Log Analytics workspace + Application Insights for the admin app.
# The App Insights connection string is a client ingestion key (not a secret),
# baked into the SPA at build time — so no data-plane role assignment is needed
# and this stays fully self-service (no portal/RBAC work).
resource "azurerm_log_analytics_workspace" "admin" {
  name                = "law-deliverybot-admin"
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_application_insights" "admin" {
  name                = "appi-deliverybot-admin"
  resource_group_name = var.resource_group_name
  location            = var.location
  workspace_id        = azurerm_log_analytics_workspace.admin.id
  application_type    = "web"
  tags                = var.tags
}
