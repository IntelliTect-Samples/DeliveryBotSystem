output "container_app_name" {
  description = "Name of the provisioned Bot API Container App."
  value       = module.bot_api_app.name
}

output "bot_api_url" {
  description = "Public HTTPS URL of the Bot API."
  value       = module.bot_api_app.url
}

output "managed_identity_principal_id" {
  description = "Principal ID of the app's system-assigned identity — grant this db_owner on BotNetApiDb."
  value       = module.bot_api_app.identity_principal_id
}
