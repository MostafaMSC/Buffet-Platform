# Buffet Baghdad — Discovery Platform (Phase 1 MVP)

Helps customers in Baghdad discover which restaurants are running breakfast,
lunch, iftar or sohor buffets on a given day, in a given area. Discovery only
— no booking or payments in this phase.

## Repository layout

```
backend/                             ASP.NET Core 8 — Clean Architecture, 4 projects
  BuffetDiscovery.sln
  src/
    BuffetDiscovery.Domain/          Entities (Restaurant, BuffetOffering, AvailabilityStatus, Area,
                                      User, enums) + RecurrenceEvaluator (pure recurrence-matching logic).
                                      No dependencies on any other project or package.

    BuffetDiscovery.Application/     Use cases, as MediatR commands/queries + handlers, one feature
                                      folder per concern:
                                        Features/Areas, Features/Offerings, Features/Restaurants,
                                        Features/Auth, Features/Dashboard, Features/Admin, Features/Uploads
                                      Common/Interfaces/    repository & service ports (implemented by
                                                             Infrastructure/Api — this is the boundary
                                                             Clean Architecture actually buys us)
                                      Common/Dtos/          request/response contracts
                                      Common/Behaviors/     MediatR pipeline (FluentValidation)
                                      Common/Exceptions/    NotFound/Conflict/Unauthorized/Validation,
                                                             mapped to HTTP responses by Api middleware
                                      Depends only on Domain.

    BuffetDiscovery.Infrastructure/  EF Core DbContext + IEntityTypeConfiguration classes, migrations,
                                      repository implementations, JwtTokenService, BCryptPasswordHasher,
                                      LocalFileStorageService, DbSeeder. Implements Application's
                                      interfaces; depends on Application + Domain.

    BuffetDiscovery.Api/             Thin controllers (build a command/query, call mediator.Send(),
                                      return the result) + CurrentUserService (the one piece that
                                      needs IHttpContextAccessor, so it lives here rather than in
                                      Infrastructure) + Program.cs wiring + wwwroot/uploads.
                                      Depends on Application + Infrastructure.

frontend/                            React 19 + TypeScript + Vite
  src/
    pages/                           CustomerHome, RestaurantDetail, Restaurant login/signup,
                                      RestaurantDashboard (+ dashboard/ subpages), AdminLogin, AdminDashboard
    components/                      Header, FilterBar, OfferingCard, PhotoUploader, ProtectedRoute
    context/AuthContext.tsx          JWT/session state
    i18n.ts                         Arabic/English bilingual strings + RTL/LTR switching
    api/client.ts                    Axios instance (JWT header, /api base)

docker-compose.yml                   Postgres 16 for local dev
```

### Why Clean Architecture + CQRS here

The dependency rule is what actually matters: `Domain` has zero dependencies,
`Application` depends only on `Domain` and defines interfaces (`IRestaurantRepository`,
`IJwtTokenService`, etc.) rather than concrete EF Core/BCrypt/JWT libraries, and
`Infrastructure`/`Api` are the only projects allowed to depend on those concrete
libraries. That boundary is compiler-enforced — `Application` project references
don't include Npgsql or EF Core at all, so a handler physically cannot reach into
`DbContext` directly. Every use case is a MediatR command or query with its own
handler (and, where it has real invariants, its own FluentValidation validator),
which is why a controller action is typically 2-3 lines: build the request, send
it, translate the result. Application-layer exceptions (`NotFoundException`,
`ConflictException`, `UnauthorizedException`, FluentValidation's `ValidationException`)
are translated to HTTP responses centrally by `Api/Middleware/ExceptionHandlingMiddleware.cs`
instead of being hand-rolled per controller action.

## Data model

- **Area** — fixed lookup list of Baghdad neighborhoods (bilingual names), used for filtering.
- **Restaurant** — profile, area, contact info, status (`Pending`/`Approved`/`Suspended`/`Rejected`).
- **BuffetOffering** — one buffet a restaurant runs (meal type, price, hours, photos, recurrence rule:
  `Daily`, `SpecificWeekdays`, `RamadanMode` with a date range, or `OneOff`).
- **AvailabilityStatus** — the concrete per-date on/off record customers actually see. Rows are
  materialized lazily from the offering's recurrence rule (`Domain/Services/RecurrenceEvaluator`,
  called from the `BrowseOfferingsQuery`/`GetDashboardOfferingsQuery` handlers) the first time a
  date is queried or loaded on the dashboard, so a restaurant can always override a single day
  (e.g. "closed today") without touching the recurrence rule itself.
- **User** — phone + password login, role `RestaurantOwner` (linked to one `Restaurant`) or `Admin`.

## Running locally

### 1. Database

```bash
docker compose up -d          # Postgres on localhost:5433 (user/db: buffet / buffet_discovery)
```

(If you don't want Docker, point `ConnectionStrings:Default` in
`backend/src/BuffetDiscovery.Api/appsettings.json` at any local Postgres 16 instance instead.)

### 2. Backend API

The `DbContext` and migrations live in `BuffetDiscovery.Infrastructure`, so EF Core tooling
needs both `--project` (where the migrations are) and `--startup-project` (where the app —
and `appsettings.json` — is hosted) from the `backend/` directory:

```bash
cd backend
dotnet ef database update \
  --project src/BuffetDiscovery.Infrastructure \
  --startup-project src/BuffetDiscovery.Api

cd src/BuffetDiscovery.Api
dotnet run                    # http://localhost:5080 — applies migrations & seeds sample data on startup
```

(`dotnet run` also calls `Database.MigrateAsync()` on startup, so the explicit `dotnet ef
database update` above is mostly useful if you want migrations applied without starting the
API, or when adding a new migration with `dotnet ef migrations add <Name>` using the same
`--project`/`--startup-project` pair.)

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
  (`backend/src/BuffetDiscovery.Infrastructure/Persistence/DbSeeder.cs`, `SeedAreasAsync`). Edit
  that list (or edit rows directly in the `Areas` table) to match the real list — the frontend
  filter and restaurant onboarding form both read from `/api/areas`, so no frontend changes are needed.
- **Auth**: phone number + password with JWT, intentionally minimal (no OTP/SMS, no password reset)
  to match "simple auth is fine" in the brief.
- **Photo uploads**: stored on local disk under `wwwroot/uploads` and served as static files. Fine
  for an MVP / single-instance deployment; swap for blob storage (S3/Azure Blob) before scaling to
  multiple app instances.
- **Out of scope** (per the brief): booking/reservations, payments, push notifications,
  reviews/ratings, native apps, WhatsApp/Telegram bots.
