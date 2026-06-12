output "name" {
  description = "Container App name."
  value       = azurerm_container_app.this.name
}

output "url" {
  description = "Public HTTPS URL of the Container App."
  value       = "https://${azurerm_container_app.this.latest_revision_fqdn}"
}

output "identity_principal_id" {
  description = "System-assigned managed identity principal ID."
  value       = azurerm_container_app.this.identity[0].principal_id
}
