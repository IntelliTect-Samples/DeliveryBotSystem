variable "resource_group_name" {
  description = "Name of the resource group."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID of the Container Apps managed environment."
  type        = string
}

variable "sql_server_id" {
  description = "ID of the SQL server."
  type        = string
}

variable "acr_login_server" {
  description = "ACR login server hostname."
  type        = string
}

variable "acr_admin_username" {
  description = "ACR admin username."
  type        = string
}

variable "acr_admin_password" {
  description = "ACR admin password."
  type        = string
  sensitive   = true
}

variable "bot_api_sql_connection_string" {
  description = "SQL connection string injected into the bot API container app."
  type        = string
  sensitive   = true
}
