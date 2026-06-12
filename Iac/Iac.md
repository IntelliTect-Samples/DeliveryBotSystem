# Infrastructure as Code

This repository now has a project-wide Terraform root under [Iac/main.tf](C:/Users/kernw/Desktop/DeliveryBotSystem%20-%20FinalProject/Iac/main.tf).

The root composes the major Delivery Bot services into one deployable stack:

- `shared-infra`
- `frontend`
- `admin-webapp`
- `order-service`
- `agent-service`
- `bot-api`
- `simulator`
- `readable-bot-network-representation`

## Final Project Architecture

The final-project deployment is designed to show a connected Azure solution built on the class project:

1. `App Service`
   Hosts the customer and admin web apps.
2. `Container Apps`
   Hosts the order service, bot API, simulator, and AI agent service.
3. `Azure OpenAI`
   Powers the delivery assistant.
4. `Event Hubs`
   Carries simulator robot-output and assignment-related events.
5. `Azure Functions`
   Projects robot-output events into a read model.
6. `Cosmos DB`
   Stores the current readable bot-network projection.
7. `Application Insights`
   Captures telemetry for the Function App projection.

## Notes

- The Terraform backend is intentionally left as a partial `azurerm` backend so each student can supply their own state storage settings.
- The previous shared-environment import file has been moved to an example file so the final-project environment can deploy cleanly without trying to import older shared resources.
- The readable bot network module is now wired into the root so the final project can demonstrate a full event-driven read model rather than only the request/response path.
