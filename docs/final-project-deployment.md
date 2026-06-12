# Final Project Deployment Plan

This final project extends the Delivery Bot System in a student-owned Azure environment. The goal is to demonstrate a connected Azure solution built on the class project without relying on the shared class resource group.

## Final Feature Story

The final focuses on an AI-assisted customer delivery flow plus an event-driven readable bot network:

1. The customer places an order in the customer web app.
2. The order service processes the order and reacts to simulator events.
3. The robot simulator publishes robot-output events to Event Hubs.
4. The readable bot network Function App consumes robot-output events.
5. The Function App projects the current bot state into Cosmos DB.
6. The customer-facing assistant uses Azure OpenAI to answer questions about the latest order, route, ETA, and assigned robot.

## Azure Services Covered

This plan touches at least five Azure services:

1. App Service
   Hosts the customer web app and admin web app.
2. Container Apps
   Hosts the order service, bot API, simulator, and agent service.
3. Azure OpenAI
   Powers the delivery assistant.
4. Event Hubs
   Carries robot-output and assignment-related events.
5. Azure Functions
   Projects robot events into a read model.
6. Cosmos DB
   Stores the readable bot network state.
7. Application Insights
   Captures Function App telemetry.

## Infrastructure Ownership

Terraform root: `Iac/`

Important notes:

- The Terraform backend is now intentionally partial.
- Supply your own Azure Storage backend values during `terraform init`.
- The old shared-environment import file is now only an example: `Iac/imports-shared-dev-reference.tf.example`.
- The readable bot network module is now wired into the root Terraform stack.

## GitHub Repository Variables

Set these repository variables before running the deployment workflows:

- `RESOURCE_GROUP_NAME`
- `ACR_NAME`
- `ACR_LOGIN_SERVER`
- `CUSTOMER_FRONTEND_APP_SERVICE_NAME`
- `ADMIN_APP_SERVICE_NAME`
- `BOT_API_CONTAINER_APP_NAME`
- `BOT_API_SQL_SERVER_NAME`
- `BOT_API_SQL_DATABASE_NAME`
- `ORDER_SERVICE_CONTAINER_APP_NAME`
- `AGENT_SERVICE_CONTAINER_APP_NAME`
- `SIMULATOR_CONTAINER_APP_NAME`
- `VITE_AGENT_API_URL`
- `VITE_MAP_TILE_URL`
- `VITE_ORDER_SERVICE_URL`
- `VITE_OSRM_API_URL`
- `VITE_SIMULATOR_API_BASE`
- `VITE_BOTNET_API_URL`
- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_DEPLOYMENT`
- `TFSTATE_RESOURCE_GROUP`
- `TFSTATE_STORAGE_ACCOUNT`
- `TFSTATE_CONTAINER`
- `TFSTATE_KEY`
- `READABLE_BOT_NETWORK_FUNCTION_APP_NAME`
- `READABLE_BOT_NETWORK_COSMOS_ACCOUNT_NAME`
- `READABLE_BOT_NETWORK_COSMOS_DATABASE_NAME`
- `READABLE_BOT_NETWORK_COSMOS_CONTAINER_NAME`
- `READABLE_BOT_NETWORK_DIAGNOSTICS_CONTAINER_NAME`

Optional admin auth variables:

- `ENTRA_CLIENT_ID`
- `ENTRA_TENANT_ID`
- `ENTRA_ADMIN_GROUP_ID`

## GitHub Repository Secrets

Set these repository secrets before deployment:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `AZURE_EVENTHUB_CONNECTION_STRING`
- `AZURE_OPENAI_API_KEY`

## Terraform Inputs

Use `Iac/final-project.tfvars.example` as the starting point for your own environment values.

At a minimum, your Terraform deployment needs:

- resource group name
- container app environment name
- ACR name
- Event Hub namespace name
- Azure OpenAI endpoint
- Azure OpenAI deployment name
- SQL/Event Hub connection string secrets injected through GitHub Actions

## Presentation Flow

Recommended demo sequence:

1. Show the architecture diagram.
2. Show the Terraform root and explain that one deployment composes the services.
3. Show the GitHub Actions workflows using OIDC.
4. Show the deployed resources in your own Azure resource group.
5. Place an order from the customer frontend.
6. Show the assistant answering order questions.
7. Show the readable bot network Function App and Cosmos DB projection.

## Honest Project Framing

Use this explanation if needed:

> This final is built on the class project codebase, but deployed in my own Azure environment because I did not have dependable access to the shared class resource group. I kept the deployment model consistent with the project by using Terraform, GitHub Actions, and Azure identity-based authentication.
