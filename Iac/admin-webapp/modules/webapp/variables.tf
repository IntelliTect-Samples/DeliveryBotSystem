variable "resource_group_name" {
  description = "Resource group that hosts the team's DeliveryBot resources."
  type        = string
}

variable "app_service_plan_name" {
  description = "Existing App Service Plan to reuse (shared with the Customer site to keep cost down)."
  type        = string
}

variable "app_service_name" {
  description = "Globally-unique name for the App Service."
  type        = string
}

variable "node_version" {
  description = "Node runtime version used by the SPA host (pm2 serve)."
  type        = string
}

variable "botnet_api_url" {
  description = "Public URL of the BotNet API (Container App), baked into the SPA at build time."
  type        = string
}

variable "simulator_api_url" {
  description = "Public URL of the Robot Simulator (Container App), baked into the SPA at build time."
  type        = string
}

variable "tags" {
  description = "Common tags applied to the App Service."
  type        = map(string)
  default     = {}
}
