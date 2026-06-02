variable "name" {
  description = "Name of the Container App."
  type        = string
}

variable "resource_group_name" {
  description = "Resource group the Container App lives in."
  type        = string
}

variable "container_app_environment_id" {
  description = "ID of the Container App Environment to deploy into."
  type        = string
}

variable "acr_login_server" {
  description = "Login server of the ACR images are pulled from (e.g. deliverybotcr.azurecr.io)."
  type        = string
}

variable "acr_username" {
  description = "ACR admin username used to authenticate image pulls."
  type        = string
}

variable "acr_password" {
  description = "ACR admin password. Stored as a Container App secret named 'acr-password'."
  type        = string
  sensitive   = true
}

variable "container_name" {
  description = "Name of the container inside the app."
  type        = string
}

variable "image" {
  description = "Initial image reference. The image tag is owned by the CD pipeline after creation (see lifecycle.ignore_changes)."
  type        = string
}

variable "target_port" {
  description = "Container port that ingress routes to."
  type        = number
  default     = 8080
}

variable "cpu" {
  description = "vCPU allocated to the container."
  type        = number
  default     = 0.5
}

variable "memory" {
  description = "Memory allocated to the container."
  type        = string
  default     = "1Gi"
}

variable "min_replicas" {
  description = "Minimum number of replicas (0 allows scale-to-zero)."
  type        = number
  default     = 0
}

variable "max_replicas" {
  description = "Maximum number of replicas."
  type        = number
  default     = 3
}

variable "secrets" {
  description = "Map of Container App secret name => secret value. Reference these from secret_env_vars."
  type        = map(string)
  default     = {}
  sensitive   = true
}

variable "env_vars" {
  description = "Map of plain environment variable name => value."
  type        = map(string)
  default     = {}
}

variable "secret_env_vars" {
  description = "Map of environment variable name => secret name (the secret must exist in `secrets`)."
  type        = map(string)
  default     = {}
}

variable "tags" {
  description = "Tags applied to the Container App."
  type        = map(string)
  default     = {}
}
