resource "azurerm_container_app" "robot_simulator" {
  name                         = "deliverybot-robot-simulator"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"

  secret {
    name  = "acr-password"
    value = var.acr_admin_password
  }

  secret {
    name  = "eventhub-connection-string"
    value = var.eventhub_connection_string
  }

  registry {
    server               = var.acr_login_server
    username             = var.acr_admin_username
    password_secret_name = "acr-password"
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    min_replicas = 0
    max_replicas = 10

    container {
      name   = "robot-simulator"
      image  = "${var.acr_login_server}/deliverybot-robot-simulator:v1"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Development"
      }

      env {
        name  = "EventTransport__Mode"
        value = "AzureEventHub"
      }

      env {
        name  = "EventTransport__InputEventHubName"
        value = var.robot_input_hub_name
      }

      env {
        name  = "EventTransport__OutputEventHubName"
        value = var.robot_output_hub_name
      }

      env {
        name  = "EventTransport__ConsumerGroup"
        value = "$Default"
      }

      env {
        name  = "EventTransport__EnableInputConsumer"
        value = "true"
      }

      env {
        name        = "EventTransport__ConnectionString"
        secret_name = "eventhub-connection-string"
      }

      env {
        name  = "Simulator__InitialBotCount"
        value = "3"
      }

      env {
        name  = "Simulator__BotIdPrefix"
        value = "bot"
      }

      env {
        name  = "Simulator__DefaultBotModel"
        value = "DeliveryBot-V1"
      }

      env {
        name  = "Simulator__DefaultLatitude"
        value = "47.65837359646208"
      }

      env {
        name  = "Simulator__DefaultLongitude"
        value = "-117.40215401730164"
      }

      env {
        name  = "Simulation__TickIntervalSeconds"
        value = "1"
      }

      env {
        name  = "Simulation__TelemetryIntervalSeconds"
        value = "5"
      }

      env {
        name  = "Simulation__DeliverySpeedMetersPerSecond"
        value = "8"
      }

      env {
        name  = "Simulation__DestinationArrivalThresholdMeters"
        value = "5"
      }
    }
  }
}
