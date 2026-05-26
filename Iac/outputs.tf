output "bot_api_url" {
  description = "Public HTTPS URL of the Bot API Container App."
  value       = "https://${azurerm_container_app.bot_api.ingress[0].fqdn}"
}

output "robot_simulator_url" {
  description = "Public HTTPS URL of the Robot Simulator Container App."
  value       = "https://${azurerm_container_app.robot_simulator.ingress[0].fqdn}"
}

output "order_service_url" {
  description = "Public HTTPS URL of the Order Service Container App."
  value       = "https://${azurerm_container_app.order_service.ingress[0].fqdn}"
}

output "acr_login_server" {
  description = "ACR login server hostname."
  value       = azurerm_container_registry.acr.login_server
}

output "sql_server_fqdn" {
  description = "Fully-qualified domain name of the SQL server."
  value       = azurerm_mssql_server.sql.fully_qualified_domain_name
}

output "eventhub_namespace_fqdn" {
  description = "AMQP endpoint of the Event Hub namespace."
  value       = "${azurerm_eventhub_namespace.simulator.name}.servicebus.windows.net"
}

output "bot_api_principal_id" {
  description = "Managed identity principal ID of the Bot API (use for SQL db_owner role assignment)."
  value       = azurerm_container_app.bot_api.identity[0].principal_id
}

output "order_service_principal_id" {
  description = "Managed identity principal ID of the Order Service."
  value       = azurerm_container_app.order_service.identity[0].principal_id
}
