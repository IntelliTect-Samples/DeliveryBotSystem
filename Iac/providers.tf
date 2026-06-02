# Universal root provider.
#
# All service modules (admin-webapp, order-service, bot-api, frontend,
# shared-infra, simulator) are called from Iac/main.tf and share this single
# azurerm provider. No provider block is needed inside the modules.
#
# Auth: GitHub Actions OIDC federated identity — no client secrets stored.
# Backend: see backend.tf.

terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }
}

provider "azurerm" {
  features {}
  use_oidc = true

  # The CI service principal has scoped roles but no subscription-level
  # resource-provider registration rights. All providers are already registered.
  resource_provider_registrations = "none"
}
