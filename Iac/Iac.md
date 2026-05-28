# Infrastructure as Code

Terraform modules for project infrastructure live under `Iac/modules`.

## Modules

- `readable-bot-network-representation`: creates the Cosmos DB-backed bot read model infrastructure for the Readable Bot Network Representation epic.

This repository does not currently define a project-wide Terraform root. The module is intentionally written so another team member can wire it into that root when the shared IaC structure is ready.
