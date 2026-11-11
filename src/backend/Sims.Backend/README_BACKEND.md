# SIMS Backend – README for Team

## Stack

| Component | Technology |
|-----------|-----------|
| Runtime   | .NET 8 / ASP.NET Core Web API |
| ORM       | Entity Framework Core 8 + SQL Server |
| NoSQL Log | Redis (StackExchange.Redis) |
| Container | Docker (multi-stage, port 8080) |

---

## Quick-Start (Docker Compose)

```bash
docker compose up --build
```

Swagger UI is available at **http://localhost:8080/swagger** in every environment.

---

## Environment Variables (override in docker-compose.yml)

| Variable | Default (appsettings.json) | Purpose |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Server=localhost,1433;Database=SimsDb;User Id=sa;Password=Adm1234!;TrustServerCertificate=True;MultipleActiveResultSets=true` | SQL Server connection string |
| `Redis` | `localhost:6379` | Redis host:port |

**Example `docker-compose.yml` service block:**

```yaml
services:
  api:
    build: ./src/backend/Sims.Backend
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=SimsDb;User Id=sa;Password=Adm1234!;TrustServerCertificate=True;MultipleActiveResultSets=true
      - Redis=redis:6379
    depends_on:
      - sqlserver
      - redis

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=Adm1234!
    ports:
      - "1433:1433"

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
```

> **Note:** The API runs `Database.MigrateAsync()` on startup, so the schema
> and seed data are applied automatically. No manual `dotnet ef` commands needed.

---

## Seed Credentials

| Username | Password    | Role  |
|----------|-------------|-------|
| admin    | Admin1234!  | Admin |
| analyst  | Analyst1234!| User  |

> **Change these before any production deployment.**

---

## API Endpoints

### Incidents  `api/incidents`

| Method | Route | Description |
|--------|-------|-------------|
| GET    | `/api/incidents` | List all incidents (optional: `?status=Open&severity=High`) |
| GET    | `/api/incidents/{id}` | Get single incident |
| POST   | `/api/incidents` | Create new incident |
| PUT    | `/api/incidents/{id}` | Update incident |
| POST   | `/api/incidents/{id}/escalate` | Escalate incident to new assignee |
| POST   | `/api/incidents/{id}/close` | Close incident |

**Create / Update body fields:**

```json
// POST /api/incidents
{
  "title": "string (max 150)",
  "description": "string",
  "affectedSystem": "string",
  "cveNumber": "CVE-YYYY-NNNN (optional)",
  "severity": "Low | Medium | High | Critical",
  "reportedBy": "string"
}

// POST /api/incidents/{id}/escalate
{
  "targetAssignee": "string",
  "reason": "string"
}
```

**Severity values:** `Low` `Medium` `High` `Critical`  
**Status values:** `Open` `InProgress` `Escalated` `Closed`

---

### Auth  `api/auth`

| Method | Route | Description |
|--------|-------|-------------|
| POST   | `/api/auth/register` | Register new user |
| POST   | `/api/auth/login` | Authenticate user (returns UserResponseDto) |
| GET    | `/api/auth/users` | List all users |
| PUT    | `/api/auth/users/{id}/status?isActive=true\|false` | Activate / deactivate account |

**Register body:**

```json
{
  "username": "string (3-100 chars)",
  "email": "user@example.com",
  "password": "string (min 8 chars)",
  "role": "User | Admin"
}
```

**Login body:**

```json
{
  "username": "admin",
  "password": "Admin1234!"
}
```

---

### Logs  `api/logs`

| Method | Route | Description |
|--------|-------|-------------|
| GET    | `/api/logs?count=50` | Fetch N most-recent structured log entries from Redis (max 500) |

**Log entry shape:**

```json
{
  "id": "uuid",
  "timestamp": "2024-04-10T14:30:00Z",
  "logLevel": "INFO | WARN | ERROR",
  "message": "Incident #1 created.",
  "details": "Title: ... | Severity: ..."
}
```

**Automatically logged events:**

| Event | Level |
|---|---|
| Incident created | INFO |
| Incident updated | INFO |
| Incident escalated | WARN |
| Incident closed | INFO |
| Successful login | INFO |
| Failed login | WARN |
| User registered | INFO |
| User status changed | INFO |

---

## Project Structure

```
src/backend/Sims.Backend/
├── Controllers/
│   ├── AuthController.cs
│   ├── IncidentsController.cs
│   └── LogsController.cs
├── Data/
│   └── SimsDbContext.cs
├── DTOs/
│   ├── CreateIncidentDto.cs
│   ├── UpdateIncidentDto.cs
│   ├── EscalateIncidentDto.cs
│   ├── RegisterUserDto.cs
│   ├── LoginDto.cs
│   └── UserResponseDto.cs
├── Models/
│   ├── Incident.cs
│   ├── IncidentSeverity.cs
│   ├── IncidentStatus.cs
│   ├── SimsLogEntry.cs
│   ├── User.cs
│   └── UserRole.cs
├── Services/
│   ├── IRedisLogService.cs
│   └── RedisLogService.cs
├── appsettings.json
├── Dockerfile
└── Program.cs
```

---

## NuGet Packages Required

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package StackExchange.Redis
dotnet add package BCrypt.Net-Next
```
