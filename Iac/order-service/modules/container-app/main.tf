# Reusable Azure Container App module.
#
# Encapsulates the team's standard Container App shape: a system-assigned
# identity, ACR pull via an admin-password secret, external ingress, and a
# single container with configurable plain + secret-backed env vars.

resource "azurerm_container_app" "this" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_environment_id
  revision_mode                = "Single"
  tags                         = var.tags

  identity {
    type = "SystemAssigned"
  }

  # ACR pull credential (admin user), stored as a secret.
  secret {
    name  = "acr-password"
    value = var.acr_password
  }

  # Caller-supplied secrets (e.g. SQL / Event Hub connection strings).
  # Iterate the (non-sensitive) secret names and look up the sensitive values,
  # so the sensitive map isn't used directly as a for_each argument — Terraform
  # rejects that.
  dynamic "secret" {
    for_each = nonsensitive(toset(keys(var.secrets)))
    content {
      name  = secret.value
      value = var.secrets[secret.value]
    }
  }

  registry {
    server               = var.acr_login_server
    username             = var.acr_username
    password_secret_name = "acr-password"
  }

  ingress {
    external_enabled = true
    target_port      = var.target_port

    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = var.container_name
      image  = var.image
      cpu    = var.cpu
      memory = var.memory

      # Plain environment variables.
      dynamic "env" {
        for_each = var.env_vars
        content {
          name  = env.key
          value = env.value
        }
      }

      # Environment variables backed by a secret.
      dynamic "env" {
        for_each = var.secret_env_vars
        content {
          name        = env.key
          secret_name = env.value
        }
      }
    }
  }

  lifecycle {
    ignore_changes = [
      # The CD pipeline deploys new image tags per commit; let it own the
      # running image instead of reverting to the value above on every apply.
      template[0].container[0].image,
    ]
  }
}
