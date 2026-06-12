output "container_app_name" {
  description = "Name of the provisioned Agent Service Container App."
  value       = module.agent_service_app.name
}

output "agent_service_url" {
  description = "Public HTTPS URL of the Agent Service."
  value       = module.agent_service_app.url
}

output "managed_identity_principal_id" {
  description = "Principal ID of the app's system-assigned identity."
  value       = module.agent_service_app.identity_principal_id
}
