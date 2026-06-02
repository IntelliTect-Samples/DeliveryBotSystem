# Reusable module: a Linux App Service that hosts a static SPA via pm2.
#
# Reuses an existing resource group and App Service Plan (passed by name) so
# the team isn't billed for a duplicate plan. The only managed resource is the
# App Service itself.

terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

data "azurerm_service_plan" "plan" {
  name                = var.app_service_plan_name
  resource_group_name = data.azurerm_resource_group.rg.name
}

resource "azurerm_linux_web_app" "admin" {
  name                = var.app_service_name
  resource_group_name = data.azurerm_resource_group.rg.name
  location            = data.azurerm_service_plan.plan.location
  service_plan_id     = data.azurerm_service_plan.plan.id
  https_only          = true

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on        = false
    app_command_line = "pm2 serve /home/site/wwwroot --no-daemon --spa"

    application_stack {
      node_version = var.node_version
    }

    # Allow the GitHub Actions workflow to push builds.
    scm_use_main_ip_restriction = true
  }

  # Build-time URLs are baked into the SPA bundle, so these app settings
  # exist mainly as a record of which upstreams this deployment talks to.
  # If the SPA gains a runtime config layer, switch to reading these.
  app_settings = {
    "WEBSITE_NODE_DEFAULT_VERSION" = "~22"
    "BOTNET_API_URL"               = var.botnet_api_url
    "SIMULATOR_API_URL"            = var.simulator_api_url
  }

  tags = var.tags

  lifecycle {
    ignore_changes = [
      # Deployments overwrite the build artifact; don't fight the workflow.
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
    ]
  }
}
