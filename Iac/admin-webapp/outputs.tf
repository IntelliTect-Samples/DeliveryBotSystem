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
