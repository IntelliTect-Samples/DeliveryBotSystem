output "app_service_name" {
  description = "Name of the provisioned App Service."
  value       = module.frontend_webapp.app_service_name
}

output "default_hostname" {
  description = "Default hostname of the Customer Frontend Web App."
  value       = module.frontend_webapp.default_hostname
}

output "app_url" {
  description = "HTTPS URL of the Customer Frontend Web App."
  value       = module.frontend_webapp.app_url
}
