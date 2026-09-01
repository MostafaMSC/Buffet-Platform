# Buffet Baghdad — Discovery Platform (Phase 1 MVP)

Helps customers in Baghdad discover which restaurants are running breakfast,
lunch, iftar or sohor buffets on a given day, in a given area. Discovery only
— no booking or payments in this phase.

## Repository layout

```
backend/                     ASP.NET Core 8 Web API + EF Core (Postgres)
  BuffetDiscovery.sln
  src/BuffetDiscovery.Api/
    Entities/                Domain model (Restaurant, BuffetOffering, AvailabilityStatus, Area, User, enums)
    Data/                    EF Core DbContext, migrations, DbSeeder
    Dtos/                    Request/response contracts
    Services/                AvailabilityService (recurrence → per-date materialization), JwtTokenService
    Controllers/              Areas, Offerings (public browse), Restaurants (public detail),
                              Auth (signup/login), RestaurantDashboard (owner), Admin, Uploads
    wwwroot/uploads/          Uploaded restaurant/offering photos (served as static files)

frontend/                    React 19 + TypeScript + Vite
  src/
    pages/                   CustomerHome, RestaurantDetail, Restaurant login/signup,
                              RestaurantDashboard (+ dashboard/ subpages), AdminLogin, AdminDashboard
    components/               Header, FilterBar, OfferingCard, PhotoUploader, ProtectedRoute
    context/AuthContext.tsx  JWT/session state
    i18n.ts                  Arabic/English bilingual strings + RTL/LTR switching
    api/client.ts             Axios instance (JWT header, /api base)

docker-compose.yml           Postgres 16 for local dev
```

One ASP.NET Web API project + one React SPA was intentionally kept flat
(no extra Domain/Application/Infrastructure assemblies) — this is a Phase 1
MVP with a small surface area; the folder-per-concern split inside the API
project gives the same separation without solution-file overhead.

## Data model

- **Area** — fixed lookup list of Baghdad neighborhoods (bilingual names), used for filtering.
- **Restaurant** — profile, area, contact info, status (`Pending`/`Approved`/`Suspended`/`Rejected`).
- **BuffetOffering** — one buffet a restaurant runs (meal type, price, hours, photos, recurrence rule:
  `Daily`, `SpecificWeekdays`, `RamadanMode` with a date range, or `OneOff`).
- **AvailabilityStatus** — the concrete per-date on/off record customers actually see. Rows are
  materialized lazily from the offering's recurrence rule (`AvailabilityService`) the first time a
  date is queried or loaded on the dashboard, so a restaurant can always override a single day
  (e.g. "closed today") without touching the recurrence rule itself.
- **User** — phone + password login, role `RestaurantOwner` (linked to one `Restaurant`) or `Admin`.

## Running locally

### 1. Database

```bash
docker compose up -d          # Postgres on localhost:5432 (user/db: buffet / buffet_discovery)
```

(If you don't want Docker, point `ConnectionStrings:Default` in
`backend/src/BuffetDiscovery.Api/appsettings.json` at any local Postgres 16 instance instead.)

### 2. Backend API

```bash
cd backend/src/BuffetDiscovery.Api
dotnet ef database update     # applies migrations
dotnet run                    # http://localhost:5080 — applies migrations & seeds sample data on startup
```

Swagger UI is available at `http://localhost:5080/swagger` in Development.

Seeded accounts:
- **Admin**: phone `07700000000`, password `Admin@123`
- 9 sample approved restaurants across different Baghdad areas with breakfast/lunch/iftar/sohor
  offerings (including one `RamadanMode` and one `SpecificWeekdays` example) so the browse page has
  data immediately.

### 3. Frontend

```bash
cd frontend
npm install
npm run dev                   # http://localhost:5173, proxies /api and /uploads to :5080
```

Open `http://localhost:5173`. The customer browse/filter/detail flow needs no login. Use
`/restaurant/signup` to create a restaurant (goes into `Pending`), then `/admin/login` with the
seeded admin account to approve it before it appears publicly.

## Notes on scope decisions

- **Language**: Bilingual Arabic/English with a header toggle (RTL for Arabic, LTR for English),
  per the project's request. Language preference is remembered per browser (`localStorage`).
- **Areas**: seeded with a placeholder list of 15 well-known Baghdad neighborhoods
  (`backend/.../Data/DbSeeder.cs`, `SeedAreasAsync`). Edit that list (or edit rows directly in the
  `Areas` table) to match the real list — the frontend filter and restaurant onboarding form both
  read from `/api/areas`, so no frontend changes are needed.
- **Auth**: phone number + password with JWT, intentionally minimal (no OTP/SMS, no password reset)
  to match "simple auth is fine" in the brief.
- **Photo uploads**: stored on local disk under `wwwroot/uploads` and served as static files. Fine
  for an MVP / single-instance deployment; swap for blob storage (S3/Azure Blob) before scaling to
  multiple app instances.
- **Out of scope** (per the brief): booking/reservations, payments, push notifications,
  reviews/ratings, native apps, WhatsApp/Telegram bots.
