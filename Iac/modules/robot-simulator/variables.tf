variable "resource_group_name" {
  description = "Name of the resource group."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID of the Container Apps managed environment."
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

variable "eventhub_connection_string" {
  description = "Event Hub namespace connection string used by the robot simulator."
  type        = string
  sensitive   = true
}

variable "robot_input_hub_name" {
  description = "Name of the robot-input event hub."
  type        = string
}

variable "robot_output_hub_name" {
  description = "Name of the robot-output event hub."
  type        = string
}
