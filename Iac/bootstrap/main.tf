# Bootstrap — creates the Azure Storage Account used as the Terraform remote backend
# for all other configurations in this project.
#
# Usage (run once per environment):
#   cd Iac/bootstrap
#   terraform init
#   terraform apply
#
# The resource group must already exist before running this.

data "azurerm_resource_group" "rg" {
  name = var.resource_group_name
}

# ── State Storage Account ──────────────────────────────────────────────────────
resource "azurerm_storage_account" "tfstate" {
  name                     = "dbstfstate01"
  resource_group_name      = data.azurerm_resource_group.rg.name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  allow_nested_items_to_be_public = false

  blob_properties {
    versioning_enabled = true
  }
}

# ── Blob Container ─────────────────────────────────────────────────────────────
resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_id    = azurerm_storage_account.tfstate.id
  container_access_type = "private"
}
