# DeliveryBot Admin & Maintenance App

Internal-staff web app for the DeliveryBot Spokane system. Tracks against issue [#18](https://github.com/IntelliTect-Samples/DeliveryBotSystem/issues/18).

Sprint 2 user stories closed by this work:
[#48 View the bot fleet](https://github.com/IntelliTect-Samples/DeliveryBotSystem/issues/48) ·
[#49 Add a new bot](https://github.com/IntelliTect-Samples/DeliveryBotSystem/issues/49) ·
[#50 Update bot configuration](https://github.com/IntelliTect-Samples/DeliveryBotSystem/issues/50) ·
[#52 Remove a bot](https://github.com/IntelliTect-Samples/DeliveryBotSystem/issues/52)

## What it does

The admin app is the staff-facing UI over two backend systems:

- **BotNet API** (PR #37, Container App `ewu-deliverybotsystem-api`) — the device registry / source of truth.
- **Robot Simulator** (PR #38, Container App `deliverybot-robot-simulator`) — the runtime simulated bots that emit telemetry.

Every admin write (create / edit / delete) is a **double-write**: BotNet is called first as the registry of record, then the simulator is called best-effort so the simulated bot stays in sync with the registry. Partial failures are surfaced in the UI as a yellow banner instead of being silently swallowed.

## Tech stack

- React 19 + Vite 8 (matches the Customer Facing Web App)
- Plain JSX, inline styles, no UI framework
- API clients in `src/api/` — one per backend, plus an orchestrator (`admin.js`) for the double-write

## Local dev

```bash
cd admin-webapp
npm install
cp .env.example .env.local   # optional — leave VITE_*_URL unset to use mock/no-sync
npm run dev
```

The app boots at <http://localhost:5173>.

| Env var | Effect when unset |
|---|---|
| `VITE_BOTNET_API_URL` | Falls back to clearly-labeled mock data |
| `VITE_SIMULATOR_API_URL` | Simulator sync silently disabled; BotNet calls still work |

## What works today

- **Bots tab**: list, refresh, add (#49), edit (#50), recharge, toggle servicing status, delete (#52) — all wired to both backends per the double-write pattern.
- **Orders tab**: placeholder (waits on #22 Order Service).
- **Configuration tab**: placeholder (waits on App Configuration Service work).

## Architecture

See [`docs/architecture.md`](docs/architecture.md) for the full solution architecture and the BotNet/Simulator integration mappings.

## Deploy

Provisioned via Terraform in [`Iac/admin-webapp/`](../Iac/admin-webapp/) and deployed via the GitHub Actions workflow [`AdminWebpage-Deploy-WF.yml`](../.github/workflows/AdminWebpage-Deploy-WF.yml). Both run on every push to `main` that touches this directory.

Target App Service: `WA-DeliveryBot-Admin-dev` (Canada Central, reuses `ASP-RGDeliveryBotdev-8b82`).
