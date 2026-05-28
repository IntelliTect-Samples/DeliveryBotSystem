# ── SQL Database ───────────────────────────────────────────────────────────────

# Serverless — auto-pauses when idle, scales vCores on demand.
resource "azurerm_mssql_database" "botnetapi_db" {
  name      = "BotNetApiDb"
  server_id = var.sql_server_id
  sku_name  = "GP_S_Gen5_2"

  max_size_gb                 = 32
  min_capacity                = 0.5
  auto_pause_delay_in_minutes = 60
  zone_redundant              = false
}

# ── Container App ──────────────────────────────────────────────────────────────

resource "azurerm_container_app" "bot_api" {
  name                         = "ewu-deliverybotsystem-api"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"

  identity {
    type = "SystemAssigned"
  }

  secret {
    name  = "acr-password"
    value = var.acr_admin_password
  }

  secret {
    name  = "sql-connection-string"
    value = var.bot_api_sql_connection_string
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
    max_replicas = 3

    container {
      name   = "botnetapi"
      image  = "${var.acr_login_server}/botnetapi:latest"
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
