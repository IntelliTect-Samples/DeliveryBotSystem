variable "resource_group_name" {
  description = "Resource group shared by all DeliveryBot resources."
  type        = string
  default     = "deliverybot-rg"
}

variable "location" {
  description = "Primary Azure region for container-based and shared app resources."
  type        = string
  default     = "westus2"
}

variable "eventhub_location" {
  description = "Azure region for the Event Hubs namespace."
  type        = string
  default     = "centralus"
}

variable "acr_name" {
  description = "Name of the shared Azure Container Registry."
  type        = string
  default     = "deliverybotdevcr"
}

variable "container_app_environment_name" {
  description = "Name of the shared Container Apps managed environment."
  type        = string
  default     = "deliverybot-dev-cae"
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
  default     = "deliverybot-dev-evhns"
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

variable "sql_location" {
  description = "Azure region for the SQL server."
  type        = string
  default     = "centralus"
}

variable "sql_ad_admin_login" {
  description = "UPN of the Azure AD user set as SQL server administrator."
  type        = string
  default     = "wmiller17@ewu.edu"
}

variable "sql_ad_admin_object_id" {
  description = "Object ID of the Azure AD SQL administrator."
  type        = string
  default     = "0b83fd03-d44e-4731-8ee0-790b50b715db"
}

variable "tenant_id" {
  description = "Azure Active Directory tenant ID."
  type        = string
  default     = "37321907-14a5-4390-987d-ec0c66c655cd"
}

variable "app_service_plan_name" {
  description = "Name of the shared App Service Plan used by both web apps."
  type        = string
  default     = "asp-deliverybot-dev"
}

variable "app_service_plan_sku_name" {
  description = "SKU for the shared App Service Plan."
  type        = string
  default     = "B1"
}

variable "app_service_plan_location" {
  description = "Region for web apps attached to the shared App Service Plan."
  type        = string
  default     = "canadacentral"
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

variable "node_version" {
  description = "Node runtime version used by pm2 in both web apps."
  type        = string
  default     = "22-lts"
}

variable "botnet_api_url" {
  description = "Public HTTPS URL of the BotNet API Container App."
  type        = string
  default     = "https://deliverybot-botapi-dev.example.com"
}

variable "simulator_api_url" {
  description = "Public HTTPS URL of the Robot Simulator Container App."
  type        = string
  default     = "https://deliverybot-simulator-dev.example.com"
}

variable "admin_app_service_name" {
  description = "Name of the Admin Web App App Service."
  type        = string
  default     = "wa-deliverybot-admin-dev"
}

variable "order_service_container_app_name" {
  description = "Name of the Order Service Container App."
  type        = string
  default     = "deliverybot-orders-dev"
}

variable "agent_service_container_app_name" {
  description = "Name of the Agent Service Container App."
  type        = string
  default     = "deliverybot-agent-dev"
}

variable "order_service_sql_connection_string" {
  description = "SQL connection string for OrderServiceDb. Supplied via TF_VAR_order_service_sql_connection_string in CI."
  type        = string
  sensitive   = true
}

variable "eventhub_connection_string" {
  description = "Event Hub namespace connection string used by the Order Service and Robot Simulator."
  type        = string
  sensitive   = true
}

variable "azure_openai_endpoint" {
  description = "Azure OpenAI resource endpoint used by the Agent Service."
  type        = string
}

variable "azure_openai_deployment" {
  description = "Azure OpenAI deployment name used by the Agent Service."
  type        = string
}

variable "azure_openai_api_key" {
  description = "Azure OpenAI API key used by the Agent Service."
  type        = string
  sensitive   = true
}

variable "azure_openai_api_version" {
  description = "Azure OpenAI API version used by the Agent Service."
  type        = string
  default     = "2024-10-21"
}

variable "readable_bot_network_name_prefix" {
  description = "Short prefix used in generated resource names for the readable bot network resources."
  type        = string
  default     = "deliverybot"
}

variable "readable_bot_network_environment" {
  description = "Environment label used in generated resource names and tags for the readable bot network resources."
  type        = string
  default     = "dev"
}

variable "readable_bot_network_eventhub_resource_group_name" {
  description = "Optional resource group containing the robot Event Hub namespace. Defaults to resource_group_name."
  type        = string
  default     = null
}

variable "readable_bot_network_robot_output_eventhub_name" {
  description = "Robot output Event Hub consumed by the readable bot network projection."
  type        = string
  default     = "robot-output"
}

variable "readable_bot_network_consumer_group_name" {
  description = "Consumer group name used by the readable bot network Function App."
  type        = string
  default     = "readable-bot-network"
}

variable "readable_bot_network_create_eventhub_consumer_group" {
  description = "Whether Terraform should create the readable bot network Event Hub consumer group."
  type        = bool
  default     = true
}

variable "readable_bot_network_assign_eventhub_receiver_role" {
  description = "Whether Terraform should assign Azure Event Hubs Data Receiver to the readable bot network Function App identity."
  type        = bool
  default     = true
}

variable "readable_bot_network_assign_cosmos_data_contributor_role" {
  description = "Whether Terraform should assign Cosmos DB Built-in Data Contributor to the readable bot network Function App identity."
  type        = bool
  default     = true
}

variable "readable_bot_network_cosmos_account_name" {
  description = "Optional explicit Cosmos DB account name for the readable bot network."
  type        = string
  default     = null
}

variable "readable_bot_network_cosmos_database_name" {
  description = "Cosmos DB SQL database name for the readable bot network."
  type        = string
  default     = "bot-network"
}

variable "readable_bot_network_cosmos_container_name" {
  description = "Cosmos DB SQL container name for the current bot read model."
  type        = string
  default     = "bots"
}

variable "readable_bot_network_diagnostics_container_name" {
  description = "Cosmos DB SQL container name for projection diagnostics."
  type        = string
  default     = "function-diagnostics"
}

variable "readable_bot_network_function_app_name" {
  description = "Optional explicit Function App name for the readable bot network."
  type        = string
  default     = null
}

variable "readable_bot_network_service_plan_name" {
  description = "Optional explicit App Service plan name for the readable bot network Function App."
  type        = string
  default     = null
}

variable "readable_bot_network_storage_account_name" {
  description = "Optional explicit storage account name for the readable bot network Function App."
  type        = string
  default     = null
}

variable "readable_bot_network_log_analytics_workspace_name" {
  description = "Optional explicit Log Analytics workspace name for the readable bot network resources."
  type        = string
  default     = null
}

variable "readable_bot_network_application_insights_name" {
  description = "Optional explicit Application Insights name for the readable bot network resources."
  type        = string
  default     = null
}

variable "bot_api_container_app_name" {
  description = "Name of the BotNet API Container App."
  type        = string
  default     = "deliverybot-botapi-dev"
}

variable "bot_api_sql_server_name" {
  description = "Name of the shared SQL server used by the Bot API."
  type        = string
  default     = "deliverybot-dev-sql"
}

variable "bot_api_sql_connection_string" {
  description = "SQL connection string for BotNetApiDb. Uses Managed Identity auth."
  type        = string
  sensitive   = true
  default     = "Server=tcp:deliverybot-dev-sql.database.windows.net,1433;Initial Catalog=BotNetApiDb;Authentication=Active Directory Managed Identity;"
}

variable "customer_frontend_app_service_name" {
  description = "Name of the Customer Frontend App Service."
  type        = string
  default     = "wa-deliverybot-dev"
}

variable "simulator_container_app_name" {
  description = "Name of the Robot Simulator Container App."
  type        = string
  default     = "deliverybot-simulator-dev"
}
