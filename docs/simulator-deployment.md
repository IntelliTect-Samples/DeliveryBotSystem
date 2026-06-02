# Simulator Deployment

## Overview

The Robot Simulator is deployed as a Container App (`deliverybot-robot-simulator`) in the `ewu-deliverybotsystem-rg` resource group. Deployment is handled by a GitHub Actions workflow that builds a new container image and updates the running Container App image only.

Infrastructure configuration for the simulator is codified in `Iac/simulator/`.

---

## IaC Module (`Iac/simulator/`)

The simulator Terraform module uses a **balanced ownership** approach. It owns the Container App's structural configuration while deliberately excluding the live Event Hub transport settings.

### What this module owns

| Resource | Managed properties |
|---|---|
| `azurerm_container_app` | Image reference, ingress (external, port 8080), revision mode (Single), scale (min 1 / max 1), system-assigned identity, ACR registry reference, `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS` |

### What this module does NOT own

The following settings are currently managed outside IaC (via Azure Portal or CLI) and are intentionally excluded from the Terraform module:

| Setting | Reason |
|---|---|
| `EventTransport__Mode` | Already configured on live Container App |
| `EventTransport__ConnectionString` | Secret — managed outside IaC |
| `EventTransport__InputEventHubName` | Already configured on live Container App |
| `EventTransport__OutputEventHubName` | Already configured on live Container App |
| `EventTransport__ConsumerGroup` | Already configured on live Container App |
| `EventTransport__EnableInputConsumer` | Already configured on live Container App |
| Container App secrets | Managed outside IaC |

A `lifecycle { ignore_changes }` block in `main.tf` ensures Terraform does not overwrite these settings when applying changes.

These settings will be brought under IaC management when the project-wide Terraform module is established.

### Referenced existing resources

These resources are read via `data` sources; they are not created or destroyed by this module:

| Resource | Name |
|---|---|
| Resource group | `ewu-deliverybotsystem-rg` |
| Container Apps environment | `managedEnvironment-ewudeliverybots-aa2f` |
| Azure Container Registry | `DeliverybotCR` |
| Event Hub namespace | `DeliverybotSimulator-EVHNS` |

### Running the module manually

Prerequisites:
- Terraform >= 1.5
- Azure CLI authenticated (`az login`) with sufficient permissions on `ewu-deliverybotsystem-rg`

```powershell
cd Iac/simulator

terraform init
terraform plan
terraform apply
```

To deploy a specific image tag:

```powershell
terraform apply -var="image_tag=<commit-sha>"
```

---

## GitHub Actions Workflow

File: `.github/workflows/simulator-deploy.yml`

### Trigger

The workflow runs automatically on push to `main` when any file under `RobotSimulator/` changes.

```yaml
on:
  push:
    branches: [main]
    paths:
      - "RobotSimulator/**"
```

### What the workflow does

1. Checks out the repository
2. Authenticates to Azure using OIDC federated identity (no client secrets)
3. Logs in to `DeliverybotCR` container registry
4. Builds the simulator Docker image from:
   - Dockerfile: `RobotSimulator/src/DeliveryBot.RobotSimulator.Api/Dockerfile`
   - Build context: `RobotSimulator/`
   - Tag: `deliverybotcr.azurecr.io/deliverybot-robot-simulator:<commit-sha>`
5. Pushes the image to ACR
6. Updates the `deliverybot-robot-simulator` Container App image **only**
7. Prints the Container App URL and health endpoint

### What the workflow does NOT change

The workflow uses `az containerapp update --image` with no `--set-env-vars`. This means all existing environment variables and secrets on the live Container App (including Event Hub transport settings) are preserved exactly as-is after every deployment.

### Required GitHub secrets

These secrets must be configured in the repository before the workflow can run. They are already used by the existing `botNetApi-deploy.yml` workflow.

| Secret | Description |
|---|---|
| `AZURE_CLIENT_ID` | Client ID of the Azure AD app registration used for OIDC |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |

---

## Known Limitations

- **Event Hub env vars are not in IaC.** If the Container App is recreated from scratch using this Terraform module, the Event Hub transport settings will not be present. They must be re-applied manually or via CLI until they are brought under IaC.
- **No Terraform remote state configured.** The module currently has no `backend` block. A remote backend (e.g., Azure Storage) should be added before team use.
- **Image tag in tfvars defaults to `latest`.** For production deployments, always supply the specific commit SHA as `image_tag`.
