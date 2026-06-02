# Temporary Readable Bot Network Driver

This Terraform root is a temporary driver for creating the Readable Bot Network Representation resources before the project-wide IaC root exists.

It uses the reusable module at:

```text
Iac/modules/readable-bot-network-representation
```

## Target

- Subscription: `IntelliTect-Dev`
- Resource group: `ewu-deliverybotsystem-rg`
- Location: `westus2`
- Event Hub namespace: `DeliverybotSimulator-EVHNS`
- Event Hub: `robot-output`

## Commands

```powershell
terraform init
terraform plan -out readable-bot-network-dev.tfplan
terraform apply readable-bot-network-dev.tfplan
```

When finished testing:

```powershell
terraform destroy
```

The Terraform state for this temporary deployment should remain local to this folder and should not be committed.

## Permission Notes

Cosmos DB creation requires the subscription resource provider `Microsoft.DocumentDB` to be registered.

If that provider is not registered, an instructor or subscription owner can register it:

```powershell
az provider register --namespace Microsoft.DocumentDB
```

This temporary driver disables the Event Hub receiver role assignment because the current user does not have `Microsoft.Authorization/roleAssignments/write` permission on the `robot-output` Event Hub scope.

After the Function App exists, an owner can grant it Event Hub read access:

```powershell
az role assignment create `
  --assignee <function_app_principal_id> `
  --role "Azure Event Hubs Data Receiver" `
  --scope <robot_output_eventhub_resource_id>
```
