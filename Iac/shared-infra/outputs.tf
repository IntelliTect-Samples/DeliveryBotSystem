output "acr_login_server" {
  description = "ACR login server hostname (e.g. deliverybotcr.azurecr.io)."
  value       = azurerm_container_registry.acr.login_server
}

output "acr_admin_username" {
  description = "ACR admin username."
  value       = azurerm_container_registry.acr.admin_username
}

output "acr_admin_password" {
  description = "ACR admin password."
  value       = azurerm_container_registry.acr.admin_password
  sensitive   = true
}

output "container_app_environment_id" {
  description = "Resource ID of the Container Apps managed environment."
  value       = azurerm_container_app_environment.env.id
}

output "container_app_environment_name" {
  description = "Name of the Container Apps managed environment."
  value       = azurerm_container_app_environment.env.name
}

output "sql_server_id" {
  description = "Resource ID of the shared SQL server."
  value       = azurerm_mssql_server.sql.id
}

output "sql_server_fqdn" {
  description = "Fully-qualified domain name of the SQL server."
  value       = azurerm_mssql_server.sql.fully_qualified_domain_name
}

output "eventhub_namespace_name" {
  description = "Name of the Event Hub namespace."
  value       = azurerm_eventhub_namespace.simulator.name
}

output "eventhub_namespace_fqdn" {
  description = "AMQP endpoint of the Event Hub namespace."
  value       = "${azurerm_eventhub_namespace.simulator.name}.servicebus.windows.net"
}

output "robot_input_hub_name" {
  description = "Name of the robot-input event hub."
  value       = azurerm_eventhub.robot_input.name
}

output "robot_output_hub_name" {
  description = "Name of the robot-output event hub."
  value       = azurerm_eventhub.robot_output.name
}
