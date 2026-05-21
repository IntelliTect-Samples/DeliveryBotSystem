# BotNetApi — Developer Integration Guide

## Purpose

BotNetApi is the backend service for the vending machine bot delivery network.

It acts as the central source of truth for:

- Bot status
- Bot locations
- Battery levels
- Inventory stock levels
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

- Bot locations
- Availability
- Battery status
- Nearest bot results

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

Example local development URL:

```text
http://localhost:5021/api/bots
https://localhost:7260/api/bots  (HTTPS)
```

---

# Full Endpoint Reference

## CRUD

| Method   | Route              | Description           |
|----------|--------------------|------------------------|
| `GET`    | `/api/bots`        | Return all bots        |
| `GET`    | `/api/bots/{id}`   | Return a single bot    |
| `POST`   | `/api/bots`        | Add a new bot          |
| `PUT`    | `/api/bots/{id}`   | Full update of a bot   |
| `DELETE` | `/api/bots/{id}`   | Remove a bot           |

## Bot Actions

| Method | Route                             | Description           |
|--------|-----------------------------------|-----------------------|
| `PUT`  | `/api/bots/{id}/recharge`         | Set battery to 100    |
| `PUT`  | `/api/bots/{id}/stock`            | Update stock level    |
| `PUT`  | `/api/bots/{id}/location`         | Update GPS location   |
| `PUT`  | `/api/bots/{id}/servicing-status` | Set servicing state   |

## Search

| Method | Route                                        | Description                |
|--------|----------------------------------------------|----------------------------|
| `GET`  | `/api/bots/findNearest?latitude=&longitude=` | Find nearest available bot |

---

# Bot Data Model

Each bot contains:

| Field               | Type     | Description                       |
| ------------------- | -------- | --------------------------------- |
| id                  | int      | Unique bot identifier             |
| name                | string   | Friendly bot name                 |
| stockLevel          | enum     | High / Medium / Low               |
| batteryLevel        | int      | 0–100                             |
| latitude            | double   | Current GPS latitude              |
| longitude           | double   | Current GPS longitude             |
| lastUpdated         | datetime | Last update timestamp (UTC)       |
| isOnline            | bool     | Whether the bot is online         |
| isServicingCustomer | bool     | Whether the bot is currently busy |

---

# Important System Rules

## Availability Rules

A bot is considered AVAILABLE only if:

```text
isOnline == true
isServicingCustomer == false
batteryLevel >= 15
```

The nearest-bot endpoint automatically filters out unavailable bots.

---

# Expected Frontend Usage

The frontend will primarily:

## 1. Display All Bots

```http
GET /api/bots
```

Use for:

- Map displays
- Status dashboards
- Admin pages

---

## 2. Find Nearest Available Bot

```http
GET /api/bots/findNearest?latitude=47.6588&longitude=-117.4260
```

Use for:

- User requests
- "Find nearest vending bot" feature
- Delivery assignment UI

The API handles:

- Distance calculation
- Availability filtering
- Ignoring busy bots
- Ignoring low battery bots

The frontend does NOT need to calculate nearest bots itself.

---

## 3. Display Individual Bot Details

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
  "stockLevel": "High",
  "batteryLevel": 100,
  "latitude": 47.6588,
  "longitude": -117.426,
  "isOnline": true
}
```

> `isServicingCustomer` is omitted — new bots always start as not servicing a customer.
> `stockLevel` accepts string values: `"High"`, `"Medium"`, or `"Low"`.

---

## Update Location

```http
PUT /api/bots/12/location
```

```json
{
  "latitude": 47.6612,
  "longitude": -117.431
}
```

Expected usage:

- Called frequently
- Simulates movement around Spokane

---

## Update Stock

```http
PUT /api/bots/12/stock
```

```json
{
  "stockLevel": "Medium"
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

This directly affects nearest-bot selection.

---

## Delete Bot

```http
DELETE /api/bots/{id}
```

Permanently removes a bot from the system. Use only when decommissioning a bot.

**Response:** `204 No Content`

---

# Nearest Bot Behavior

The endpoint:

```http
GET /api/bots/findNearest
```

works as follows:

1. Loads all bots
2. Filters out:
   - Offline bots
   - Busy bots
   - Bots under 15% battery
3. Calculates geographic distance
4. Sorts nearest-to-farthest
5. Returns the first valid bot

If no bots qualify:

```http
404 Not Found
```

or similar response.

---

# Example Response

```json
{
  "id": 4,
  "name": "Bot-4",
  "stockLevel": "High",
  "batteryLevel": 82,
  "latitude": 47.6592,
  "longitude": -117.4235,
  "lastUpdated": "2026-05-17T18:12:55Z",
  "isOnline": true,
  "isServicingCustomer": false
}
```

---

# Expected Update Frequency

## Bot Simulator

Recommended:

- Location updates every few seconds
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

- SQL Server LocalDB or SQL Express

Production:

- Azure SQL Database

Entity Framework Core migrations will manage schema creation.

**Migration commands:**

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

---

# Seed Data

Four bots are seeded at real Spokane, WA coordinates on first run:

| ID | Name        | Battery | Online | Servicing | Notes                                          |
|----|-------------|---------|--------|-----------|------------------------------------------------|
| 1  | BOT-ALPHA   | 92%     | Yes    | No        | Available                                      |
| 2  | BOT-BRAVO   | 61%     | Yes    | Yes       | Skipped by `findNearest` — busy                |
| 3  | BOT-CHARLIE | 8%      | No     | No        | Skipped by `findNearest` — offline + low battery |
| 4  | BOT-DELTA   | 77%     | Yes    | No        | Available                                      |

---

# Swagger Support

Swagger UI will be available during development:

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
- Nearest available bot lookup
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

# Assumptions

- All bots operate within Spokane, Washington
- GPS coordinates are trusted
- Simulators are responsible for realistic movement
- Frontend handles visualization only
- API is the source of truth for availability

---

# Design Philosophy

This API is intentionally:

- Simple
- Beginner-friendly
- Easy to explain in a classroom setting
- Structured similarly to real production APIs
- Built so it can later evolve into a larger distributed system without major rewrites
