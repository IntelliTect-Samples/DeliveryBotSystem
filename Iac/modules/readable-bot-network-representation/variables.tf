variable "resource_group_name" {
  description = "Name of the resource group where read model resources will be created."
  type        = string
}

variable "location" {
  description = "Azure region for the read model resources."
  type        = string
}

variable "name_prefix" {
  description = "Short project or workload prefix used in generated resource names."
  type        = string

  validation {
    condition     = length(var.name_prefix) >= 2 && length(var.name_prefix) <= 24
    error_message = "name_prefix must be between 2 and 24 characters."
  }
}

variable "environment" {
  description = "Environment label used in generated resource names and tags."
  type        = string
  default     = "dev"
}

variable "tags" {
  description = "Tags applied to all resources created by this module."
  type        = map(string)
  default     = {}
}

variable "eventhub_resource_group_name" {
  description = "Resource group containing the existing robot Event Hub namespace. Defaults to resource_group_name."
  type        = string
  default     = null
}

variable "eventhub_namespace_name" {
  description = "Name of the existing Event Hub namespace that contains the robot-output hub."
  type        = string
}

variable "robot_output_eventhub_name" {
  description = "Name of the existing Event Hub that receives simulator robot-output events."
  type        = string
  default     = "robot-output"
}

variable "eventhub_consumer_group_name" {
  description = "Consumer group used by the read model Function App."
  type        = string
  default     = "readable-bot-network"
}

variable "create_eventhub_consumer_group" {
  description = "Whether this module should create the Event Hub consumer group."
  type        = bool
  default     = true
}

variable "assign_eventhub_receiver_role" {
  description = "Whether to assign Azure Event Hubs Data Receiver to the Function App identity for robot-output."
  type        = bool
  default     = true
}

variable "cosmos_account_name" {
  description = "Optional explicit Cosmos DB account name. If omitted, a globally unique name is generated."
  type        = string
  default     = null
}

variable "cosmos_database_name" {
  description = "Cosmos DB SQL database name for the bot read model."
  type        = string
  default     = "bot-network"
}

variable "cosmos_container_name" {
  description = "Cosmos DB SQL container name for current bot documents."
  type        = string
  default     = "bots"
}

variable "cosmos_partition_key_paths" {
  description = "Partition key path list for the bot documents. Keep /botId to match the read model contract."
  type        = list(string)
  default     = ["/botId"]

  validation {
    condition     = length(var.cosmos_partition_key_paths) > 0
    error_message = "At least one Cosmos DB partition key path is required."
  }
}

variable "cosmos_enable_serverless" {
  description = "Enable Cosmos DB serverless capacity mode. Recommended for low-cost class/dev environments."
  type        = bool
  default     = true
}

variable "cosmos_database_throughput" {
  description = "Database-level RU/s throughput when serverless mode is disabled."
  type        = number
  default     = 400
}

variable "cosmos_consistency_level" {
  description = "Cosmos DB consistency level."
  type        = string
  default     = "Session"
}

variable "cosmos_free_tier_enabled" {
  description = "Enable Cosmos DB free tier. Only one account per subscription can use this."
  type        = bool
  default     = false
}

variable "assign_cosmos_data_contributor_role" {
  description = "Whether to assign Cosmos DB Built-in Data Contributor to the Function App managed identity."
  type        = bool
  default     = true
}

variable "function_app_name" {
  description = "Optional explicit Function App name. If omitted, a unique name is generated."
  type        = string
  default     = null
}

variable "service_plan_name" {
  description = "Optional explicit App Service plan name. If omitted, a unique name is generated."
  type        = string
  default     = null
}

variable "service_plan_sku_name" {
  description = "SKU for the Function App service plan. Y1 is Azure Functions consumption."
  type        = string
  default     = "Y1"
}

variable "storage_account_name" {
  description = "Optional explicit Function App storage account name. Must be globally unique, lowercase, and 3-24 alphanumeric characters."
  type        = string
  default     = null
}

variable "storage_account_replication_type" {
  description = "Replication type for the Function App storage account."
  type        = string
  default     = "LRS"
}

variable "log_analytics_workspace_name" {
  description = "Optional explicit Log Analytics workspace name. If omitted, a unique name is generated."
  type        = string
  default     = null
}

variable "application_insights_name" {
  description = "Optional explicit Application Insights name. If omitted, a unique name is generated."
  type        = string
  default     = null
}

variable "log_retention_in_days" {
  description = "Log Analytics retention period."
  type        = number
  default     = 30
}

variable "functions_extension_version" {
  description = "Azure Functions runtime extension version."
  type        = string
  default     = "~4"
}

variable "functions_worker_runtime" {
  description = "Azure Functions worker runtime."
  type        = string
  default     = "dotnet-isolated"
}

variable "function_dotnet_version" {
  description = "Dotnet version configured for the Linux Function App application stack."
  type        = string
  default     = "8.0"
}

variable "additional_app_settings" {
  description = "Additional Function App settings to merge into the default read model settings."
  type        = map(string)
  default     = {}
}
