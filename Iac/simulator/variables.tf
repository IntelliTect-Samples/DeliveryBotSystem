variable "resource_group_name" {
  description = "Name of the existing resource group that contains all simulator resources."
  type        = string
}

variable "location" {
  description = "Azure region of the resource group."
  type        = string
}

variable "container_app_env_name" {
  description = "Name of the existing Container Apps managed environment."
  type        = string
}

variable "acr_name" {
  description = "Name of the existing Azure Container Registry (without .azurecr.io)."
  type        = string
}

variable "event_hub_namespace_name" {
  description = "Name of the existing Event Hub namespace used by the simulator."
  type        = string
}

variable "eventhub_connection_string" {
  description = "Event Hub namespace connection string. Passed in via TF_VAR_eventhub_connection_string; never committed."
  type        = string
  sensitive   = true
}

variable "container_app_name" {
  description = "Name of the simulator Container App."
  type        = string
  default     = "deliverybot-robot-simulator"
}

variable "image_name" {
  description = "Container image name (without registry prefix or tag)."
  type        = string
  default     = "deliverybot-robot-simulator"
}

variable "image_tag" {
  description = "Container image tag. Typically the deploying commit SHA."
  type        = string
  default     = "latest"
}

# ── Simulator configuration ─────────────────────────────────────────────────

variable "event_transport_mode" {
  description = "Transport mode for the simulator (AzureEventHub or InMemory)."
  type        = string
  default     = "AzureEventHub"
}

variable "event_transport_input_hub" {
  description = "Name of the robot-input event hub."
  type        = string
  default     = "robot-input"
}

variable "event_transport_output_hub" {
  description = "Name of the robot-output event hub."
  type        = string
  default     = "robot-output"
}

variable "event_transport_consumer_group" {
  description = "Event Hub consumer group used by the simulator."
  type        = string
  default     = "$Default"
}

variable "simulator_initial_bot_count" {
  description = "Number of bots spawned on simulator startup."
  type        = number
  default     = 3
}

variable "simulator_bot_id_prefix" {
  description = "Prefix applied to generated bot IDs."
  type        = string
  default     = "bot"
}

variable "simulator_default_bot_model" {
  description = "Default bot model name."
  type        = string
  default     = "DeliveryBot-V1"
}

variable "simulator_default_latitude" {
  description = "Default starting latitude for bots."
  type        = string
  default     = "47.65837359646208"
}

variable "simulator_default_longitude" {
  description = "Default starting longitude for bots."
  type        = string
  default     = "-117.40215401730164"
}

variable "simulation_tick_interval_seconds" {
  description = "Simulation tick interval in seconds."
  type        = number
  default     = 1
}

variable "simulation_telemetry_interval_seconds" {
  description = "How often (seconds) bots emit telemetry events."
  type        = number
  default     = 5
}

variable "simulation_delivery_speed_mps" {
  description = "Bot travel speed in metres per second."
  type        = number
  default     = 8
}

variable "simulation_arrival_threshold_meters" {
  description = "Distance in metres at which a bot is considered to have arrived."
  type        = number
  default     = 5
}
