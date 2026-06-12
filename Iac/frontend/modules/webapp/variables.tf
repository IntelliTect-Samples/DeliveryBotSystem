variable "resource_group_name" {
  description = "Resource group that hosts the DeliveryBot resources."
  type        = string
}

variable "location" {
  description = "Region for the App Service Plan and frontend app."
  type        = string
}

variable "app_service_plan_id" {
  description = "Resource ID of the shared App Service Plan."
  type        = string
}

variable "app_service_name" {
  description = "Globally-unique name for the App Service."
  type        = string
}

variable "node_version" {
  description = "Node runtime version used by the SPA host (pm2 serve)."
  type        = string
  default     = "22-lts"
}

variable "tags" {
  description = "Tags applied to the App Service."
  type        = map(string)
  default     = {}
}
