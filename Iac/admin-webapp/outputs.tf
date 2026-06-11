output "app_service_name" {
  description = "Name of the provisioned App Service."
  value       = module.admin_webapp.app_service_name
}

output "default_hostname" {
  description = "Default hostname of the Admin Web App."
  value       = module.admin_webapp.default_hostname
}

output "app_url" {
  description = "HTTPS URL of the Admin Web App."
  value       = module.admin_webapp.app_url
}

output "admin_app_insights_name" {
  description = "Name of the admin app's Application Insights resource."
  value       = azurerm_application_insights.admin.name
}

output "admin_app_insights_connection_string" {
  description = "App Insights connection string (client ingestion key) for the admin SPA."
  value       = azurerm_application_insights.admin.connection_string
  sensitive   = true
}
