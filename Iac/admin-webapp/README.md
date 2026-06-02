# Admin Web App — Terraform

Provisions the Azure App Service that hosts the [Admin & Maintenance App](../../admin-webapp/) (issue #18).

## Structure

Per the [project guidelines](../../docs/project-guidelines.md), the App Service is defined in a reusable module that the root config consumes.

```
Iac/admin-webapp/
├── providers.tf          # terraform block, azurerm backend + provider (root only)
├── main.tf               # calls module "admin_webapp"
├── variables.tf          # root inputs + defaults
├── outputs.tf            # re-exports the module's outputs
└── modules/
    └── webapp/           # reusable App Service module
        ├── main.tf       # data sources (RG, plan) + azurerm_linux_web_app
        ├── variables.tf  # module inputs
        └── outputs.tf    # name, hostname, url
```

The root `main.tf` includes a `moved {}` block so the refactor into a module is a no-op against existing state (the live App Service is preserved, not recreated).

## What it creates

| Resource | Notes |
|---|---|
| `module.admin_webapp.azurerm_linux_web_app.admin` | `WA-DeliveryBot-Admin-dev`, Node 22 Linux, `pm2 serve` startup, System Assigned Managed Identity |

## What it reuses (data sources, not managed)

| Resource | Why |
|---|---|
| `azurerm_resource_group.rg` (`ewu-deliverybotsystem-rg`) | Team's shared RG |
| `azurerm_service_plan.plan` (`ASP-RGDeliveryBotdev-8b82`) | Shared with Customer site — no duplicate plan cost |

## State

Stored in Azure Blob:

- Storage account: `dbstfstate01` (pre-existing in the RG)
- Container: `tfstate`
- Key: `admin-webapp.tfstate` (unique to this module — won't collide with PR #74's shared Iac)

## How it runs

The [`AdminWebpage-Deploy-WF.yml`](../../.github/workflows/AdminWebpage-Deploy-WF.yml) workflow:

1. Authenticates to Azure via OIDC federated identity (existing repo secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`)
2. Ensures the `tfstate` container exists in `dbstfstate01`
3. Runs `terraform init` + `apply -auto-approve` against this directory
4. Builds the React app and deploys to the App Service Terraform just created/updated

## Local execution (rarely needed)

If you want to run this locally you'll need `terraform`, `az` CLI, and an Azure session:

```bash
az login
terraform init
terraform plan
terraform apply
```

## Migration note

This module deliberately stores its state in a unique key (`admin-webapp.tfstate`) rather than depending on Bill's PR #74 backend config. Once #74 lands and the team agrees on a backend convention, switch [`providers.tf`](providers.tf) to consume the shared backend.
