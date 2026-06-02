output "name" {
  description = "Name of the Container App."
  value       = azurerm_container_app.this.name
}

output "fqdn" {
  description = "Ingress FQDN of the Container App."
  value       = azurerm_container_app.this.ingress[0].fqdn
}

output "url" {
  description = "Public HTTPS URL of the Container App."
  value       = "https://${azurerm_container_app.this.ingress[0].fqdn}"
}

output "identity_principal_id" {
  description = "Principal ID of the system-assigned managed identity."
  value       = azurerm_container_app.this.identity[0].principal_id
}
