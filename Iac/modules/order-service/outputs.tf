output "url" {
  description = "Public HTTPS URL of the Order Service Container App."
  value       = "https://${azurerm_container_app.order_service.ingress[0].fqdn}"
}

output "principal_id" {
  description = "Managed identity principal ID of the Order Service."
  value       = azurerm_container_app.order_service.identity[0].principal_id
}
