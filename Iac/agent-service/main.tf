data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_container_app_environment" "env" {
  name                = var.container_app_environment_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

module "agent_service_app" {
  source = "./modules/container-app"

  name                         = var.container_app_name
  resource_group_name          = data.azurerm_resource_group.rg.name
  container_app_environment_id = data.azurerm_container_app_environment.env.id

  acr_login_server = data.azurerm_container_registry.acr.login_server
  acr_username     = data.azurerm_container_registry.acr.admin_username
  acr_password     = data.azurerm_container_registry.acr.admin_password

  container_name = "agentservice"
  image          = "${data.azurerm_container_registry.acr.login_server}/${var.image_name}:latest"
  target_port    = 8080

  env_vars = {
    "ASPNETCORE_ENVIRONMENT"              = "Production"
    "AzureOpenAI__Endpoint"               = var.azure_openai_endpoint
    "AzureOpenAI__Deployment"             = var.azure_openai_deployment
    "AzureOpenAI__ApiVersion"             = var.azure_openai_api_version
    "AzureOpenAI__ApiKeySecretName"       = var.azure_openai_api_key_secret_name
    "KeyVault__VaultUri"                  = var.key_vault_uri
    "Integrations__OrderServiceBaseUrl"   = var.order_service_url
    "Integrations__SimulatorBaseUrl"      = var.simulator_api_url
    "TranscriptArchive__BlobServiceUri"   = var.transcript_archive_blob_service_uri
    "TranscriptArchive__ContainerName"    = var.transcript_archive_container_name
    "TranscriptArchive__Enabled"          = "true"
    "Search__Enabled"                     = "true"
    "Search__Endpoint"                    = var.search_endpoint
    "Search__IndexName"                   = var.search_index_name
    "ServiceBus__Enabled"                 = "true"
    "ServiceBus__FullyQualifiedNamespace" = var.servicebus_fully_qualified_namespace
    "ServiceBus__QueueName"               = var.support_escalation_queue_name
    "Cors__AllowedOrigins"                = var.cors_allowed_origins
  }

  tags = var.tags
}
