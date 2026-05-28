variable "resource_group_name" {
  description = "Name of the resource group."
  type        = string
}

variable "location" {
  description = "Primary Azure region."
  type        = string
}

variable "sql_location" {
  description = "Azure region for the SQL server."
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
