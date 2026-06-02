# Provider + state backend for the Robot Simulator Container App.
#
# Auth: GitHub Actions OIDC federated identity — no client secrets stored.
# State: key "simulator.tfstate" in the team's shared dbstfstate01 account.

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
    key                  = "simulator.tfstate"
    use_oidc             = true
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  features {}
  use_oidc = true

  resource_provider_registrations = "none"
}
