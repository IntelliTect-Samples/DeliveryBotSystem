variable "resource_group_name" {
  description = "Name of the shared resource group."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "location" {
  description = "Primary Azure region for shared resources."
  type        = string
  default     = "westus2"
}

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
