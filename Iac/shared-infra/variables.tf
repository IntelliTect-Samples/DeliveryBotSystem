variable "resource_group_name" {
  description = "Name of the shared resource group."
  type        = string
  default     = "deliverybot-rg"
}

variable "location" {
  description = "Primary Azure region for shared application platform resources."
  type        = string
  default     = "westus2"
}

variable "eventhub_location" {
  description = "Azure region for the Event Hubs namespace."
  type        = string
  default     = "centralus"
}

variable "sql_location" {
  description = "Azure region for the SQL server."
  type        = string
  default     = "centralus"
}

variable "acr_name" {
  description = "Name of the shared Azure Container Registry."
  type        = string
}

variable "app_service_plan_name" {
  description = "Name of the shared App Service Plan used by the web apps."
  type        = string
}

variable "app_service_plan_sku_name" {
  description = "SKU for the shared App Service Plan."
  type        = string
  default     = "B1"
}

variable "create_app_service_plan" {
  description = "Whether to create the shared App Service Plan in this stack."
  type        = bool
  default     = true
}

variable "existing_app_service_plan_resource_group_name" {
  description = "Optional resource group for an existing shared App Service Plan. Defaults to resource_group_name."
  type        = string
  default     = null
}

variable "container_app_environment_name" {
  description = "Name of the shared Container Apps managed environment."
  type        = string
}

variable "create_container_app_environment" {
  description = "Whether to create the shared Container Apps managed environment in this stack."
  type        = bool
  default     = true
}

variable "existing_container_app_environment_resource_group_name" {
  description = "Optional resource group for an existing Container Apps managed environment. Defaults to resource_group_name."
  type        = string
  default     = null
}

variable "eventhub_namespace_name" {
  description = "Name of the shared Event Hub namespace."
  type        = string
}

variable "robot_input_partition_count" {
  description = "Partition count for the robot-input Event Hub."
  type        = number
  default     = 2
}

variable "robot_output_partition_count" {
  description = "Partition count for the robot-output Event Hub."
  type        = number
  default     = 2
}

variable "robot_input_message_retention" {
  description = "Message retention in days for the robot-input Event Hub."
  type        = number
  default     = 7
}

variable "robot_output_message_retention" {
  description = "Message retention in days for the robot-output Event Hub."
  type        = number
  default     = 7
}

variable "log_analytics_workspace_name" {
  description = "Optional explicit Log Analytics workspace name for shared resources."
  type        = string
  default     = null
}

variable "sql_server_name" {
  description = "Name of the shared SQL server."
  type        = string
}

variable "sql_ad_admin_login" {
  description = "UPN of the Azure AD user set as SQL server administrator."
  type        = string
}

variable "sql_ad_admin_object_id" {
  description = "Object ID of the Azure AD SQL administrator."
  type        = string
}

variable "tenant_id" {
  description = "Azure Active Directory tenant ID."
  type        = string
}