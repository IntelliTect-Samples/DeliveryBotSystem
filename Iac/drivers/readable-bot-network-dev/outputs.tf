output "function_app_name" {
  description = "Temporary Function App created for the readable bot network projection."
  value       = module.readable_bot_network_representation.function_app_name
}

output "function_app_principal_id" {
  description = "Managed identity principal ID for Event Hub RBAC follow-up."
  value       = module.readable_bot_network_representation.function_app_principal_id
}

output "cosmos_account_name" {
  description = "Temporary Cosmos DB account created for the bot read model."
  value       = module.readable_bot_network_representation.cosmos_account_name
}

output "cosmos_account_endpoint" {
  description = "Cosmos DB endpoint for the bot read model."
  value       = module.readable_bot_network_representation.cosmos_account_endpoint
}

output "cosmos_database_name" {
  description = "Cosmos database name for the bot read model."
  value       = module.readable_bot_network_representation.cosmos_database_name
}

output "cosmos_container_name" {
  description = "Cosmos container name for current bot documents."
  value       = module.readable_bot_network_representation.cosmos_container_name
}

output "eventhub_consumer_group_name" {
  description = "Event Hub consumer group used by the projection Function App."
  value       = module.readable_bot_network_representation.eventhub_consumer_group_name
}
