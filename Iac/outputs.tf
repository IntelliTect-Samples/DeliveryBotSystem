output "bot_api_url" {
  description = "Public HTTPS URL of the Bot API Container App."
  value       = module.bot_api.url
}

output "robot_simulator_url" {
  description = "Public HTTPS URL of the Robot Simulator Container App."
  value       = module.robot_simulator.url
}

output "order_service_url" {
  description = "Public HTTPS URL of the Order Service Container App."
  value       = module.order_service.url
}

output "acr_login_server" {
  description = "ACR login server hostname."
  value       = module.shared_infra.acr_login_server
}

output "sql_server_fqdn" {
  description = "Fully-qualified domain name of the SQL server."
  value       = module.shared_infra.sql_server_fqdn
}

output "eventhub_namespace_fqdn" {
  description = "AMQP endpoint of the Event Hub namespace."
  value       = module.shared_infra.eventhub_namespace_fqdn
}

output "bot_api_principal_id" {
  description = "Managed identity principal ID of the Bot API (use for SQL db_owner role assignment)."
  value       = module.bot_api.principal_id
}

output "order_service_principal_id" {
  description = "Managed identity principal ID of the Order Service."
  value       = module.order_service.principal_id
}

