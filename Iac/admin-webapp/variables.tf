variable "resource_group_name" {
  description = "Resource group that hosts the team's DeliveryBot resources."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "app_service_plan_name" {
  description = "Existing App Service Plan to reuse (shared with the Customer site to keep cost down)."
  type        = string
  default     = "ASP-RGDeliveryBotdev-8b82"
}

variable "app_service_name" {
  description = "Globally-unique name for the Admin Web App App Service."
  type        = string
  default     = "WA-DeliveryBot-Admin-dev"
}

variable "location" {
  description = "Region for the App Service. Must match the existing plan."
  type        = string
  default     = "canadacentral"
}

variable "node_version" {
  description = "Node runtime version used by the SPA host (pm2 serve)."
  type        = string
  default     = "22-lts"
}

variable "botnet_api_url" {
  description = "Public URL of the BotNet API (Container App), baked into the SPA at build time."
  type        = string
  default     = "https://ewu-deliverybotsystem-api.mangocoast-332176b0.westus2.azurecontainerapps.io"
}

variable "simulator_api_url" {
  description = "Public URL of the Robot Simulator (Container App), baked into the SPA at build time."
  type        = string
  default     = "https://deliverybot-robot-simulator.mangocoast-332176b0.westus2.azurecontainerapps.io"
}

variable "tags" {
  description = "Common tags applied to admin-owned resources."
  type        = map(string)
  default = {
    project   = "DeliveryBot"
    component = "admin-webapp"
    owner     = "CarsonL15"
    issue     = "#18"
  }
}
