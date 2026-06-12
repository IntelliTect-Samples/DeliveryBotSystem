variable "resource_group_name" {
  description = "Resource group that hosts the team's DeliveryBot resources."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "container_app_environment_name" {
  description = "Existing shared Container App Environment (managed by shared-infra)."
  type        = string
  default     = "deliverybot-dev-cae"
}

variable "acr_name" {
  description = "Existing shared Azure Container Registry (managed by shared-infra)."
  type        = string
  default     = "deliverybotdevcr"
}

variable "sql_server_name" {
  description = "Existing shared SQL server (managed by shared-infra)."
  type        = string
  default     = "deliverybot-dev-sql"
}

variable "container_app_name" {
  description = "Name of the Bot API Container App."
  type        = string
  default     = "ewu-deliverybotsystem-api"
}

variable "image_name" {
  description = "Repository name of the Bot API image in ACR (tag is managed by the CD pipeline)."
  type        = string
  default     = "botnetapi"
}

variable "sql_connection_string" {
  description = "SQL connection string for BotNetApiDb. Uses Managed Identity auth — passed in from the CD pipeline, never committed."
  type        = string
  sensitive   = true
  default     = "Server=tcp:deliverybot-dev-sql.database.windows.net,1433;Initial Catalog=BotNetApiDb;Authentication=Active Directory Managed Identity;"
}

variable "tags" {
  description = "Common tags applied to Bot API resources."
  type        = map(string)
  default = {
    project   = "DeliveryBot"
    component = "bot-api"
    owner     = "wmiller17"
  }
}

