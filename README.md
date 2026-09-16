# MeetingRoomReservation

A booking system for a limited set of meeting rooms where concurrent requests for the same time
slot must never result in a double-booking, and booking status changes are reflected to all
viewers in real time.

Built with **ASP.NET Core / .NET 10** (backend) and **Blazor Server** (frontend), following
**Clean Architecture** + **CQRS**. Development was carried out with active use of
[Claude Code](https://claude.com/claude-code) — see [`CLAUDE.md`](CLAUDE.md) for the full
architecture reference that guided the sessions.

## Live demo

| App | URL |
|---|---|
| Web (frontend) | https://webapp-reenbitbookingsystem-web-dev-dvgceefghjfdatb0.polandcentral-01.azurewebsites.net |
| Api (backend) | https://webapp-reenbitbookingsystem-dev-bnaph9e5ccdaewfe.polandcentral-01.azurewebsites.net |

### Test accounts

Seeded automatically on first run by `DatabaseSeeder`:

| Role  | Email                    | Password    |
|-------|--------------------------|-------------|
| Admin | admin@meetingrooms.local | Admin#12345 |
| User  | user@meetingrooms.local  | User#12345  |

These are demo-only credentials, seeded for local/dev use — never reuse them anywhere else.

## What it does

- Any authenticated user can browse resources (meeting rooms), view a resource's schedule for a
  given date, and book a free slot.
- An **Admin** can additionally create, edit, and delete resources, and see bookings across all
  users.
- Booking a slot two people are racing for **never double-books**: exactly one request succeeds,
  the other gets a clear `409 Conflict` — never a silent overwrite, never a `500`.
- Everyone currently viewing a resource's schedule sees slot status flip in real time via
  **Azure SignalR Service**, no page refresh needed.

## Architecture

Clean Architecture, strict inward dependency direction, CQRS via MediatR:

```
Domain          → Entities, value objects, domain errors — no external dependencies
Application     → Commands/Queries, handlers, validators, repository interfaces
Infrastructure  → EF Core DbContext, repositories, migrations, SignalR notifier, DI wiring
Api             → Controllers, SignalR Hub, DTOs, composition root
Web             → Blazor Server frontend
Tests           → Unit tests + automated concurrency test
```

The Domain layer applies **tactical DDD patterns**: aggregate roots (`Resource`, `Booking`) that
can only be constructed/mutated through factory methods returning `Result`/`Result<T>` (invalid
states are unrepresentable), immutable value objects (`WorkingHours`, `TimeSlot`), and repositories
as the aggregate-level persistence boundary. It stops short of full DDD by design, though: the
one invariant that matters most — no double-booking — is enforced at the database level (a unique
index) rather than in-memory by the aggregate, because slots aren't materialized as rows until
booked. See [Concurrency control](#concurrency-control--design-decision) below for why.

Request flow: **Controller → MediatR Command/Query → Handler → Domain → Repository (EF Core) →
Azure SQL**, with FluentValidation running as a MediatR pipeline behavior, and a `Result` /
`Result<T>` object propagating expected failures (validation, not-found, conflict) without
throwing exceptions.

## Concurrency control — design decision

Booking conflicts are prevented with a **unique index on `Booking(ResourceId, SlotStart)`** at the
database level, combined with an **insert-only write path** — deliberately not a
"check if free, then insert" two-step process, and not classic optimistic concurrency via
`RowVersion`:

- Creating a booking is a direct `INSERT`; the handler never queries "is this slot free?" first —
  the database itself is the single source of truth.
- If two requests race for the same `(ResourceId, SlotStart)`, SQL Server's unique index guarantees
  only one `INSERT` succeeds; the other fails with a unique-constraint-violation `SqlException`
  (error 2601/2627).
- The repository catches specifically that error and translates it into a `Result` carrying a
  `ConflictError`, which the API maps to `409 Conflict`.
- **Why not `RowVersion`-based optimistic concurrency**: slots aren't materialized as rows until
  booked, so there is no pre-existing "free slot" row to attach a version token to.
- **Why not pessimistic locking** (`UPDLOCK`/`HOLDLOCK`, serializable transactions): booking
  requests are short-lived with no work between check and write, so relying on the database's own
  uniqueness guarantee avoids holding locks across a network round-trip.

This is verified by an automated test — see [Testing](#testing) below.

## Real-time updates

`SignalRBookingNotifier` (Infrastructure) publishes to `BookingHub` (Api) whenever a booking is
created or cancelled, via the Azure SignalR Service Management SDK (decoupled from the Hub type,
which lives in Api, to keep Infrastructure from depending on it). Clients viewing a resource's
schedule join a SignalR group per `resourceId` and get pushed updates without refreshing. A
notification failure is best-effort and never fails the underlying booking/cancel request.

## Tech stack

| Concern | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core Web API |
| Frontend | Blazor Server |
| Database | Azure SQL Database |
| ORM | Entity Framework Core 10 |
| CQRS / Mediator | MediatR |
| Validation | FluentValidation |
| Object mapping | Mapster |
| Real-time | Azure SignalR Service |
| Auth | ASP.NET Core Identity + JWT, roles `Admin`/`User` |
| API docs | Swashbuckle (Swagger), `Development` only |
| Tests | xUnit + Moq |

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB works for local dev)

### Run locally

```bash
git clone https://github.com/paulSoryu/MeetingRoomReservation
cd MeetingRoomReservation

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project MeetingRoomReservation.Api
dotnet user-secrets set "Azure:SignalR:ConnectionString" "<your SignalR connection string>" --project MeetingRoomReservation.Api
dotnet user-secrets set "Jwt:Key" "<a long random signing key>" --project MeetingRoomReservation.Api

dotnet restore
dotnet run --project MeetingRoomReservation.Api
dotnet run --project MeetingRoomReservation.Web
```

Pending EF Core migrations are applied automatically at startup, and the database is seeded with
demo resources and the test accounts above.

## Testing

```bash
dotnet test
```

The concurrency guarantee is exercised directly, not assumed: `BookingConcurrencyTests` fires 20
simultaneous booking requests at the identical slot against a real SQL Server database, and asserts
exactly one succeeds while the rest come back as `ConflictError` — with exactly one row landing in
the database. Run just that test with:

```bash
dotnet test --filter "FullyQualifiedName~Concurrency"
```

(Requires a reachable SQL Server — defaults to LocalDB.)

## API reference

All endpoints are under `/api`; Swagger UI at `/swagger` in `Development`.

| Method | Route | Description | Role |
|---|---|---|---|
| `GET` | `/api/resources` | List resources | Any authenticated user |
| `GET` | `/api/resources/{id}/schedule` | Free/booked slots for a resource | Any authenticated user |
| `POST` | `/api/resources` | Create a resource | Admin |
| `PUT` | `/api/resources/{id}` | Update a resource | Admin |
| `DELETE` | `/api/resources/{id}` | Remove a resource | Admin |
| `POST` | `/api/bookings` | Book a free slot | Any authenticated user |
| `GET` | `/api/bookings/mine` | Current user's bookings | Any authenticated user |
| `GET` | `/api/bookings` | All bookings across all users | Admin |
| `DELETE` | `/api/bookings/{id}` | Cancel a booking (owner or Admin) | Any authenticated user |
| `POST` | `/api/auth/login` | Exchange email/password for a JWT | Anonymous |

## Development process

This project was built with Claude Code across the full stack — Domain, Application,
Infrastructure, Api, Web, and Tests — with the design decisions and project structure recorded in
[`CLAUDE.md`](CLAUDE.md) as they were made. See the commit history for how the work was broken
down layer by layer.
