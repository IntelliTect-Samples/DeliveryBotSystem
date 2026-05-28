# Provider + state backend for the Order Service Container App.
#
# Auth: provided by the GitHub Actions workflow via `azure/login@v2` (OIDC)
# and the ARM_USE_OIDC / ARM_USE_AZUREAD env vars — no secrets stored here.
# State: lives in the team's pre-existing storage account `dbstfstate01`
# under a unique key so it doesn't collide with the other features' state.

terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  backend "azurerm" {
    resource_group_name  = "ewu-deliverybotsystem-rg"
    storage_account_name = "dbstfstate01"
    container_name       = "tfstate"
    key                  = "order-service.tfstate"
    use_oidc             = true
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  features {}
  use_oidc = true
}
