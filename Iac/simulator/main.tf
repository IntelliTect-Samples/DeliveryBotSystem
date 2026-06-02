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
# The simulator holds bot state in memory; only a single replica is safe.
# All environment variables and secrets are managed here — the CD pipeline
# only updates the running image tag.
# ---------------------------------------------------------------------------

locals {
  sub = "a06983f7-7384-4a09-a092-b13a3896be85"
  rg  = var.resource_group_name
}

# One-time import of the pre-existing Container App.
# SAFE TO DELETE after the first successful apply.
import {
  to = azurerm_container_app.simulator
  id = "/subscriptions/${local.sub}/resourceGroups/${local.rg}/providers/Microsoft.App/containerApps/${var.container_app_name}"
}

resource "azurerm_container_app" "simulator" {
  name                         = var.container_app_name
  resource_group_name          = data.azurerm_resource_group.rg.name
  container_app_environment_id = data.azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  secret {
    name  = "eventhub-connection-string"
    value = var.eventhub_connection_string
  }

  registry {
    server = data.azurerm_container_registry.acr.login_server
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = var.container_app_name
      image  = "${data.azurerm_container_registry.acr.login_server}/${var.image_name}:${var.image_tag}"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Development"
      }

      env {
        name  = "ASPNETCORE_URLS"
        value = "http://+:8080"
      }

      env {
        name  = "EventTransport__Mode"
        value = var.event_transport_mode
      }

      env {
        name        = "EventTransport__ConnectionString"
        secret_name = "eventhub-connection-string"
      }

      env {
        name  = "EventTransport__InputEventHubName"
        value = var.event_transport_input_hub
      }

      env {
        name  = "EventTransport__OutputEventHubName"
        value = var.event_transport_output_hub
      }

      env {
        name  = "EventTransport__ConsumerGroup"
        value = var.event_transport_consumer_group
      }

      env {
        name  = "EventTransport__EnableInputConsumer"
        value = "true"
      }

      env {
        name  = "Simulator__InitialBotCount"
        value = tostring(var.simulator_initial_bot_count)
      }

      env {
        name  = "Simulator__BotIdPrefix"
        value = var.simulator_bot_id_prefix
      }

      env {
        name  = "Simulator__DefaultBotModel"
        value = var.simulator_default_bot_model
      }

      env {
        name  = "Simulator__DefaultLatitude"
        value = var.simulator_default_latitude
      }

      env {
        name  = "Simulator__DefaultLongitude"
        value = var.simulator_default_longitude
      }

      env {
        name  = "Simulation__TickIntervalSeconds"
        value = tostring(var.simulation_tick_interval_seconds)
      }

      env {
        name  = "Simulation__TelemetryIntervalSeconds"
        value = tostring(var.simulation_telemetry_interval_seconds)
      }

      env {
        name  = "Simulation__DeliverySpeedMetersPerSecond"
        value = tostring(var.simulation_delivery_speed_mps)
      }

      env {
        name  = "Simulation__DestinationArrivalThresholdMeters"
        value = tostring(var.simulation_arrival_threshold_meters)
      }
    }
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
    ignore_changes = [
      # The CD pipeline deploys new image tags per commit; let it own the
      # running image rather than reverting to var.image_tag on every apply.
      template[0].container[0].image,
    ]
  }
}

