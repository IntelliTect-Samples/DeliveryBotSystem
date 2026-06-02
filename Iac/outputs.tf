# Aggregated outputs from all service modules.

# ── Shared infrastructure ──────────────────────────────────────────────────────

output "acr_login_server" {
  description = "ACR login server hostname."
  value       = module.shared_infra.acr_login_server
}

output "container_app_environment_id" {
  description = "Resource ID of the shared Container App Environment."
  value       = module.shared_infra.container_app_environment_id
}

output "sql_server_fqdn" {
  description = "FQDN of the shared SQL server."
  value       = module.shared_infra.sql_server_fqdn
}

# ── Admin Web App ──────────────────────────────────────────────────────────────

output "admin_webapp_url" {
  description = "HTTPS URL of the Admin Web App."
  value       = module.admin_webapp.app_url
}

# ── Order Service ──────────────────────────────────────────────────────────────

output "order_service_url" {
  description = "HTTPS URL of the Order Service Container App."
  value       = module.order_service.order_service_url
}

# ── Bot API ────────────────────────────────────────────────────────────────────

output "bot_api_url" {
  description = "HTTPS URL of the Bot API Container App."
  value       = module.bot_api.bot_api_url
}

# ── Customer Frontend ──────────────────────────────────────────────────────────

output "customer_frontend_url" {
  description = "HTTPS URL of the Customer Frontend App Service."
  value       = module.frontend.app_url
}

# ── Robot Simulator ────────────────────────────────────────────────────────────

output "simulator_url" {
  description = "HTTPS URL of the Robot Simulator Container App."
  value       = module.simulator.container_app_url
}

output "simulator_health_url" {
  description = "Health check endpoint for the Robot Simulator."
  value       = module.simulator.health_url
}
