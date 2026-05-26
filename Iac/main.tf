# ── Resource Group ─────────────────────────────────────────────────────────────

resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
}

# ── Azure Container Registry ───────────────────────────────────────────────────

resource "azurerm_container_registry" "acr" {
  name                = "DeliverybotCR"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Standard"
  admin_enabled       = true
}

# ── Log Analytics Workspace ────────────────────────────────────────────────────

resource "azurerm_log_analytics_workspace" "logs" {
  name                = "workspaceewudeliverybotsystemrg8609"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# ── Event Hub Namespace ────────────────────────────────────────────────────────

resource "azurerm_eventhub_namespace" "simulator" {
  name                = "DeliverybotSimulator-EVHNS"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  sku                 = "Standard"
  capacity            = 1
  zone_redundant      = true
}

resource "azurerm_eventhub" "robot_input" {
  name              = "robot-input"
  namespace_id      = azurerm_eventhub_namespace.simulator.id
  partition_count   = 1
  message_retention = 1
}

resource "azurerm_eventhub" "robot_output" {
  name              = "robot-output"
  namespace_id      = azurerm_eventhub_namespace.simulator.id
  partition_count   = 2
  message_retention = 1
}

# ── Container Apps Managed Environment ────────────────────────────────────────

resource "azurerm_container_app_environment" "env" {
  name                       = "managedEnvironment-ewudeliverybots-aa2f"
  resource_group_name        = azurerm_resource_group.rg.name
  location                   = azurerm_resource_group.rg.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.logs.id
}

# ── SQL Server ─────────────────────────────────────────────────────────────────

resource "azurerm_mssql_server" "sql" {
  name                = "deliverybotsystem-sql"
  resource_group_name = azurerm_resource_group.rg.name
  location            = var.sql_location
  version             = "12.0"

  # Azure AD-only authentication — no SQL login allowed.
  azuread_administrator {
    login_username              = var.sql_ad_admin_login
    object_id                   = var.sql_ad_admin_object_id
    tenant_id                   = var.tenant_id
    azuread_authentication_only = true
  }
}

# Allow Azure services (e.g. Container Apps) to reach the SQL server.
resource "azurerm_mssql_firewall_rule" "allow_azure_services" {
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.sql.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}

# ── SQL Databases ──────────────────────────────────────────────────────────────

# Serverless — auto-pauses when idle, scales vCores on demand.
resource "azurerm_mssql_database" "botnetapi_db" {
  name      = "BotNetApiDb"
  server_id = azurerm_mssql_server.sql.id
  sku_name  = "GP_S_Gen5_2"

  max_size_gb                 = 32
  min_capacity                = 0.5
  auto_pause_delay_in_minutes = 60
  zone_redundant              = false
}

# Provisioned General Purpose — always-on for the order service.
resource "azurerm_mssql_database" "order_service_db" {
  name      = "OrderServiceDb"
  server_id = azurerm_mssql_server.sql.id
  sku_name  = "GP_Gen5_2"

  max_size_gb = 2
}

# ── Container App: Bot API ─────────────────────────────────────────────────────

resource "azurerm_container_app" "bot_api" {
  name                         = "ewu-deliverybotsystem-api"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  # Pull images from ACR using admin credentials stored as a secret.
  secret {
    name  = "acr-password"
    value = azurerm_container_registry.acr.admin_password
  }

  secret {
    name  = "sql-connection-string"
    value = var.bot_api_sql_connection_string
  }

  registry {
    server               = azurerm_container_registry.acr.login_server
    username             = azurerm_container_registry.acr.admin_username
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
    max_replicas = 3

    container {
      name   = "botnetapi"
      image  = "${azurerm_container_registry.acr.login_server}/botnetapi:latest"
      cpu    = 0.5
      memory = "1Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      env {
        name        = "ConnectionStrings__DefaultConnection"
        secret_name = "sql-connection-string"
      }
    }
  }
}

# ── Container App: Robot Simulator ────────────────────────────────────────────

resource "azurerm_container_app" "robot_simulator" {
  name                         = "deliverybot-robot-simulator"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  secret {
    name  = "acr-password"
    value = azurerm_container_registry.acr.admin_password
  }

  secret {
    name  = "eventhub-connection-string"
    value = var.eventhub_connection_string
  }

  registry {
    server               = azurerm_container_registry.acr.login_server
    username             = azurerm_container_registry.acr.admin_username
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
      image  = "${azurerm_container_registry.acr.login_server}/deliverybot-robot-simulator:v1"
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
        value = azurerm_eventhub.robot_input.name
      }

      env {
        name  = "EventTransport__OutputEventHubName"
        value = azurerm_eventhub.robot_output.name
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

# ── Container App: Order Service ───────────────────────────────────────────────

resource "azurerm_container_app" "order_service" {
  name                         = "deliverybot-order-service"
  resource_group_name          = azurerm_resource_group.rg.name
  container_app_environment_id = azurerm_container_app_environment.env.id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
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
      name   = "order-service"
      image  = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
      cpu    = 0.5
      memory = "1Gi"
    }
  }
}
