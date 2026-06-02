output "app_service_name" {
  description = "Name of the provisioned App Service."
  value       = azurerm_linux_web_app.frontend.name
}

output "default_hostname" {
  description = "Default hostname of the App Service."
  value       = azurerm_linux_web_app.frontend.default_hostname
}

output "app_url" {
  description = "HTTPS URL of the App Service."
  value       = "https://${azurerm_linux_web_app.frontend.default_hostname}"
}
