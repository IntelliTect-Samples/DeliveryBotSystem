variable "name" {
  description = "Name of the Container App."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group that hosts the Container App."
  type        = string
}

variable "container_app_environment_id" {
  description = "Managed environment resource ID."
  type        = string
}

variable "acr_login_server" {
  description = "Azure Container Registry login server."
  type        = string
}

variable "acr_username" {
  description = "Azure Container Registry admin username."
  type        = string
}

variable "acr_password" {
  description = "Azure Container Registry admin password."
  type        = string
  sensitive   = true
}

variable "container_name" {
  description = "Container name inside the app template."
  type        = string
}

variable "image" {
  description = "Container image reference."
  type        = string
}

variable "target_port" {
  description = "Public ingress target port."
  type        = number
}

variable "cpu" {
  description = "CPU allocated to the container."
  type        = number
  default     = 0.5
}

variable "memory" {
  description = "Memory allocated to the container."
  type        = string
  default     = "1Gi"
}

variable "min_replicas" {
  description = "Minimum replica count."
  type        = number
  default     = 0
}

variable "max_replicas" {
  description = "Maximum replica count."
  type        = number
  default     = 1
}

variable "env_vars" {
  description = "Plain environment variables."
  type        = map(string)
  default     = {}
}

variable "secret_env_vars" {
  description = "Environment variables that reference secrets."
  type        = map(string)
  default     = {}
}

variable "secrets" {
  description = "Secret values keyed by secret name."
  type        = map(string)
  sensitive   = true
  default     = {}
}

variable "tags" {
  description = "Tags applied to the Container App."
  type        = map(string)
  default     = {}
}
