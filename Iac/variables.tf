# ── Identity ───────────────────────────────────────────────────────────────────

variable "subscription_id" {
  description = "Azure subscription ID."
  type        = string
  default     = "a06983f7-7384-4a09-a092-b13a3896be85"
}

variable "tenant_id" {
  description = "Azure Active Directory tenant ID."
  type        = string
  default     = "37321907-14a5-4390-987d-ec0c66c655cd"
}

# ── Locations ─────────────────────────────────────────────────────────────────

variable "location" {
  description = "Primary Azure region for most resources."
  type        = string
  default     = "westus2"
}

variable "sql_location" {
  description = "Azure region for the SQL server and databases."
  type        = string
  default     = "southeastasia"
}

# ── Resource Group ─────────────────────────────────────────────────────────────

variable "resource_group_name" {
  description = "Name of the shared resource group."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

# ── SQL ────────────────────────────────────────────────────────────────────────

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

# ── Secrets (sensitive — supply via environment variables or a .tfvars file) ──

variable "eventhub_connection_string" {
  description = "Event Hub namespace connection string used by the robot simulator."
  type        = string
  sensitive   = true
}

variable "bot_api_sql_connection_string" {
  description = "SQL connection string injected into the bot API container app."
  type        = string
  sensitive   = true
  default     = "Server=tcp:deliverybotsystem-sql.database.windows.net,1433;Initial Catalog=BotNetApiDb;Authentication=Active Directory Managed Identity;"
}
