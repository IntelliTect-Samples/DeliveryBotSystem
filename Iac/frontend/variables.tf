variable "resource_group_name" {
  description = "Resource group that hosts the team's DeliveryBot resources."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "app_service_plan_name" {
  description = "Existing App Service Plan to reuse (shared with the Admin site)."
  type        = string
  default     = "ASP-RGDeliveryBotdev-8b82"
}

variable "app_service_name" {
  description = "Globally-unique name for the Customer Frontend App Service."
  type        = string
  default     = "WA-DeliveryBot-dev"
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
