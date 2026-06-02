# Admin & Maintenance App — Solution Architecture

Scope: the Admin Web App slice of the DeliveryBot system (issue #18 + Sprint 2 stories #48, #49, #50, #52). Other services are referenced where the admin app interacts with them but are out of scope to build here.

## Solution diagram

```
                       ┌──────────────────┐
                       │  Admin User      │   ─ add bots
                       │  (staff / ops)   │   ─ configure bots
                       └────────┬─────────┘   ─ remove bots
                                │  HTTPS      ─ view fleet
                                ▼
                  ┌──────────────────────────┐
                  │  Admin Web App           │   ← App Service Linux + pm2,
                  │  React + Vite SPA        │     deployed via OIDC from
                  │  (WA-DeliveryBot-Admin-  │     GitHub Actions
                  │   dev, Canada Central)   │
                  └─────────────┬────────────┘
                                │
                  ┌─────────────┼─────────────┐
                  │             │             │
                  ▼             ▼             ▼
        ┌──────────────────┐         ┌──────────────────┐
        │ BotNet API       │         │ Robot Simulator  │
        │ (PR #37)         │         │ (PR #38)         │
        │ Container App,   │         │ Container App,   │
        │ ewu-deliverybot- │         │ deliverybot-     │
        │ system-api       │         │ robot-simulator  │
        │                  │         │                  │
        │ source of truth  │         │ runtime state    │
        │ for registry     │         │ + telemetry      │
        └────────┬─────────┘         └──────────────────┘
                 │
                 ▼
        ┌──────────────────┐
        │ Azure SQL        │
        │ BotNetApiDb      │
        └──────────────────┘

  Cross-cutting (provisioned by the team):
   • Key Vault, App Insights, Log Analytics workspace
   • Container Apps Environment (managedEnvironment-ewudeliverybots-aa2f)
   • App Service Plan (ASP-RGDeliveryBotdev-8b82, shared with customer site)
```

## Double-write integration with the simulator

Every admin write touches **both** systems. BotNet is the registry of record; the simulator hosts the runtime simulated bot that emits telemetry events.

| Operation | BotNet call (first) | Simulator call (best-effort, after BotNet OK) |
|---|---|---|
| **View fleet** (#48) | `GET /api/bots` | — (BotNet is sufficient) |
| **Add bot** (#49) | `POST /api/bots` → returns `{ id, name, … }` | `POST /bots` with `{ botId: slug(name), model: "DeliveryBot-V1", currentLocation: Spokane }` |
| **Update bot** (#50) | `PUT /api/bots/{id}` | `PATCH /bots/{slug(name)}` with `{ powerLevel: batteryLevel }` |
| **Remove bot** (#52) | `DELETE /api/bots/{id}` | `DELETE /bots/{slug(name)}` |

### Field mappings

- BotNet `name` → simulator `botId` (lowercased, hyphenated for safety: `"Bot-001"` → `"bot-001"`). Defined in [`src/api/simulator.js`](../src/api/simulator.js) `toBotId()`.
- BotNet `batteryLevel` (0–100 int) → simulator `powerLevel` (double).
- Simulator-only fields default to Spokane city center coordinates and `DeliveryBot-V1` model.

### Error semantics

Not transactional. If BotNet succeeds and the simulator fails, the admin app shows a yellow banner: *"Bot N saved in BotNet, but simulator sync failed: <reason>"*. The admin can re-attempt the operation or fix the simulator. Rollback is intentionally not automated — for a class project, transactional 2-phase commit would be overkill.

### Graceful degradation

If `VITE_SIMULATOR_API_URL` is unset (or the simulator is down), simulator calls are skipped silently and the admin app operates against BotNet alone. The header shows a `Simulator: Offline` badge so the operator knows sync is disabled.

## Azure services in scope

| # | Service | Purpose | Class-covered |
|---|---|---|---|
| 1 | App Service (Linux, Node 22) | host the Admin Web App | ✅ |
| 2 | Container Apps | host BotNet API + Simulator (consumed by admin) | ✅ |
| 3 | Azure SQL Database | bot registry storage (via BotNet API) | ✅ |
| 4 | App Service Plan | shared with customer site | ✅ |
| 5 | Azure Monitor / App Insights | logs, traces, alerts | ✅ |
| 6 | Container Apps Environment | hosts upstream services | ✅ |

Comfortably clears the Final's "≥5 services, ≥4 covered in class" bar. Extra credit eligibility: GitHub Action deploys the app (+5), Terraform for the App Service in `Iac/admin-webapp/` (+5).

## Out of scope (other teams own)

- **Customer Facing Web App** — crawfordkid2, PR #58 (`frontend/customer-webapp/`)
- **Bot Simulator / Robot** — DakotaCondos, PR #38 (we consume this)
- **BotNet Management API** — Bill Miller, PR #37 (we consume this)
- **Order Service** — Jake, on `order-service` branch (we'll consume once it lands)
- **Ordering Agent (Foundry)** — unassigned, issue #19
- **Robot Data Ingestion** — wiilke, issue #12

## Known gaps

- **CORS** — admin App Service in Canada Central calls Container Apps in West US 2. If BotNet / Simulator don't permit the admin origin, browser requests will fail. Surface the actual error via the banner and ask upstream owners to add the admin URL to their CORS allow-list.
- **Auth** — no Entra ID gate yet (Story #54 is in the backlog, not Sprint 2).
- **Live telemetry feed** — no Event Hub consumer yet (Story #55 is in the backlog).
