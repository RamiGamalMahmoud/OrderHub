# Architecture

OrderHub is a single ASP.NET Core 9 Blazor Server application backed by PostgreSQL.
Everything runs in one process; there is no separate API, worker, or desktop client.

## Solution layout

| Project | Responsibility |
| --- | --- |
| `OrderHub.Web` | Blazor Server host, Razor components, file controllers, cookie authentication, permission authorization, HTTP pipeline, DI composition (`Program.cs`). |
| `OrderHub.Application` | MediatR requests and handlers, DTOs/contracts, validation, permission attributes, `Result`/`Result<T>`. |
| `OrderHub.Domain` | Entities, value objects, order lifecycle and reassignment rules, pricing, inventory and work-record invariants. No infrastructure dependencies. |
| `OrderHub.Infrastructure` | EF Core `AppDbContext`, entity configurations, repositories, migrations, read services, PDF generation, local file storage, numbering. |
| `OrderHub.Tests` | xUnit tests for Application/Domain/Infrastructure plus a Node test under `OrderHub.Tests/Web`. It does not reference `OrderHub.Web`. |

`OrderHub.sln` contains exactly these five projects.

## Request flow

1. A Razor component calls `IRequestExecutor`.
2. `AuthenticatedRequestExecutor` creates a DI scope, resolves the current user, builds an `OperationUser`, and dispatches the MediatR request.
3. `AuthorizationBehavior` reads `[RequirePermission]` / `[RequireHigherManager]` from the request record and rejects unauthorized requests before the handler runs.
4. The handler works with domain entities and repositories.
5. Persistence goes through `IUnitOfWork.SaveChangesAsync`, which allocates monthly work-record numbers and translates persistence failures into `PersistenceException` / `PersistenceConflictException`.

Components never call repositories or `AppDbContext` directly. Calling a handler directly (as most tests do) bypasses authorization by design.

## Authorization

- Authentication is cookie based; authorization uses permission features and levels (`PermissionFeature` × `PermissionLevel`).
- `role_permissions` stores the role matrix with levels `Read`, `Write`, and `Approve`. Sealed roles (for example `sales`, `manager`, and `storekeeper`) are seeded by migrations and cannot be renamed or re-permissioned through the UI.
- Web authorization handlers/middleware map the current user's roles to feature levels. `AuthorizationDeniedMiddleware` redirects real browser navigations to `/access-denied` and returns `403 Forbidden` for other requests.
- New commands/queries must carry the appropriate authorization attribute. Missing attributes silently make a request callable by any authenticated user.

## Persistence conventions

- All SQL identifiers are snake_case (`UseSnakeCaseNamingConvention`). Raw SQL must use snake_case names.
- One `AppDbContext` per scope; `AppDbContextFactory` creates short-lived contexts for read services and background-style work.
- Ledger and history rows (`inventory_movements`, order/work-record/step status history, `order_delivery_movements`, `standalone_trip_histories`, stock receipts) are immutable: `AppDbContext` rejects updates and deletes (with narrow, explicit exceptions such as marking a movement `Reversed` and binding draft receipt attachments). Corrections are new reversing rows.
- `StockBalance` is a cached projection of the immutable ledger plus active allocations. It can be rebuilt by a higher manager.
- Migrations are applied explicitly; the app does not auto-migrate at startup.

## UI and localization

- UI culture is pinned to `ar`; user-facing text is Arabic and the layout is RTL.
- Numbers and dates that must show Latin digits are formatted explicitly (for example `CultureInfo.InvariantCulture`), never with the OS default.
- MudBlazor provides components; Tailwind is compiled by `TailwindBlazor`. A build rewrites `OrderHub.Web/wwwroot/css/tailwind.css`.
- HTTPS redirection is intentionally disabled for the offline/LAN Windows-service deployment.

## Files, documents, and health

- Uploaded files and generated documents are stored under `ApplicationData:RootPath`, never in the repository.
- QuestPDF generates commercial documents; snapshots on documents preserve the values used at issue time.
- `/health` is an anonymous health-check endpoint.
- Configuration comes from standard ASP.NET Core sources; keep the base `appsettings.json` credential free and supply connection strings/paths per environment.

## Tests

- Database-backed tests use the dedicated PostgreSQL database `orderhub_test` (Docker Compose, host port `5433`).
- `PostgresTestDatabase` validates `ORDERHUB_TEST_POSTGRES`, refuses any database whose name is not `orderhub_test`, takes a PostgreSQL advisory lock, applies all migrations once, and exposes `Reset()` to truncate every public table except `__EFMigrationsHistory`; database-backed fixtures call it before seeding.
- xUnit parallelization is disabled across the assembly because all database tests share one schema.
- Tests that need persistence must seed their own parent rows (users, cities, clients, suppliers, deliverymen, carriers). There is no disabled-FK escape hatch.
- Constraint tests assert PostgreSQL `SqlState` values (`23503`, `23505`, `23514`) rather than provider-specific text.
