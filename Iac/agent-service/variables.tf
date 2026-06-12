variable "resource_group_name" {
  description = "Resource group that hosts the team's DeliveryBot resources."
  type        = string
  default     = "ewu-deliverybotsystem-rg"
}

variable "container_app_environment_name" {
  description = "Existing shared Container App Environment."
  type        = string
  default     = "deliverybot-dev-cae"
}

variable "acr_name" {
  description = "Existing shared Azure Container Registry the image is pulled from."
  type        = string
  default     = "deliverybotdevcr"
}

variable "container_app_name" {
  description = "Name of the Agent Service Container App."
  type        = string
  default     = "deliverybot-agent-service"
}

variable "image_name" {
  description = "Repository name of the Agent Service image in ACR."
  type        = string
  default     = "agentservice"
}

variable "azure_openai_endpoint" {
  description = "Azure OpenAI resource endpoint."
  type        = string
}

variable "azure_openai_deployment" {
  description = "Azure OpenAI deployment name used by the agent service."
  type        = string
}

variable "azure_openai_api_key" {
  description = "Azure OpenAI API key."
  type        = string
  sensitive   = true
}

variable "azure_openai_api_version" {
  description = "Azure OpenAI API version used for chat completions."
  type        = string
  default     = "2024-10-21"
}

variable "tags" {
  description = "Common tags applied to Agent Service resources."
  type        = map(string)
  default = {
    project   = "DeliveryBot"
    component = "agent-service"
  }
}

