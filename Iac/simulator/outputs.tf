output "container_app_name" {
  description = "Name of the simulator Container App."
  value       = azurerm_container_app.simulator.name
}

output "container_app_fqdn" {
  description = "Fully-qualified domain name of the simulator Container App ingress."
  value       = azurerm_container_app.simulator.ingress[0].fqdn
}

output "container_app_url" {
  description = "Base HTTPS URL for the simulator."
  value       = "https://${azurerm_container_app.simulator.ingress[0].fqdn}"
}

output "health_url" {
  description = "Health check URL for the simulator."
  value       = "https://${azurerm_container_app.simulator.ingress[0].fqdn}/health"
}

output "acr_login_server" {
  description = "ACR login server used for the simulator image."
  value       = data.azurerm_container_registry.acr.login_server
}

output "image_reference" {
  description = "Full image reference deployed to the Container App."
  value       = "${data.azurerm_container_registry.acr.login_server}/${var.image_name}:${var.image_tag}"
}

output "event_hub_namespace_name" {
  description = "Name of the Event Hub namespace referenced by the simulator."
  value       = data.azurerm_eventhub_namespace.evhns.name
}

output "event_hub_namespace_fqdn" {
  description = "Fully-qualified Event Hub namespace host (for reference)."
  value       = "${data.azurerm_eventhub_namespace.evhns.name}.servicebus.windows.net"
}
