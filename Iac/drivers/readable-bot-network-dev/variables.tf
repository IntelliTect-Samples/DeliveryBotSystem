variable "subscription_id" {
  description = "Azure subscription ID used for this temporary driver deployment."
  type        = string
  default     = "a06983f7-7384-4a09-a092-b13a3896be85"
}

variable "resource_group_name" {
  description = "Existing project resource group for this temporary deployment."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "location" {
  description = "Azure region for the temporary deployment."
  type        = string
  default     = "westus2"
}

variable "eventhub_namespace_name" {
  description = "Existing Event Hub namespace that contains robot-output."
  type        = string
  default     = "DeliverybotSimulator-EVHNS"
}

variable "robot_output_eventhub_name" {
  description = "Existing Event Hub where simulator output events are published."
  type        = string
  default     = "robot-output"
}
