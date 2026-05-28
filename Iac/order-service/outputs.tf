output "container_app_name" {
  description = "Name of the provisioned Order Service Container App."
  value       = module.order_service_app.name
}

output "order_service_url" {
  description = "Public HTTPS URL of the Order Service."
  value       = module.order_service_app.url
}

output "managed_identity_principal_id" {
  description = "Principal ID of the app's system-assigned identity — grant this AcrPull and a SQL user."
  value       = module.order_service_app.identity_principal_id
}
