# Azure AI Foundry (Azure OpenAI) for the AI Delivery Concierge (#43).
#
# Consumption-billed: there is no standing/hourly cost — you pay per token only,
# and gpt-4o-mini is fractions of a cent at demo volume.
#
# AUTH: the app code prefers keyless managed identity (DefaultAzureCredential).
# The ideal setup grants the app's identity the "Cognitive Services OpenAI User"
# role (see the commented role_assignment below) and sets local_auth_enabled=false.
# That role grant needs `Microsoft.Authorization/roleAssignments/write`, which the
# CI service principal does NOT have — so for now we keep key auth enabled and pass
# the key in as a Container App secret (same pattern as the Event Hub secret).
# Once someone with RBAC rights enables the role, flip local_auth_enabled to false,
# uncomment the role_assignment, and remove the Foundry__ApiKey secret.

resource "azurerm_cognitive_account" "foundry" {
  name                  = var.foundry_account_name
  resource_group_name   = data.azurerm_resource_group.rg.name
  location              = var.foundry_location
  kind                  = "OpenAI"
  sku_name              = "S0"
  custom_subdomain_name = var.foundry_account_name

  # Key auth stays enabled until the managed-identity role grant is in place.
  local_auth_enabled = true

  tags = var.tags
}

resource "azurerm_cognitive_deployment" "concierge" {
  name                 = var.foundry_deployment_name
  cognitive_account_id = azurerm_cognitive_account.foundry.id

  model {
    format  = "OpenAI"
    name    = var.foundry_model_name
    version = var.foundry_model_version
  }

  sku {
    name     = "GlobalStandard"
    capacity = var.foundry_capacity
  }
}

# Grant the Order Service's managed identity permission to call the model (keyless).
# DISABLED: creating a role assignment needs `Microsoft.Authorization/roleAssignments/write`,
# which the RG-scoped CI service principal lacks (same wall hit with AcrPull). Uncomment
# once an owner grants the SP "Role Based Access Control Administrator", then set
# local_auth_enabled=false above and drop the Foundry__ApiKey secret.
# resource "azurerm_role_assignment" "concierge_openai_user" {
#   scope                = azurerm_cognitive_account.foundry.id
#   role_definition_name = "Cognitive Services OpenAI User"
#   principal_id         = module.order_service_app.identity_principal_id
# }
