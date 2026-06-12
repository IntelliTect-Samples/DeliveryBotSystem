# Agent Service — Infrastructure (Terraform)

Provisions the Agent Service Azure Container App (`deliverybot-agent-service`).

It reuses the shared resource group, Container App Environment, and ACR, and
only owns the Agent Service app itself.

## Required inputs

| Variable | Purpose |
|---|---|
| `azure_openai_endpoint` | Azure OpenAI resource endpoint |
| `azure_openai_deployment` | Azure OpenAI deployment name |
| `azure_openai_api_key` | Azure OpenAI API key |

## Usage

```bash
cd Iac/agent-service
terraform init
terraform plan
terraform apply
```
