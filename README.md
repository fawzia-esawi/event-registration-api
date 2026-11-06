# Event Registration API

Backend service for the Event Registration System, built as part of the Iscosoft training project. It manages categories, events, participants and registrations, and exposes a REST API documented with Swagger.

## Technology

- ASP.NET Core Web API (controllers) on .NET 8
- MySQL 8
- Dapper and MySqlConnector (plain SQL, no ORM)
- MediatR and FluentValidation
- Serilog (console and daily rolling log files in `logs/`)
- Swagger (Swashbuckle)

## Prerequisites

| Software | Version |
|---|---|
| .NET SDK | 8.0 or later (the project targets `net8.0`; tested with SDK 10.0.401) |
| MySQL Server | 8.0.13 or later (tested with 8.0.47). Version 8.0.13 is the minimum because the tables use expression defaults such as `DEFAULT (UTC_TIMESTAMP())` |
| MySQL Workbench or the `mysql` command-line client | any recent version |
| Git | any recent version |

The frontend lives in a separate repository (`event-registration-web`) and needs Node.js; see its README.

## Installation

```powershell
git clone https://github.com/fawzia-esawi/event-registration-api.git
cd event-registration-api/EventRegistration
dotnet restore
```

All the commands below run from the `EventRegistration` project folder (the one that contains `Program.cs`).

Note: the solution file `EventRegistration.Api.slnx` needs a recent SDK or Visual Studio. Running the commands from the project folder works with any .NET 8 SDK.

## Environment setup

1. Copy the example file:

   ```powershell
   Copy-Item .env.example .env
   ```

2. Open `.env` and set your MySQL password in the connection string:

   ```env
   DB_CONNECTION_STRING=server=localhost;port=3306;database=EventRegistration;user=root;password=your_password;
   ```

`.env` is ignored by Git. Never commit it.

CORS is enabled only for the origins listed in `Cors:AllowedOrigins` in `appsettings.json` (default `http://localhost:5173`, the Vite dev server). Change it if your frontend runs on another URL.

## Database setup

1. Create the database:

   ```sql
   CREATE DATABASE IF NOT EXISTS EventRegistration
     CHARACTER SET utf8mb4
     COLLATE utf8mb4_unicode_ci;
   ```

2. Apply the migrations **in numeric order** (`Database/migrations`). In MySQL Workbench use *File > Open SQL Script*, select the `EventRegistration` schema and run each file:

   1. `001_create_categories.sql`
   2. `002_create_events.sql`
   3. `003_create_participants.sql`
   4. `004_create_registrations.sql`

   If more numbered files exist after 004, apply them in order too. Do not edit a migration that was already applied; add a new one.

   With the command line (PowerShell):

   ```powershell
   Get-Content Database\migrations\001_create_categories.sql | mysql -u root -p EventRegistration
   ```

   Repeat for the other files.

3. Load the sample data (5 categories, 8 events, 12 participants, 12 active registrations and 3 cancelled registrations). Run it once, after all the migrations, on empty tables:

   ```powershell
   Get-Content Database\seed.sql | mysql -u root -p EventRegistration
   ```

## Run

```powershell
dotnet run --launch-profile http
```

| What | URL |
|---|---|
| API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |

The `https` profile (`dotnet run --launch-profile https`) serves `https://localhost:5001` and needs the development certificate (`dotnet dev-certs https --trust`).

The frontend expects the API at `http://localhost:5000/api` (`VITE_API_BASE_URL`).

## Main endpoints

| Resource | Endpoints |
|---|---|
| Categories | `GET/POST /api/categories`, `GET/PUT/DELETE /api/categories/{id}` |
| Events | `GET/POST /api/events`, `GET/PUT/DELETE /api/events/{id}` |
| Participants | `GET/POST /api/participants`, `GET/PUT/DELETE /api/participants/{id}` |
| Registrations | `GET/POST /api/events/{eventId}/registrations`, `GET /api/registrations/{id}`, `PATCH /api/registrations/{id}/cancel` |
| Dashboard | `GET /api/dashboard/summary` |

Errors always use the same shape:

```json
{
  "success": false,
  "timestamp": "2026-06-29T09:00:00Z",
  "message": "Human-readable message",
  "errors": ["Optional field-level details"]
}
```

All dates are stored and returned in UTC.

## Known limitations

- There is no authentication or authorization.
- There are no automated tests; the API was tested manually with Swagger.
- Migrations are applied manually; there is no migration runner and no rollback.
- `seed.sql` is not idempotent; run it once on empty tables.
- Log files in `logs/` rotate daily but are never cleaned up.