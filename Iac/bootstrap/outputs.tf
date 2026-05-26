output "storage_account_name" {
  description = "Name of the Terraform state storage account."
  value       = azurerm_storage_account.tfstate.name
}

output "container_name" {
  description = "Blob container that holds .tfstate files."
  value       = azurerm_storage_container.tfstate.name
}

output "resource_group_name" {
  description = "Resource group containing the state storage account."
  value       = data.azurerm_resource_group.rg.name
}
