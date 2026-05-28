output "url" {
  description = "Public HTTPS URL of the Bot API Container App."
  value       = "https://${azurerm_container_app.bot_api.ingress[0].fqdn}"
}

output "principal_id" {
  description = "Managed identity principal ID of the Bot API."
  value       = azurerm_container_app.bot_api.identity[0].principal_id
}
