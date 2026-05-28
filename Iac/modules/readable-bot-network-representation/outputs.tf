output "function_app_name" {
  description = "Name of the Function App that will host the Event Hub projection code."
  value       = azurerm_linux_function_app.read_model.name
}

output "function_app_id" {
  description = "Resource ID of the Function App."
  value       = azurerm_linux_function_app.read_model.id
}

output "function_app_principal_id" {
  description = "System-assigned managed identity principal ID for the Function App."
  value       = azurerm_linux_function_app.read_model.identity[0].principal_id
}

output "cosmos_account_name" {
  description = "Name of the Cosmos DB account for the bot read model."
  value       = azurerm_cosmosdb_account.read_model.name
}

output "cosmos_account_endpoint" {
  description = "Endpoint URI for the Cosmos DB account."
  value       = azurerm_cosmosdb_account.read_model.endpoint
}

output "cosmos_database_name" {
  description = "Cosmos DB SQL database name."
  value       = azurerm_cosmosdb_sql_database.read_model.name
}

output "cosmos_container_name" {
  description = "Cosmos DB SQL container name."
  value       = azurerm_cosmosdb_sql_container.bots.name
}

output "cosmos_container_partition_key_paths" {
  description = "Partition key paths configured for the bots container."
  value       = azurerm_cosmosdb_sql_container.bots.partition_key_paths
}

output "eventhub_namespace_name" {
  description = "Event Hub namespace used by the read model Function App."
  value       = data.azurerm_eventhub_namespace.robot_events.name
}

output "eventhub_fully_qualified_namespace" {
  description = "Fully qualified Event Hub namespace for identity-based Function bindings."
  value       = local.eventhub_fully_qualified_domain
}

output "robot_output_eventhub_name" {
  description = "Robot output Event Hub consumed by the read model Function App."
  value       = data.azurerm_eventhub.robot_output.name
}

output "eventhub_consumer_group_name" {
  description = "Consumer group configured for the read model projection."
  value       = var.eventhub_consumer_group_name
}

output "storage_account_name" {
  description = "Storage account used by the Function App runtime."
  value       = azurerm_storage_account.function.name
}

output "application_insights_name" {
  description = "Application Insights resource for Function App telemetry."
  value       = azurerm_application_insights.read_model.name
}
