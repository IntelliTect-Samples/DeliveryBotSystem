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

output "agent_service_url" {
  description = "HTTPS URL of the Agent Service Container App."
  value       = module.agent_service.agent_service_url
}

# ── Readable Bot Network Representation ───────────────────────────────────────

output "readable_bot_network_function_app_name" {
  description = "Name of the readable bot network Function App."
  value       = module.readable_bot_network_representation.function_app_name
}

output "readable_bot_network_cosmos_account_name" {
  description = "Name of the readable bot network Cosmos DB account."
  value       = module.readable_bot_network_representation.cosmos_account_name
}

output "readable_bot_network_cosmos_database_name" {
  description = "Cosmos DB database name for the readable bot network."
  value       = module.readable_bot_network_representation.cosmos_database_name
}

output "readable_bot_network_cosmos_container_name" {
  description = "Cosmos DB container name for current bot documents."
  value       = module.readable_bot_network_representation.cosmos_container_name
}

output "readable_bot_network_diagnostics_container_name" {
  description = "Cosmos DB diagnostics container name for the readable bot network."
  value       = module.readable_bot_network_representation.cosmos_diagnostics_container_name
}

output "readable_bot_network_application_insights_name" {
  description = "Application Insights resource name for the readable bot network Function App."
  value       = module.readable_bot_network_representation.application_insights_name
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
