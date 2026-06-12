locals {
  app_service_plan_name_value          = var.create_app_service_plan ? azurerm_service_plan.shared[0].name : data.azurerm_service_plan.existing_shared[0].name
  app_service_plan_id_value            = var.create_app_service_plan ? azurerm_service_plan.shared[0].id : data.azurerm_service_plan.existing_shared[0].id
  container_app_environment_name_value = var.create_container_app_environment ? azurerm_container_app_environment.env[0].name : data.azurerm_container_app_environment.existing_env[0].name
  container_app_environment_id_value   = var.create_container_app_environment ? azurerm_container_app_environment.env[0].id : data.azurerm_container_app_environment.existing_env[0].id
}

output "acr_name" {
  description = "Name of the shared Azure Container Registry."
  value       = azurerm_container_registry.acr.name
}

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

output "app_service_plan_id" {
  description = "Resource ID of the shared App Service Plan."
  value       = local.app_service_plan_id_value
}

output "app_service_plan_name" {
  description = "Name of the shared App Service Plan."
  value       = local.app_service_plan_name_value
}

output "container_app_environment_id" {
  description = "Resource ID of the Container Apps managed environment."
  value       = local.container_app_environment_id_value
}

output "container_app_environment_name" {
  description = "Name of the Container Apps managed environment."
  value       = local.container_app_environment_name_value
}

output "sql_server_id" {
  description = "Resource ID of the shared SQL server."
  value       = azurerm_mssql_server.sql.id
}

output "sql_server_name" {
  description = "Name of the shared SQL server."
  value       = azurerm_mssql_server.sql.name
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
