data "azurerm_client_config" "current" {}

resource "random_string" "agent_support_suffix" {
  length  = 6
  upper   = false
  special = false
}

locals {
  agent_key_vault_name                  = coalesce(var.agent_key_vault_name, "deliverybotagt-${random_string.agent_support_suffix.result}-kv")
  agent_transcript_storage_account_name = coalesce(var.agent_transcript_storage_account_name, "deliverybotagt${random_string.agent_support_suffix.result}sa")
  agent_search_service_name             = coalesce(var.agent_search_service_name, "deliverybotagt-${random_string.agent_support_suffix.result}-search")
  support_servicebus_namespace_name     = coalesce(var.support_servicebus_namespace_name, "deliverybotagt-${random_string.agent_support_suffix.result}-sb")
  api_management_name                   = coalesce(var.api_management_name, "deliverybotagt-${random_string.agent_support_suffix.result}-apim")
}

resource "azurerm_key_vault" "agent" {
  name                = local.agent_key_vault_name
  location            = var.location
  resource_group_name = var.resource_group_name
  tenant_id           = var.tenant_id
  sku_name            = "standard"

  soft_delete_retention_days = 7
  purge_protection_enabled   = false
}

resource "azurerm_key_vault_access_policy" "agent_provisioner" {
  key_vault_id = azurerm_key_vault.agent.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azurerm_client_config.current.object_id

  secret_permissions = [
    "Delete",
    "Get",
    "List",
    "Purge",
    "Recover",
    "Set",
  ]
}

resource "azurerm_key_vault_secret" "agent_openai_api_key" {
  name         = var.agent_key_vault_openai_secret_name
  value        = var.azure_openai_api_key
  key_vault_id = azurerm_key_vault.agent.id

  depends_on = [azurerm_key_vault_access_policy.agent_provisioner]
}

resource "azurerm_storage_account" "agent_transcripts" {
  name                     = local.agent_transcript_storage_account_name
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  allow_nested_items_to_be_public = false

  blob_properties {
    versioning_enabled = true

    delete_retention_policy {
      days = 7
    }

    container_delete_retention_policy {
      days = 7
    }
  }
}

resource "azurerm_storage_container" "agent_transcripts" {
  name                  = var.agent_transcript_container_name
  storage_account_id    = azurerm_storage_account.agent_transcripts.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "support_escalations" {
  name                  = var.support_escalation_container_name
  storage_account_id    = azurerm_storage_account.agent_transcripts.id
  container_access_type = "private"
}

resource "azurerm_search_service" "agent" {
  name                = local.agent_search_service_name
  resource_group_name = var.resource_group_name
  location            = var.location
  sku                 = var.agent_search_sku

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_servicebus_namespace" "support" {
  name                = local.support_servicebus_namespace_name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
}

resource "azurerm_servicebus_queue" "support_escalations" {
  name         = var.support_escalation_queue_name
  namespace_id = azurerm_servicebus_namespace.support.id

  requires_duplicate_detection            = true
  duplicate_detection_history_time_window = "PT10M"
}

resource "azurerm_api_management" "deliverybot" {
  name                = local.api_management_name
  location            = var.location
  resource_group_name = var.resource_group_name
  publisher_name      = var.api_management_publisher_name
  publisher_email     = var.api_management_publisher_email
  sku_name            = "Consumption_0"

  identity {
    type = "SystemAssigned"
  }
}

locals {
  apim_apis = {
    orders = {
      display_name = "DeliveryBot Order Service"
      path         = "orders"
      service_url  = module.order_service.order_service_url
    }
    agent = {
      display_name = "DeliveryBot Agent Service"
      path         = "agent"
      service_url  = module.agent_service.agent_service_url
    }
    botnet = {
      display_name = "DeliveryBot BotNet API"
      path         = "botnet"
      service_url  = module.bot_api.bot_api_url
    }
  }

  apim_operation_methods = toset(["GET", "POST", "PUT", "PATCH", "DELETE"])
  apim_operations = {
    for pair in setproduct(keys(local.apim_apis), local.apim_operation_methods) :
    "${pair[0]}-${lower(pair[1])}" => {
      api_name = pair[0]
      method   = pair[1]
    }
  }
}

resource "azurerm_api_management_api" "deliverybot" {
  for_each = local.apim_apis

  name                  = each.key
  resource_group_name   = var.resource_group_name
  api_management_name   = azurerm_api_management.deliverybot.name
  revision              = "1"
  display_name          = each.value.display_name
  path                  = each.value.path
  protocols             = ["https"]
  service_url           = each.value.service_url
  subscription_required = false
}

resource "azurerm_api_management_api_operation" "deliverybot_proxy" {
  for_each = local.apim_operations

  operation_id        = "proxy-${lower(each.value.method)}"
  api_name            = azurerm_api_management_api.deliverybot[each.value.api_name].name
  api_management_name = azurerm_api_management.deliverybot.name
  resource_group_name = var.resource_group_name
  display_name        = "${each.value.method} proxy"
  method              = each.value.method
  url_template        = "/*"

  response {
    status_code = 200
  }
}

resource "azurerm_key_vault_access_policy" "agent_service" {
  key_vault_id = azurerm_key_vault.agent.id
  tenant_id    = var.tenant_id
  object_id    = module.agent_service.managed_identity_principal_id

  secret_permissions = ["Get"]
}

resource "azurerm_role_assignment" "agent_transcript_blob_contributor" {
  scope                = azurerm_storage_account.agent_transcripts.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = module.agent_service.managed_identity_principal_id
}

resource "azurerm_role_assignment" "agent_search_index_contributor" {
  scope                = azurerm_search_service.agent.id
  role_definition_name = "Search Index Data Contributor"
  principal_id         = module.agent_service.managed_identity_principal_id
}

resource "azurerm_role_assignment" "agent_search_service_contributor" {
  scope                = azurerm_search_service.agent.id
  role_definition_name = "Search Service Contributor"
  principal_id         = module.agent_service.managed_identity_principal_id
}

resource "azurerm_role_assignment" "agent_servicebus_sender" {
  scope                = azurerm_servicebus_queue.support_escalations.id
  role_definition_name = "Azure Service Bus Data Sender"
  principal_id         = module.agent_service.managed_identity_principal_id
}

resource "azurerm_role_assignment" "function_servicebus_receiver" {
  scope                = azurerm_servicebus_queue.support_escalations.id
  role_definition_name = "Azure Service Bus Data Receiver"
  principal_id         = module.readable_bot_network_representation.function_app_principal_id
}

resource "azurerm_role_assignment" "function_support_blob_contributor" {
  scope                = azurerm_storage_account.agent_transcripts.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = module.readable_bot_network_representation.function_app_principal_id
}
