# CLAUDE.md

This file gives Claude Code context for working on this repository. Keep it up to date as the project evolves — it is read automatically at the start of every session.

## Project

**MeetingRoomReservation** — a booking system for a limited set of meeting rooms where concurrent requests for the same time slot must never result in a double-booking, and booking status changes must be reflected to all viewers in real time.

## Architecture

Clean Architecture with a strict dependency direction (outer layers depend on inner layers, never the reverse), combined with **CQRS** via **MediatR**.

```
MeetingRoomReservation.Domain          → Entities, value objects, domain errors — no external dependencies
MeetingRoomReservation.Application     → Commands/Queries, handlers, validators, repository interfaces
MeetingRoomReservation.Infrastructure  → EF Core DbContext, repositories, migrations, SignalR notifier, DI wiring
MeetingRoomReservation.Api             → Controllers, SignalR Hub, request/response DTOs, mapping, composition root
MeetingRoomReservation.Web             → Blazor Server frontend (Razor components, SignalR client)
MeetingRoomReservation.Tests           → Unit tests + automated concurrency test
```

Request flow: **Controller → MediatR Command/Query → Handler → Domain → Repository (EF Core) → Azure SQL**, with **FluentValidation** running as a MediatR pipeline behavior before any handler executes, and a `Result` / `Result<T>` object propagating success/failure without throwing exceptions for expected business-rule violations (including booking conflicts).

The **Blazor Server** app (`MeetingRoomReservation.Web`) talks to the Api project's HTTP endpoints for commands/queries, and separately subscribes to the SignalR Hub for real-time slot-status updates. Blazor Server itself runs over SignalR internally, but that internal connection is unrelated to — and does not replace — the Azure SignalR Service connection used to broadcast booking updates to all viewers.

## Tech Stack

| Concern | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core Web API |
| Frontend | Blazor Server |
| Database | Azure SQL Database |
| ORM | Entity Framework Core 10 (`Microsoft.EntityFrameworkCore.SqlServer`) |
| CQRS / Mediator | MediatR |
| Validation | FluentValidation (as a MediatR pipeline behavior) |
| Object Mapping | Mapster |
| Real-time | Azure SignalR Service (Default mode) |
| Auth | ASP.NET Core Identity + JWT, roles: `Admin`, `User` |
| API Docs | Swashbuckle (Swagger) — `Development` environment only |
| Error Handling | `IExceptionHandler` (`GlobalExceptionHandler`) + RFC 7807 `ProblemDetails` |
| Tests | xUnit |

## Project Structure

```
MeetingRoomReservation.Domain/
├── Models/
│   ├── Resources/          Resource aggregate root (meeting room) + ValueObjects
│   ├── Bookings/           Booking aggregate + ValueObjects (TimeSlot)
│   └── Users/               Role-related domain concerns, if any live here
└── Results/                 Result / Result<T> + typed DomainError hierarchy per aggregate

MeetingRoomReservation.Application/
├── Features/
│   ├── Resources/           Commands (Create/Update/Delete — Admin only) & Queries (List, GetSchedule)
│   └── Bookings/            BookSlot command, CancelBooking command, GetBookings query
├── Interfaces/               IResourceRepository, IBookingRepository, IBookingNotifier
└── Validation/                ValidationBehavior<TRequest,TResponse> pipeline + ValidationError / NotFoundError / ConflictError

MeetingRoomReservation.Infrastructure/
├── Data/
│   ├── ApplicationDbContext.cs
│   ├── Repositories/         ResourceRepository, BookingRepository
│   └── Seeders/                DatabaseSeeder (seeds demo resources + admin/user test accounts)
├── Configurations/            EF Core Fluent API entity configurations (unique index on Booking(ResourceId, SlotStart) here)
├── Realtime/                   SignalRBookingNotifier : IBookingNotifier
├── Migrations/
└── Extensions/                 DependencyInjection.cs — AddInfrastructure()

MeetingRoomReservation.Api/
├── Controllers/                ResourcesController, BookingsController
├── Hubs/                        BookingHub
├── DTOs/
├── GlobalExceptionHandler.cs
└── Program.cs                   Composition root

MeetingRoomReservation.Web/
├── Components/Pages/            ResourceSchedule.razor, MyBookings.razor, AdminResources.razor
├── Services/                    Typed HTTP client wrapping the Api, SignalR client wrapper
└── Program.cs

MeetingRoomReservation.Tests/
├── Bookings/                     Unit tests for handlers/validators
└── Concurrency/                  BookingConcurrencyTests — fires N simultaneous requests at one slot
```

## Domain Model & Business Rules

**Resource** — aggregate root representing a bookable meeting room: name, working hours, and slot duration. Slots are **not** materialized as their own entities/table — they are computed on the fly from working hours + slot duration, minus whatever already has a `Booking`. Create/update/remove restricted to `Admin`.

**Booking** — created from a `ResourceId`, a `TimeSlot` (`SlotStart` + `SlotEnd`, derived from the resource's working hours/slot duration), and the requesting user. A resource's slot can have at most one active booking at a time.

**Concurrency control**: a **unique index on `Booking(ResourceId, SlotStart)`** at the database level, combined with an insert-only write path — not a "check if free → then insert" two-step process, and not classic optimistic concurrency via `RowVersion`.

- Booking creation is a direct `INSERT` — the handler does not query "is this slot free?" first. The database itself is the single source of truth for whether the slot is taken.
- If two requests race for the same `(ResourceId, SlotStart)`, SQL Server's unique index guarantees only one `INSERT` succeeds; the other fails with a unique-constraint-violation `SqlException` (error numbers 2601/2627).
- The repository/handler catches that specific `SqlException`, translates it into a `Result` carrying a `ConflictError`, which the controller maps to **`409 Conflict`** — never a silent overwrite, never an unhandled `500`. Other `SqlException`s are not swallowed by this check — only the specific constraint-violation codes are treated as a booking conflict.
- Why not `RowVersion`-based optimistic concurrency: there is no pre-existing "free slot" row to attach a `RowVersion` token to (slots aren't materialized — see above), so there is nothing for EF Core to version-check against before the first successful booking exists.
- Why not pessimistic locking (`SELECT ... WITH (UPDLOCK, HOLDLOCK)` / serializable transactions): with short-lived booking requests and no long-running work between check and write, relying on the database's own uniqueness guarantee avoids holding locks across a network round-trip and scales better under contention.

## Real-time Updates

`SignalRBookingNotifier` (Infrastructure) publishes to the `BookingHub` (Api) whenever a booking is created/cancelled. Clients viewing a resource's schedule join a SignalR group per `resourceId` and receive slot-status updates without refreshing. The Blazor Server frontend's SignalR client subscribes to this Hub — separate from Blazor's own internal circuit connection.

## API Reference

All endpoints are under `/api`. Swagger UI at `/swagger` in `Development`.

### Resources — `/api/resources`

| Method | Route | Description | Role |
|---|---|---|---|
| `GET` | `/api/resources` | List resources | Any authenticated user |
| `GET` | `/api/resources/{id}/schedule` | Free/booked slots for a resource | Any authenticated user |
| `POST` | `/api/resources` | Create a resource | Admin |
| `PUT` | `/api/resources/{id}` | Update a resource | Admin |
| `DELETE` | `/api/resources/{id}` | Remove a resource | Admin |

### Bookings — `/api/bookings`

| Method | Route | Description | Role |
|---|---|---|---|
| `POST` | `/api/bookings` | Book a free slot (optimistic-concurrency protected) | Any authenticated user |
| `GET` | `/api/bookings/mine` | Current user's bookings | Any authenticated user |
| `GET` | `/api/bookings` | All bookings across all users | Admin |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Access to the Azure SQL Database instance (or a local SQL Server for dev)

### Run locally

```bash
git clone https://github.com/paulSoryu/MeetingRoomReservation
cd MeetingRoomReservation

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>" --project MeetingRoomReservation.Api
dotnet user-secrets set "Azure:SignalR:ConnectionString" "<your SignalR connection string>" --project MeetingRoomReservation.Api

dotnet restore
dotnet run --project MeetingRoomReservation.Api
dotnet run --project MeetingRoomReservation.Web
```

Pending EF Core migrations are applied automatically at startup, and the database is seeded with demo resources plus a test `Admin` and test `User` account (see README for credentials).

## Configuration

Connection settings live under `ConnectionStrings:DefaultConnection` and `Azure:SignalR:ConnectionString`. Never commit real values — use `dotnet user-secrets` locally and Azure App Service Application Settings in deployment.

## Database & Migrations

The `Infrastructure` project owns the EF Core `ApplicationDbContext`, entity configurations, and migrations.

```bash
dotnet ef migrations add <MigrationName> \
  --project MeetingRoomReservation.Infrastructure \
  --startup-project MeetingRoomReservation.Api
```

## Error Handling

Domain and validation failures never throw; they flow back as a `Result` whose `DomainError` is translated into an RFC 7807-style `ProblemDetails` response:

| Error type | HTTP status |
|---|---|
| Validation | `400 Bad Request` |
| Not Found | `404 Not Found` |
| Conflict (double-booking / business rule violation) | `409 Conflict` |
| Unhandled | `500 Internal Server Error` |

Unhandled exceptions are additionally caught globally by `GlobalExceptionHandler`.

## Testing

`MeetingRoomReservation.Tests/Concurrency/BookingConcurrencyTests` fires multiple simultaneous booking requests at the same slot (via `Task.WhenAll` against the handler or a test server) and asserts that exactly one succeeds with the rest returning a conflict result — never a duplicate booking, never an unhandled exception.

```bash
dotnet test
dotnet test --filter "FullyQualifiedName~Concurrency"
```

## Code Style

- Standard C# conventions (PascalCase for public members, `_camelCase` for private fields).
- Async methods always suffixed `Async`.
- Controllers stay thin — business logic lives in MediatR handlers, not controllers.
- Entities are constructed/mutated only through static factory methods (`Create`, `Update`) returning `Result`/`Result<T>` — invalid states should be unrepresentable.
- XML doc comments on public controller methods (surfaced in Swagger).

## Current Status

[Update as work progresses, e.g.:]
- [x] Azure infrastructure provisioned (SQL, SignalR, Web App)
- [ ] Domain models and initial migration
- [ ] Identity/roles and auth
- [ ] Booking command with optimistic concurrency
- [ ] SignalR Hub + Blazor client integration
- [ ] Automated concurrency test
- [ ] Deployment
