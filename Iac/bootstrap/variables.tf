variable "subscription_id" {
  description = "Azure subscription ID."
  type        = string
  default     = "a06983f7-7384-4a09-a092-b13a3896be85"
}

variable "resource_group_name" {
  description = "Resource group that holds the tfstate storage account (must already exist)."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "location" {
  description = "Azure region for the state storage account."
  type        = string
  default     = "westus2"
}
