# Root-level variable declarations.
#
# These are the "injection points" this file talks about: the root main.tf
# reads these values and passes them into each service module. Variables
# that are only used by one module use a descriptive prefix (e.g.
# admin_app_service_name) to avoid collisions; variables shared across
# multiple modules keep a simple name (e.g. resource_group_name).

# ── Shared infrastructure ──────────────────────────────────────────────────────

variable "resource_group_name" {
  description = "Resource group shared by all DeliveryBot resources."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "location" {
  description = "Primary Azure region (Container Apps, Event Hubs, etc.)."
  type        = string
  default     = "westus2"
}

variable "acr_name" {
  description = "Name of the shared Azure Container Registry."
  type        = string
  default     = "DeliverybotCR"
}

variable "container_app_environment_name" {
  description = "Name of the shared Container Apps managed environment."
  type        = string
  default     = "managedEnvironment-ewudeliverybots-aa2f"
}

variable "eventhub_namespace_name" {
  description = "Name of the shared Event Hub namespace."
  type        = string
  default     = "DeliverybotSimulator-EVHNS"
}

# ── Shared-infra specific ──────────────────────────────────────────────────────

variable "sql_location" {
  description = "Azure region for the SQL server (kept in southeastasia for cost/availability)."
  type        = string
  default     = "southeastasia"
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

# ── Shared App Service settings (admin-webapp + customer frontend) ─────────────

variable "app_service_plan_name" {
  description = "Existing App Service Plan shared by admin-webapp and customer frontend."
  type        = string
  default     = "ASP-RGDeliveryBotdev-8b82"
}

variable "node_version" {
  description = "Node runtime version used by pm2 in both web apps."
  type        = string
  default     = "22-lts"
}

# ── Shared API URLs (admin-webapp + order-service) ─────────────────────────────

variable "botnet_api_url" {
  description = "Public HTTPS URL of the BotNet API Container App."
  type        = string
  default     = "https://ewu-deliverybotsystem-api.mangocoast-332176b0.westus2.azurecontainerapps.io"
}

variable "simulator_api_url" {
  description = "Public HTTPS URL of the Robot Simulator Container App."
  type        = string
  default     = "https://deliverybot-robot-simulator.mangocoast-332176b0.westus2.azurecontainerapps.io"
}

# ── Admin Web App ──────────────────────────────────────────────────────────────

variable "admin_app_service_name" {
  description = "Name of the Admin Web App App Service."
  type        = string
  default     = "WA-DeliveryBot-Admin-dev"
}

# ── Order Service ──────────────────────────────────────────────────────────────

variable "order_service_container_app_name" {
  description = "Name of the Order Service Container App."
  type        = string
  default     = "deliverybot-order-service"
}

variable "order_service_sql_connection_string" {
  description = "SQL connection string for OrderServiceDb. Supplied via TF_VAR_order_service_sql_connection_string in CI — never committed."
  type        = string
  sensitive   = true
}

variable "eventhub_connection_string" {
  description = "Event Hub namespace connection string. Used by Order Service and Robot Simulator. Supplied via TF_VAR_eventhub_connection_string in CI — never committed."
  type        = string
  sensitive   = true
}

# ── Bot API ────────────────────────────────────────────────────────────────────

variable "bot_api_container_app_name" {
  description = "Name of the BotNet API Container App."
  type        = string
  default     = "ewu-deliverybotsystem-api"
}

variable "bot_api_sql_server_name" {
  description = "Name of the shared SQL server used by the Bot API."
  type        = string
  default     = "deliverybotsystem-sql"
}

variable "bot_api_sql_connection_string" {
  description = "SQL connection string for BotNetApiDb. Uses Managed Identity auth — no password in the string."
  type        = string
  sensitive   = true
  default     = "Server=tcp:deliverybotsystem-sql.database.windows.net,1433;Initial Catalog=BotNetApiDb;Authentication=Active Directory Managed Identity;"
}

# ── Customer Frontend ──────────────────────────────────────────────────────────

variable "customer_frontend_app_service_name" {
  description = "Name of the Customer Frontend App Service."
  type        = string
  default     = "WA-DeliveryBot-dev"
}

# ── Robot Simulator ────────────────────────────────────────────────────────────

variable "simulator_container_app_name" {
  description = "Name of the Robot Simulator Container App."
  type        = string
  default     = "deliverybot-robot-simulator"
}
