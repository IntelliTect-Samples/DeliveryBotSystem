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

variable "azure_openai_api_version" {
  description = "Azure OpenAI API version used for chat completions."
  type        = string
  default     = "2024-10-21"
}

variable "azure_openai_api_key_secret_name" {
  description = "Key Vault secret name that stores the Azure OpenAI API key."
  type        = string
}

variable "key_vault_uri" {
  description = "Vault URI used by the agent service to resolve secrets with managed identity."
  type        = string
}

variable "transcript_archive_blob_service_uri" {
  description = "Blob service URI used for transcript archive writes."
  type        = string
}

variable "transcript_archive_container_name" {
  description = "Blob container name used for transcript archive writes."
  type        = string
}

variable "order_service_url" {
  description = "Order Service base URL used for live order enrichment."
  type        = string
  default     = ""
}

variable "simulator_api_url" {
  description = "Robot Simulator base URL used for live bot enrichment."
  type        = string
  default     = ""
}

variable "search_endpoint" {
  description = "Azure AI Search endpoint used for agent grounding."
  type        = string
  default     = ""
}

variable "search_index_name" {
  description = "Azure AI Search index name used for agent grounding."
  type        = string
  default     = "delivery-agent-knowledge"
}

variable "servicebus_fully_qualified_namespace" {
  description = "Service Bus namespace host used for support escalation publishing."
  type        = string
  default     = ""
}

variable "support_escalation_queue_name" {
  description = "Service Bus queue name used for support escalation publishing."
  type        = string
  default     = "support-escalations"
}

variable "cors_allowed_origins" {
  description = "Comma-separated frontend origins allowed to call the Agent Service."
  type        = string
  default     = ""
}

variable "tags" {
  description = "Common tags applied to Agent Service resources."
  type        = map(string)
  default = {
    project   = "DeliveryBot"
    component = "agent-service"
  }
}

