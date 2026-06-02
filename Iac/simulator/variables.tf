variable "resource_group_name" {
  description = "Name of the existing resource group that contains all simulator resources."
  type        = string
}

variable "location" {
  description = "Azure region of the resource group."
  type        = string
}

variable "container_app_env_name" {
  description = "Name of the existing Container Apps managed environment."
  type        = string
}

variable "acr_name" {
  description = "Name of the existing Azure Container Registry (without .azurecr.io)."
  type        = string
}

variable "event_hub_namespace_name" {
  description = "Name of the existing Event Hub namespace used by the simulator."
  type        = string
}

variable "container_app_name" {
  description = "Name of the simulator Container App."
  type        = string
  default     = "deliverybot-robot-simulator"
}

variable "image_name" {
  description = "Container image name (without registry prefix or tag)."
  type        = string
  default     = "deliverybot-robot-simulator"
}

variable "image_tag" {
  description = "Container image tag. Typically the deploying commit SHA."
  type        = string
  default     = "latest"
}
