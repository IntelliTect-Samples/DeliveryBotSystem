# ── SQL Database ───────────────────────────────────────────────────────────────

# Provisioned General Purpose — always-on for the order service.
resource "azurerm_mssql_database" "order_service_db" {
  name      = "OrderServiceDb"
  server_id = var.sql_server_id
  sku_name  = "GP_Gen5_2"

  max_size_gb = 2
}

# ── Container App ──────────────────────────────────────────────────────────────

resource "azurerm_container_app" "order_service" {
  name                         = "deliverybot-order-service"
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
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
