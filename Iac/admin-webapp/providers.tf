# Provider + state backend for the Admin Web App App Service.
#
# Auth: assumed to be set by the surrounding GitHub Actions workflow via
# `azure/login@v2` (OIDC) and the ARM_USE_OIDC / ARM_USE_AZUREAD env vars.
# State: lives in the team's pre-existing storage account `dbstfstate01`
# under a unique key so we don't collide with Bill's root-level Iac (#74).

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
    key                  = "admin-webapp.tfstate"
    use_oidc             = true
    use_azuread_auth     = true
  }
}

provider "azurerm" {
  features {}
  use_oidc = true

  # The CI service principal has scoped roles (RG Contributor + Blob Data
  # Contributor) but no subscription-level resource-provider registration
  # rights. Microsoft.Web is already registered for the subscription, so skip
  # the provider's default auto-registration to avoid a 403 on apply.
  resource_provider_registrations = "none"
}
