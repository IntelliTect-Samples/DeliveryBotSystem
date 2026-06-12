variable "resource_group_name" {
  description = "Resource group that hosts the DeliveryBot resources."
  type        = string
  default     = "deliverybot-rg"
}

variable "location" {
  description = "Region for the shared App Service Plan and frontend app."
  type        = string
  default     = "westus2"
}

variable "app_service_plan_id" {
  description = "Resource ID of the shared App Service Plan."
  type        = string
}

variable "app_service_name" {
  description = "Globally-unique name for the Customer Frontend App Service."
  type        = string
  default     = "wa-deliverybot-dev"
}

variable "node_version" {
  description = "Node runtime version used by the SPA host (pm2 serve)."
  type        = string
  default     = "22-lts"
}

variable "tags" {
  description = "Common tags applied to frontend resources."
  type        = map(string)
  default = {
    project   = "DeliveryBot"
    component = "frontend"
  }
}
