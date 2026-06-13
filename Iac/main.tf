module "shared_infra" {
  source = "./shared-infra"

  resource_group_name                                    = var.resource_group_name
  location                                               = var.location
  eventhub_location                                      = var.eventhub_location
  sql_location                                           = var.sql_location
  acr_name                                               = var.acr_name
  app_service_plan_name                                  = var.app_service_plan_name
  app_service_plan_sku_name                              = var.app_service_plan_sku_name
  create_app_service_plan                                = var.create_app_service_plan
  existing_app_service_plan_resource_group_name          = var.existing_app_service_plan_resource_group_name
  container_app_environment_name                         = var.container_app_environment_name
  create_container_app_environment                       = var.create_container_app_environment
  existing_container_app_environment_resource_group_name = var.existing_container_app_environment_resource_group_name
  eventhub_namespace_name                                = var.eventhub_namespace_name
  robot_input_partition_count                            = var.robot_input_partition_count
  robot_output_partition_count                           = var.robot_output_partition_count
  sql_server_name                                        = var.bot_api_sql_server_name
  sql_ad_admin_login                                     = var.sql_ad_admin_login
  sql_ad_admin_object_id                                 = var.sql_ad_admin_object_id
  tenant_id                                              = var.tenant_id
}

module "admin_webapp" {
  source = "./admin-webapp"

  resource_group_name = var.resource_group_name
  location            = var.app_service_plan_location
  app_service_plan_id = module.shared_infra.app_service_plan_id
  app_service_name    = var.admin_app_service_name
  node_version        = var.node_version
  botnet_api_url      = var.botnet_api_url
  simulator_api_url   = var.simulator_api_url
}

module "order_service" {
  source = "./order-service"

  resource_group_name            = var.resource_group_name
  container_app_environment_name = module.shared_infra.container_app_environment_name
  acr_name                       = module.shared_infra.acr_name
  container_app_name             = var.order_service_container_app_name
  sql_connection_string          = var.order_service_sql_connection_string
  eventhub_connection_string     = var.eventhub_connection_string
  event_hub_namespace_name       = module.shared_infra.eventhub_namespace_name
  botnet_api_url                 = var.botnet_api_url
}

module "agent_service" {
  source = "./agent-service"

  resource_group_name                  = var.resource_group_name
  container_app_environment_name       = module.shared_infra.container_app_environment_name
  acr_name                             = module.shared_infra.acr_name
  container_app_name                   = var.agent_service_container_app_name
  azure_openai_endpoint                = var.azure_openai_endpoint
  azure_openai_deployment              = var.azure_openai_deployment
  azure_openai_api_version             = var.azure_openai_api_version
  azure_openai_api_key_secret_name     = azurerm_key_vault_secret.agent_openai_api_key.name
  key_vault_uri                        = azurerm_key_vault.agent.vault_uri
  transcript_archive_blob_service_uri  = azurerm_storage_account.agent_transcripts.primary_blob_endpoint
  transcript_archive_container_name    = azurerm_storage_container.agent_transcripts.name
  order_service_url                    = module.order_service.order_service_url
  simulator_api_url                    = module.simulator.container_app_url
  search_endpoint                      = "https://${azurerm_search_service.agent.name}.search.windows.net"
  search_index_name                    = var.agent_search_index_name
  servicebus_fully_qualified_namespace = "${azurerm_servicebus_namespace.support.name}.servicebus.windows.net"
  support_escalation_queue_name        = azurerm_servicebus_queue.support_escalations.name
  cors_allowed_origins                 = module.frontend.app_url
}

module "readable_bot_network_representation" {
  source = "./modules/readable-bot-network-representation"

  resource_group_name = var.resource_group_name
  location            = var.location
  name_prefix         = var.readable_bot_network_name_prefix
  environment         = var.readable_bot_network_environment

  eventhub_resource_group_name = var.readable_bot_network_eventhub_resource_group_name
  eventhub_namespace_name      = var.eventhub_namespace_name
  robot_output_eventhub_name   = var.readable_bot_network_robot_output_eventhub_name
  eventhub_consumer_group_name = var.readable_bot_network_consumer_group_name

  cosmos_account_name                 = var.readable_bot_network_cosmos_account_name
  cosmos_database_name                = var.readable_bot_network_cosmos_database_name
  cosmos_container_name               = var.readable_bot_network_cosmos_container_name
  cosmos_diagnostics_container_name   = var.readable_bot_network_diagnostics_container_name
  function_app_name                   = var.readable_bot_network_function_app_name
  service_plan_name                   = var.readable_bot_network_service_plan_name
  storage_account_name                = var.readable_bot_network_storage_account_name
  log_analytics_workspace_name        = var.readable_bot_network_log_analytics_workspace_name
  application_insights_name           = var.readable_bot_network_application_insights_name
  assign_eventhub_receiver_role       = var.readable_bot_network_assign_eventhub_receiver_role
  assign_cosmos_data_contributor_role = var.readable_bot_network_assign_cosmos_data_contributor_role
  create_eventhub_consumer_group      = var.readable_bot_network_create_eventhub_consumer_group
  additional_app_settings = {
    SupportEscalationQueueName                           = azurerm_servicebus_queue.support_escalations.name
    SupportEscalationServiceBus__fullyQualifiedNamespace = "${azurerm_servicebus_namespace.support.name}.servicebus.windows.net"
    SupportEscalationServiceBus__credential              = "managedidentity"
    EscalationArchive__BlobServiceUri                    = azurerm_storage_account.agent_transcripts.primary_blob_endpoint
    EscalationArchive__ContainerName                     = azurerm_storage_container.support_escalations.name
  }
}

module "bot_api" {
  source = "./bot-api"

  resource_group_name            = var.resource_group_name
  container_app_environment_name = module.shared_infra.container_app_environment_name
  acr_name                       = module.shared_infra.acr_name
  sql_server_name                = module.shared_infra.sql_server_name
  container_app_name             = var.bot_api_container_app_name
  sql_connection_string          = var.bot_api_sql_connection_string
}

module "frontend" {
  source = "./frontend"

  resource_group_name = var.resource_group_name
  location            = var.app_service_plan_location
  app_service_plan_id = module.shared_infra.app_service_plan_id
  app_service_name    = var.customer_frontend_app_service_name
  node_version        = var.node_version
}

module "simulator" {
  source = "./simulator"

  resource_group_name        = var.resource_group_name
  location                   = var.location
  container_app_env_name     = module.shared_infra.container_app_environment_name
  acr_name                   = module.shared_infra.acr_name
  event_hub_namespace_name   = module.shared_infra.eventhub_namespace_name
  eventhub_connection_string = var.eventhub_connection_string
  container_app_name         = var.simulator_container_app_name
}
