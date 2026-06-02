# ---------------------------------------------------------------------------
# Data sources — reference existing Azure resources; nothing here is created
# or destroyed by this module.
# ---------------------------------------------------------------------------

data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_container_app_environment" "env" {
  name                = var.container_app_env_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_container_registry" "acr" {
  name                = var.acr_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

data "azurerm_eventhub_namespace" "evhns" {
  name                = var.event_hub_namespace_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

# ---------------------------------------------------------------------------
# Robot Simulator Container App
#
# Ownership boundary (Balanced approach):
#   OWNED by this module:
#     - Container image reference
#     - Ingress: external, port 8080
#     - Revision mode: single (stateful in-memory simulator; one replica only)
#     - Scale: min 1, max 1
#     - System-assigned managed identity
#     - ACR registry reference
#     - Non-sensitive env vars: ASPNETCORE_ENVIRONMENT, ASPNETCORE_URLS
#
#   NOT owned by this module (managed outside IaC):
#     - EventTransport__Mode
#     - EventTransport__ConnectionString
#     - EventTransport__InputEventHubName
#     - EventTransport__OutputEventHubName
#     - EventTransport__ConsumerGroup
#     - EventTransport__EnableInputConsumer
#     - Any Container App secrets
#
#   Reason: these env vars and secrets are already live on the Container App
#   and must not be overwritten during early IaC rollout. They will be brought
#   under IaC management when the project-wide IaC module is established.
# ---------------------------------------------------------------------------

resource "azurerm_container_app" "simulator" {
  name                         = var.container_app_name
  resource_group_name          = data.azurerm_resource_group.rg.name
  container_app_environment_id = data.azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  registry {
    server = data.azurerm_container_registry.acr.login_server
  }

  template {
    container {
      name   = var.container_app_name
      image  = "${data.azurerm_container_registry.acr.login_server}/${var.image_name}:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      # Non-sensitive runtime environment variables.
      # Event Hub transport settings are intentionally excluded — see ownership
      # boundary comment above.
      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }
    }

    # Scale is fixed at exactly one replica.
    # The simulator holds bot state in memory; multiple replicas would produce
    # independent, conflicting bot fleets with no shared state.
    min_replicas = 1
    max_replicas = 1
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  lifecycle {
    # Prevent Terraform from overwriting env vars or secrets that are managed
    # outside this module (e.g., Event Hub connection settings set via portal
    # or CLI). Remove this ignore block once those settings are brought under
    # IaC management.
    ignore_changes = [
      template[0].container[0].env,
      secret,
    ]
  }
}
