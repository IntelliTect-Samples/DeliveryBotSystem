# Order Service — Infrastructure (Terraform)

Provisions the **Order Service** Azure Container App (`deliverybot-order-service`).

It reuses the team's shared infrastructure (resource group, Container App
Environment, and ACR) via `data` sources, and only owns the Order Service app
itself. The app is created through the reusable [`container-app`](./modules/container-app)
module.

## Layout

```
order-service/
├── providers.tf            # terraform + azurerm + remote state (dbstfstate01)
├── main.tf                 # data sources for shared infra + module call
├── variables.tf
├── outputs.tf
└── modules/
    └── container-app/      # reusable Azure Container App module
```

## What it creates

- `azurerm_container_app.deliverybot-order-service` with:
  - a **system-assigned managed identity**
  - **ACR pull** via the `acr-password` secret + `registry` block (admin creds)
  - external **ingress** on port 8080
  - env vars: `ASPNETCORE_ENVIRONMENT`, `BotNetApi__BaseUrl`
  - secret-backed env vars: `ConnectionStrings__DefaultConnection`,
    `EventHub__ConnectionString`

The **image tag is owned by the CD pipeline**, not Terraform — the module sets
an initial `:latest` image and `ignore_changes` on it so `terraform apply`
doesn't revert the running revision the pipeline deployed.

## Required inputs (sensitive — supplied by the pipeline, never committed)

| Variable | Source |
|---|---|
| `sql_connection_string`      | built from the SQL server/db + Managed Identity auth |
| `eventhub_connection_string` | `AZURE_EVENTHUB_CONNECTION_STRING` GitHub secret |

Pass them as `TF_VAR_sql_connection_string` / `TF_VAR_eventhub_connection_string`.

## Usage

```bash
cd Iac/order-service
terraform init
terraform plan
terraform apply
```

Auth is via OIDC (`azure/login@v2`) in CI; locally, `az login` works with
`use_oidc` disabled or `ARM_*` env vars set.

## Importing the existing app

The `deliverybot-order-service` Container App was originally created by hand.
Before the first `apply`, import it so Terraform adopts it instead of trying to
create a duplicate:

```bash
terraform import \
  module.order_service_app.azurerm_container_app.this \
  /subscriptions/<SUB_ID>/resourceGroups/ewu-deliverybotsystem-rg/providers/Microsoft.App/containerApps/deliverybot-order-service
```

Then run `terraform plan` and reconcile any diff (e.g. tags) before applying.

## Open decision — SQL server

The running app's connection string points at `jacob-orderservice-sql2`, a
server created manually and **not** in any Terraform. The shared root Iac
defines `OrderServiceDb` on `deliverybotsystem-sql` instead. Decide whether to:
- consolidate onto the shared `deliverybotsystem-sql`, or
- add `jacob-orderservice-sql2` to Terraform.

Either way, the chosen server's connection string is passed via
`sql_connection_string`; this stack does not yet manage the database itself.

## Follow-up

The deploy workflow currently sets env vars with `az containerapp update
--set-env-vars`. Once Terraform owns the env vars, the workflow should be
trimmed to **only update the image tag**, to avoid drift between the two.
