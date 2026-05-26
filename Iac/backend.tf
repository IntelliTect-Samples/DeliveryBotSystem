terraform {
  backend "azurerm" {
    resource_group_name  = "ewu-deliverybotsystem-rg"
    storage_account_name = "dbstfstate01"
    container_name       = "tfstate"
    key                  = "deliverybotsystem.tfstate"
  }
}
