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

  secrets = {
    "azure-openai-api-key" = var.azure_openai_api_key
  }

  env_vars = {
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "AzureOpenAI__Endpoint"  = var.azure_openai_endpoint
    "AzureOpenAI__Deployment" = var.azure_openai_deployment
    "AzureOpenAI__ApiVersion" = var.azure_openai_api_version
  }

  secret_env_vars = {
    "AzureOpenAI__ApiKey" = "azure-openai-api-key"
  }

  tags = var.tags
}
