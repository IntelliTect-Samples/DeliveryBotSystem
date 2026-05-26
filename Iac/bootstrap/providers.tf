terraform {
  required_version = ">= 1.6"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Bootstrap uses local state — it creates the remote backend used by everything else.
  # Do NOT add a remote backend block here.
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}
