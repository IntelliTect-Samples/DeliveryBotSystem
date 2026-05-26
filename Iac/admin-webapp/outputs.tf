output "app_service_name" {
  description = "Name of the provisioned App Service."
  value       = azurerm_linux_web_app.admin.name
}

output "default_hostname" {
  description = "Default hostname of the Admin Web App."
  value       = azurerm_linux_web_app.admin.default_hostname
}

output "app_url" {
  description = "HTTPS URL of the Admin Web App."
  value       = "https://${azurerm_linux_web_app.admin.default_hostname}"
}
