variable "resource_group_name" {
  description = "Resource group that hosts the DeliveryBot resources."
  type        = string
  default     = "deliverybot-rg"
}

variable "location" {
  description = "Region for the shared App Service Plan and admin app."
  type        = string
  default     = "westus2"
}

variable "app_service_plan_id" {
  description = "Resource ID of the shared App Service Plan."
  type        = string
}

variable "app_service_name" {
  description = "Globally-unique name for the Admin Web App App Service."
  type        = string
  default     = "wa-deliverybot-admin-dev"
}

variable "node_version" {
  description = "Node runtime version used by the SPA host (pm2 serve)."
  type        = string
  default     = "22-lts"
}

variable "botnet_api_url" {
  description = "Public URL of the BotNet API baked into the SPA at build time."
  type        = string
  default     = "https://deliverybot-botapi-dev.example.com"
}

variable "simulator_api_url" {
  description = "Public URL of the Robot Simulator baked into the SPA at build time."
  type        = string
  default     = "https://deliverybot-simulator-dev.example.com"
}

variable "tags" {
  description = "Common tags applied to admin-owned resources."
  type        = map(string)
  default = {
    project   = "DeliveryBot"
    component = "admin-webapp"
  }
}
