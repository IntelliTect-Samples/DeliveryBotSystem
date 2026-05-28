# BotNetApi — Developer Integration Guide

## Purpose

BotNetApi is the backend service for the vending machine bot delivery network.

It acts as the central source of truth for:

- Bot status
- Battery levels
- Availability/service state

Other systems in the project communicate with the bots exclusively through this API.

Primary consumers:

1. Frontend web application
2. Bot simulator application

---

# High Level Architecture

```text
Bot Simulators
      |
      v
 ASP.NET Core API
      |
      v
 Azure SQL Database
      ^
      |
Frontend Web App
```

The bot simulators push updates into the API.

The frontend pulls data from the API to display:

- Bot availability
- Battery status

The frontend and simulators should NOT communicate directly with the database.

---

# Tech Stack

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- Azure SQL Database
- Swagger/OpenAPI

---

# Base API Route

```text
/api/bots
```

Production base URL:

```text
https://ewu-deliverybotsystem-api.mangocoast-332176b0.westus2.azurecontainerapps.io
```

Local development URL:

```text
http://localhost:5021/api/bots
https://localhost:7260/api/bots  (HTTPS)
```

---

# Full Endpoint Reference

## CRUD

| Method   | Route            | Description          |
| -------- | ---------------- | -------------------- |
| `GET`    | `/api/bots`      | Return all bots      |
| `GET`    | `/api/bots/{id}` | Return a single bot  |
| `POST`   | `/api/bots`      | Add a new bot        |
| `PUT`    | `/api/bots/{id}` | Full update of a bot |
| `DELETE` | `/api/bots/{id}` | Remove a bot         |

## Bot Actions

| Method | Route                             | Description         |
| ------ | --------------------------------- | ------------------- |
| `PUT`  | `/api/bots/{id}/recharge`         | Set battery to 100  |
| `PUT`  | `/api/bots/{id}/servicing-status` | Set servicing state |

---

# Bot Data Model

Each bot contains:

| Field               | Type     | Description                       |
| ------------------- | -------- | --------------------------------- |
| id                  | int      | Unique bot identifier             |
| name                | string   | Friendly bot name                 |
| batteryLevel        | int      | 0–100                             |
| lastUpdated         | datetime | Last update timestamp (UTC)       |
| isOnline            | bool     | Whether the bot is online         |
| isServicingCustomer | bool     | Whether the bot is currently busy |

---

# Expected Frontend Usage

The frontend will primarily:

## 1. Display All Bots

```http
GET /api/bots
```

Use for:

- Status dashboards
- Admin pages

---

## 2. Display Individual Bot Details

```http
GET /api/bots/{id}
```

---

# Expected Bot Simulator Usage

The simulator should periodically push updates into the API.

Typical simulator flow:

## Create Bot

```http
POST /api/bots
```

Example:

```json
{
  "name": "BOT-ECHO",
  "batteryLevel": 100,
  "isOnline": true
}
```

> `isServicingCustomer` is omitted — new bots always start as not servicing a customer.

---

## Full Update

```http
PUT /api/bots/12
```

```json
{
  "name": "BOT-ECHO",
  "batteryLevel": 85,
  "isOnline": true,
  "isServicingCustomer": false
}
```

---

## Recharge Battery

```http
PUT /api/bots/12/recharge
```

Automatically sets:

```text
batteryLevel = 100
```

---

## Mark Bot as Busy

```http
PUT /api/bots/12/servicing-status
```

```json
{
  "isServicingCustomer": true
}
```

When servicing is complete:

```json
{
  "isServicingCustomer": false
}
```

---

## Delete Bot

```http
DELETE /api/bots/{id}
```

Permanently removes a bot from the system. Use only when decommissioning a bot.

**Response:** `204 No Content`

---

# Example Response

```json
{
  "id": 4,
  "name": "Bot-4",
  "batteryLevel": 82,
  "lastUpdated": "2026-05-17T18:12:55Z",
  "isOnline": true,
  "isServicingCustomer": false
}
```

---

# Expected Update Frequency

## Bot Simulator

Recommended:

- Battery updates periodically
- Service state changes as needed

## Frontend

Recommended:

- Poll every few seconds

OR

- Add SignalR later if real-time updates become necessary

SignalR is intentionally NOT included yet to keep the project simple.

---

# Database Notes

Development:

- SQL Server LocalDB

Production:

- Azure SQL Database

Entity Framework Core migrations will manage schema creation.

**Migration commands:**

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

---

# Swagger Support

Swagger UI is available during local development:

```text
http://localhost:5021/swagger
https://localhost:7260/swagger  (HTTPS)
```

Developers can:

- Test endpoints
- View request/response schemas
- Experiment without Postman

---

# Current Scope

Included:

- CRUD operations
- Bot status management
- EF Core persistence

Not included yet:

- Authentication
- Authorization
- Real-time websocket updates
- Queueing systems
- Distributed services
- Bot routing/pathfinding
- Reservations
- Multi-city support

---

# Design Philosophy

This API is intentionally:

- Simple
- Beginner-friendly
- Easy to explain in a classroom setting
- Structured similarly to real production APIs
- Built so it can later evolve into a larger distributed system without major rewrites
