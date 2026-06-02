variable "resource_group_name" {
  description = "Resource group that hosts the team's DeliveryBot resources."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "container_app_environment_name" {
  description = "Existing shared Container App Environment (created by the root Iac)."
  type        = string
  default     = "managedEnvironment-ewudeliverybots-aa2f"
}

variable "acr_name" {
  description = "Existing shared Azure Container Registry the image is pulled from."
  type        = string
  default     = "DeliverybotCR"
}

variable "container_app_name" {
  description = "Name of the Order Service Container App."
  type        = string
  default     = "deliverybot-order-service"
}

variable "image_name" {
  description = "Repository name of the Order Service image in ACR (tag is managed by the CD pipeline)."
  type        = string
  default     = "orderservice"
}

variable "botnet_api_url" {
  description = "Base URL of the BotNet API the Order Service calls to select a bot."
  type        = string
  default     = "https://ewu-deliverybotsystem-api.mangocoast-332176b0.westus2.azurecontainerapps.io"
}

variable "sql_connection_string" {
  description = "Connection string for OrderServiceDb. Uses Managed Identity auth — passed in from the CD pipeline, never committed."
  type        = string
  sensitive   = true
}

variable "eventhub_connection_string" {
  description = "Connection string for the robot-input Event Hub — passed in from the CD pipeline, never committed."
  type        = string
  sensitive   = true
}

variable "tags" {
  description = "Common tags applied to Order Service resources."
  type        = map(string)
  default = {
    project   = "DeliveryBot"
    component = "order-service"
    owner     = "npcjake"
    issue     = "#43"
  }
}
