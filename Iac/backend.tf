# Terraform remote state backend.
#
# State is stored in the pre-existing Azure Blob Storage account:
#   Storage account : dbstfstate01
#   Resource group  : ewu-deliverybotsystem-rg
#   Container       : tfstate
#   Blob key        : deliverybot.tfstate
#
# Auth uses OIDC + Azure AD (no SAS tokens or storage keys).
# The storage account and container were verified via:
#   az storage account show --name dbstfstate01
#   az storage container list --account-name dbstfstate01 --auth-mode login

terraform {
  backend "azurerm" {
    resource_group_name  = "ewu-deliverybotsystem-rg"
    storage_account_name = "dbstfstate01"
    container_name       = "tfstate"
    key                  = "deliverybot.tfstate"
    use_oidc             = true
    use_azuread_auth     = true
  }
}
