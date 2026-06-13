# Agent Service — Infrastructure (Terraform)

Provisions the Agent Service Azure Container App (`deliverybot-agent-service`).

It reuses the shared resource group, Container App Environment, and ACR, and
only owns the Agent Service app itself.

## Required inputs

| Variable | Purpose |
|---|---|
| `azure_openai_endpoint` | Azure OpenAI resource endpoint |
| `azure_openai_deployment` | Azure OpenAI deployment name |
| `azure_openai_api_key_secret_name` | Key Vault secret name that stores the Azure OpenAI API key |
| `key_vault_uri` | Key Vault URI read by the Agent Service through managed identity |
| `transcript_archive_blob_service_uri` | Blob service URI used for transcript archive writes |
| `transcript_archive_container_name` | Blob container name used for transcript archive writes |
| `order_service_url` | Order Service URL used for live order enrichment |
| `simulator_api_url` | Robot Simulator URL used for live bot enrichment |
| `search_endpoint` | Azure AI Search endpoint used for chatbot grounding |
| `search_index_name` | Azure AI Search index name |
| `servicebus_fully_qualified_namespace` | Service Bus namespace used for support escalation publishing |
| `support_escalation_queue_name` | Service Bus queue name used for support escalation publishing |

## Usage

```bash
cd Iac/agent-service
terraform init
terraform plan
terraform apply
```
