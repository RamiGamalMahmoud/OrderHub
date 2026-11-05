# AGENTS.md — Guide for AI Agents working on OrderHub

Verified against the codebase and CI. Keep every line high-signal only — if the repo already explains it, leave it out.

## What this project is

**OrderHub** — ASP.NET Core **Blazor** (Interactive Server, .NET 9) ERP app for orders, products, clients, suppliers, delivery workflows, commercial documents, users, and role-based permissions.

- UI language **Arabic**, layout **RTL** (`<html lang="ar" dir="rtl">`) — all user-facing strings are Arabic, and layouts mirror with logical properties (`ps/pe/ms/me`), not `left/right`.
- Stack pinned by `global.json` (SDK 9.0.100): MudBlazor 9.11.0, MediatR 14.2, EF Core + **PostgreSQL**, QuestPDF (Community license), FluentValidation, **Tailwind CSS v4 via the `TailwindBlazor` 0.2.0 NuGet package** (no npm pipeline).

## Repo layout (`OrderHub.sln`, 5 projects)

| Project | Role |
|---|---|
| `OrderHub.Domain` | Entities, value objects (`EntityName`, `Money`), enums, lifecycle rules, pricing (`PricingCalculator`). No external dependencies. |
| `OrderHub.Application` | CQRS Commands/Queries (MediatR) under `Features/<Area>/<UseCase>` with `Contracts/` records. References Domain only. |
| `OrderHub.Infrastructure` | EF Core `AppDbContext`, `Configurations/`, `Migrations/`, repositories, QuestPDF document generation, file storage, `DependencyInjection/`. |
| `OrderHub.Web` | Host: `Program.cs`, Razor components (28 routes), cookie auth, permission authorization, `Controllers/Files/*` PDF endpoints, static assets. |
| `OrderHub.Tests` | xUnit; SQLite for all tests. Also `Web/order-local-time.test.mjs` (Node). |

Non-obvious quirks:
- Misspelled-but-real paths (do not "fix" casually; match them): `OrderHub.Infrastructure/Reopsitories/`, `DesignTimeAppDbContextFactrory.cs`, `Orders/OrderDetials.razor`, `Setup/CreateAdmin.razor.cs.cs`.
- `OrderHub.Web/Resources/Images/*.png` are **required runtime assets** — CI's release audit fails without them.
- `wwwroot/fonts/*.ttf` are intentionally excluded from `Content` in the csproj.

## Architecture rules (follow exactly)

1. **Vertical slice CQRS**: everything goes through MediatR. Components inject `IRequestExecutor` and call `await RequestExecutor.ExecuteAsync(new SomeQuery(...))` — never call repositories from pages.
2. **Result pattern**: handlers return `Result`/`Result<T>` (`OrderHub.Domain.Common.Result`) with `IsSuccess` + `ErrorMessage` + `ErrorCode` (`"Conflict"` = optimistic-concurrency version conflict). Do not throw for expected failures.
3. **Domain entities**: private setters, private parameterless ctor + `internal` ctor, factory/behavior methods with validation. DTO mapping happens in Application/Infrastructure — never expose entities to the UI.
4. **EF configurations** live in `OrderHub.Infrastructure/Configurations/*Configuration.cs`; migrations go to `OrderHub.Infrastructure/Migrations`, applied via the standard `dotnet ef` workflow (design-time factory: `DesignTimeAppDbContextFactrory.cs`). **There is no `--migrate` flag on the Web app.** Naming pattern: descending timestamps like `20261030000000_Name.cs`.
5. **Authorization**: page-level `[PermissionAuthorize(PermissionFeature.X, PermissionLevel.Read|Write)]` (+ `[Authorize(Roles = "Admin")]` in Administration). Row-level checks via `PermissionAccess.IsAllowedAsync(...)`. `AuthorizationDeniedException` → middleware redirects to `/access-denied`. `PermissionFeature` enum is the backing model for the roles editor matrix.
6. **History/audit**: order lifecycle owns `OrderStatusHistory`/`DeliveryStepStatusHistory` rows; the workflow page renders them via `GetOrderHistoryQuery` (paged 5/page). Legacy migration entries carry `Source == OrderHistorySource.LegacyMigration` — keep their disclaimer text.

## Build / test (exact commands, from repo root)

```powershell
dotnet restore OrderHub.sln                                   # required first; CI chain depends on it
dotnet build OrderHub.sln --configuration Release             # restore → build order matters
dotnet test  OrderHub.sln --configuration Release --no-build # CI uses --no-build after Release build
node OrderHub.Tests/Web/order-local-time.test.mjs            # only test needing Node (CI pins Node 24)
```

Dev run: `dotnet run --project OrderHub.Web` — needs `ConnectionStrings:DefaultConnection` + `ApplicationData:RootPath` (user-secrets or env; it throws at startup if missing). First run goes through `/setup/admin`.

### Testing model (no PostgreSQL opt-in)

- All DB tests are **SQLite in-memory** (`SqliteConnection("Data Source=:memory:")` + `EnsureCreated`) with `UseSnakeCaseNamingConvention()`. Any new EF column must therefore survive snake_case + SQLite FK constraints — keep test options identical to existing tests.
- There are **no** env-var-gated integration tests in the repo (the `README.md` mention of a `docs/` folder is stale — it does not exist).

## Config & release (verified from `.github/workflows/web-release.yml`)

- Production releases are created **only** by pushing an unused canonical `vX.Y.Z` tag pointing at **the latest commit of `master`** — CI refuses anything else, then restore→build→test (Postgres/rehearsal intentionally disabled in CI) → Node test → **self-contained `win-x64` publish** with `-p:Version=X.Y.Z` → zip + sha256 → draft→publish GitHub release. **Agents must not push tags.**
- CI forbids in the package: `appsettings.*.json` other than base, `secrets`, `launchSettings.json`, source files. Base `appsettings.json` must contain **only `Logging` and `AllowedHosts`** — production config comes from external `%ProgramData%/OrderHub/Config/appsettings.Server.json`. Never add connection strings/secrets to source config.
- Runs as Windows service `OrderHubWeb` (registered via `AddWindowsService` in `Program.cs`). Health probe: anonymous `/health`.
- `/setup/admin` exists but is a **non-functional WIP stub**: its `CreateAdminAsync` body is commented out (no command wired; fake `Task.Delay(3000)`), and there is no "no users exist" gating server-side. Its code-behind is the odd filename `CreateAdmin.razor.cs.cs` (double `.cs`) — edit *that* file, it compiles as the partial.

## Web / UI conventions

- **Tailwind pipeline is wired, not npm**: csproj properties `TailwindInputFile=Styles/app.css` (currently only `@import "tailwindcss";`) → `TailwindOutputFile=wwwroot/css/tailwind.css`, and `builder.UseTailwind()` in `Program.cs`. Razor sources are scanned on build; put custom tokens in `Styles/app.css` `@theme`.
- CSS link order in `Components/App.razor` matters: MudBlazor css → `app.css` → `css/order-workflow.css` → `OrderHub.Web.styles.css` → `css/tailwind.css` (last, wins specificity).
- Only one JS file exists (`wwwroot/js/order-local-time.js`, covered by the Node test). **Do not add JavaScript** — Blazor/MudBlazor can express everything the UI needs.
- **MudMenu/MudPopover content renders into the global `MudPopoverProvider`** (both providers are set up in `MainLayout`) → any styling of menus/popovers must be **global CSS**, never component-scoped isolation.
- `MainLayout.razor` and `StandaloneLayout.razor` each carry a **duplicated inline `MudTheme`** (`Tajawal` font + blue-600 palette) — keep them in sync when touching either, or merge deliberately.
- `NavMenu.razor` is the single source of nav items (permission-filtered groups; Dropdown + Drawer variants).
- Status tones are centralized: `OrderWorkflowPresentation.StatusTone(...)` → `order-tone-*` classes styled in `wwwroot/css/order-workflow.css` (also used on Orders list rows and details page).
- **Numbers always `CultureInfo.InvariantCulture`** so digits stay Latin under Arabic culture (e.g. `value.ToString("N2", CultureInfo.InvariantCulture)`).
- Established page patterns: list pages load via `GetAll*Query` with in-memory search + `MudTable`/`MudDataGrid`, permission-gated actions; editors are `*EditorDialog.razor` (MudDialog) driven by `*EditorState.cs` validation + `MudForm`; the order editor is a multi-step page under `Editor/`.
- MudBlazor 9.11 gotchas: `MudDataGrid` `CurrentPage` is **0-based** and its setter triggers a server reload (`NavigateTo(Page)` takes a `Page` object); guard every async button with busy flags — hover/loading races double-fire.

## House rules for agents

1. **Do not commit unless explicitly asked.** The working tree may carry uncommitted user work — never discard, stash, or `checkout --` files you did not author.
2. Match the dominant style of each file (CQRS files separated by blank lines; verbose multiline formatting; explicit `>` attribute-tag style on big MudBlazor components).
3. Ongoing redesign plan/task registry lives **outside the repo** at `C:\Users\ramig\.opencode\plan\orderhub-redesign-tasks.md` — update it when executing; do not open plan-mode changes inside the repo.
4. Backend extension touchpoints in order: entity validation → Application handler/contracts → Infrastructure configuration + migration → UI contract usage. Keep `InternalsVisibleTo("OrderHub.Tests")` intact.
5. Secrets never in source (see Config section).
