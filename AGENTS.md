# AGENTS.md — Guide for AI Agents working on OrderHub

Instructions for coding agents (Claude Code, OpenCode, Copilot, …) working in this repository. Read this before making changes.

---

## 1. What this project is

**OrderHub** — an ASP.NET Core **Blazor** (Interactive Server, .NET 9) ERP app for managing orders, products, clients, suppliers, delivery workflows, commercial documents, users, and role-based permissions.

- UI language is **Arabic**, layout is **RTL** (`<html lang="ar" dir="rtl">`) — all user-facing strings are Arabic.
- Stack: .NET 9 (pinned by `global.json`), MudBlazor 9.11.0, MediatR 14.2, EF Core + **PostgreSQL**, QuestPDF (Community license), FluentValidation, Tailwind CSS v4 (via the `TailwindBlazor` MSBuild package).

## 2. Repository layout (5 projects in `OrderHub.sln`)

| Project | Role |
|---|---|
| `OrderHub.Domain` | Entities, value objects (`EntityName`, `Money`…), enums, lifecycle rules, pricing (`PricingCalculator`). No external dependencies. |
| `OrderHub.Application` | CQRS Commands/Queries (MediatR) under `Features/`, DTOs, interfaces, authorization abstractions. References Domain only. |
| `OrderHub.Infrastructure` | EF Core `AppDbContext`, entity `Configurations/`, `Migrations/` (PostgreSQL), repositories (note the misspelled folder `Reopsitories/`), document generation, file storage, `DependencyInjection/` registration. |
| `OrderHub.Web` | Host: `Program.cs`, Razor components, cookie auth (`/login`), permission authorization, static assets. |
| `OrderHub.Tests` | xUnit tests (Application/Domain/Infrastructure/Web). SQLite for isolated tests; PostgreSQL tests are opt-in via env vars. Also `Web/order-local-time.test.mjs` (Node). |

### Application feature folders (CQRS)

`Features/Administration`, `Authentication`, `Documents`, `OrderDrafts`, `Orders` (Create/Update/GetPage/Lifecycle/Archive/…), `Products`, `Setup` (Cities, Suppliers, ShippingCarriers, Properties, PaymentMethods, Roles…).

Each feature is typically:

```
Features/<Area>/<UseCase>/
  ├── <UseCase>Command.cs or <UseCase>Query.cs   (MediatR IRequest)
  ├── Contracts/  (input/output records)
  └── <UseCase>Handler.cs
```

## 3. Architecture rules (follow exactly)

1. **Vertical slice CQRS**: everything goes through MediatR (`IRequestExecutor` wraps `IMediator`). Components inject `IRequestExecutor` and call `await RequestExecutor.ExecuteAsync(new SomeQuery(...))` — never call repositories from pages.
2. **Result pattern**: handlers return `Result` / `Result<T>` (`OrderHub.Domain.Common.Result`) with `IsSuccess` + `ErrorMessage` + `ErrorCode` (`"Conflict"` is used for optimistic-concurrency/version conflicts). Do not throw for expected failures.
3. **Domain entities**: private setters, private parameterless ctor + `internal` ctor, factory/behavior methods with validation (see any existing entity, e.g. `Category`, `RolePermission`, `Order.Lifecycle.cs`). DTO mapping happens in Application/Infrastructure, never expose entities directly.
4. **EF configurations** live in `OrderHub.Infrastructure/Configurations/*Configuration.cs`, often based on `ModelConfigurationBase<T>`; register/mapping via `AppDbContext`. Migrations go to `OrderHub.Infrastructure/Migrations` (timestamps descending naming like `20261030000000_Name.cs`).
5. **Authorization**: page-level `[PermissionAuthorize(PermissionFeature.X, PermissionLevel.Read|Write)]` (+ `[Authorize(Roles = "Admin")]` in Administration). Row-level checks via injected `PermissionAccess.IsAllowedAsync(...)`. `AuthorizationDeniedException` → components navigate to `/access-denied`. `PermissionFeature` enum is the **backing model for the 14 features in the Roles editor** — adding an enum value automatically appears in the roles matrix.
6. **Data flow types**: `record` contracts (e.g. `CityListItem(int Id, string Name)`). Keep contracts minimal; the UI decides presentation.
7. **History/audit**: order lifecycle owns `OrderStatusHistory` / `DeliveryStepStatusHistory` rows; workflow page renders them from `GetOrderHistoryQuery` (paged, 5/page). Legacy migration entries carry `Source == OrderHistorySource.LegacyMigration` and must keep their disclaimer text.

## 4. Build / run / test

```powershell
# from repo root
dotnet restore OrderHub.sln
dotnet build OrderHub.sln --configuration Release
dotnet test  OrderHub.sln --configuration Release
node OrderHub.Tests/Web/order-local-time.test.mjs   # only test needing Node
dotnet run --project OrderHub.Web                   # needs ConnectionStrings:DefaultConnection + ApplicationData:RootPath (user-secrets/env)
```

- PostgreSQL integration tests are **opt-in** env vars (`ORDERHUB_CATEGORY_TEST_POSTGRES`, `ORDERHUB_PROPERTY_TEST_POSTGRES`, `ORDERHUB_ORDER_SYNC_TEST_POSTGRES`, `ORDERHUB_LIFECYCLE_TEST_POSTGRES`); otherwise they skip. Release rehearsal via `ORDERHUB_REHEARSAL_SOURCE`.
- Migrations are applied with the standard `dotnet ef` workflow using the design-time factory in Infrastructure. The Web `--migrate` flag is a placeholder and does **not** apply migrations. Production migrations follow `docs/order-lifecycle-release-runbook.md`.
- Publishing: `dotnet publish OrderHub.Web/OrderHub.Web.csproj -c Release -o ./publish`. Runs as Windows service `OrderHubWeb`; Production config comes from external `%ProgramData%/OrderHub/Config/appsettings.Server.json`.

### Known dev-environment gotchas

- **VS debug sessions lock DLLs** → builds may fail with `MSB3021/MSB3027` copy errors. Stop the debug session or build a non-locked project.
- OneDrive-synced workspace: occasional transient file locks; retry.
- Documentation of history is in `CHANGELOG.md` (historical notes) and `docs/`.

## 5. Web / UI conventions

### Tailwind CSS v4 — already wired

- The pipeline is the **`TailwindBlazor` NuGet package (0.2.0)** with MSBuild properties in `OrderHub.Web.csproj`, **not** a standalone npm setup:
  - `TailwindInputFile = Styles/app.css` (contains only `@import "tailwindcss";` today)
  - `TailwindOutputFile = wwwroot/css/tailwind.css`
  - `TailwindVersion = 4.1.18`, `TailwindEnabled = true`
- It scans Razor sources automatically on build. **Add custom utility tokens/`@theme` config in `Styles/app.css`.**
- CSS order in `Components/App.razor`: MudBlazor css → `app.css` → `css/order-workflow.css` → `OrderHub.Web.styles.css` → `css/tailwind.css`. Fonts load from Google Fonts (Roboto today; the redesign plan switches to Cairo + IBM Plex Sans Arabic).
- Only one tiny JS file exists (`wwwroot/js/order-local-time.js` for browser-local time rendering). **Avoid adding JavaScript; prefer Blazor (C#)** and MudBlazor components.

### Blazor UI patterns (established)

- **Editor dialogs + editor state**: list pages open `*EditorDialog.razor` (MudDialog) driven by `*EditorState.cs` (e.g. `CityEditorState`, `RoleEditorState`, `OrderEditorState` with `State/OrderItemEditorState.cs`). Validation in the state class + `MudForm`.
- **List pages**: load once via a `GetAll*Query`, in-memory search/filter, `MudTable`/`MudDataGrid` with permission-gated actions (`_canWrite`). Server paging example: `Orders.razor.cs` + `OrdersListState`.
- Order editor is a multi-step page (`Editor/OrderEditor.razor(.cs)` + `Steps`, `Products`, `State/…`).
- **Workflow page** `/orders/{id}/workflow`: stage strip, per-step cards with allowed-status buttons, history panels (`OrderWorkflow.razor` + `Workflow/*`). Status tones are centralized in `OrderWorkflowPresentation.StatusTone(...)` returning `order-tone-*` classes styled in `wwwroot/css/order-workflow.css` (also used on Orders list rows).
- Local time is displayed through the `UserLocalTime` component (`Workflow/UserLocalTime.razor`), not `DateTime.ToString`.

### MudBlazor gotchas (v9.11)

- `MudMenu` popovers render into the global `MudPopoverProvider` → their styles must be **global CSS**, not component-scoped.
- `MudDataGrid`: `CurrentPage` setter triggers a server reload; `CurrentPageChanged` is **0-based**; `NavigateTo(Page)` takes a `Page` object.
- `MudTextField` supports live `Counter`/`MaxLength`; `MudRadioGroup` binds enums directly (used for permission levels).
- Bump `WaitAndRetry` style loading-hover races: busy flags (`_busy`) must guard every async button.

### Formatting & numbers (UI law)

- Display numeric text with **`CultureInfo.InvariantCulture`** (Latin digits 0-9 even under Arabic culture), e.g. `value.ToString("N2", CultureInfo.InvariantCulture)`.
- Layout is RTL; Tailwind logical values (`ms-*/me-*/ps-*/pe-*` or `pr-*/pl-*` explicitly) matter — prefer Tailwind utilities over bespoke CSS.

## 6. House rules for agents

1. **Do not commit unless explicitly asked.** The working tree may carry uncommitted user work — never discard, stash, or `checkout --` files you did not author.
2. Keep changes **consistent with surrounding code style** (spaces between CQRS files, verbose multiline formatting, explicit `>` tags on MudBlazor attributes in one-arg-per-line style for big components). Match the dominant style of each file.
3. Plans/tasks files for ongoing redesign work live **outside the repo** at `C:\Users\ramig\.opencode\plan\orderhub-redesign-tasks.md` (14 closed templates + task checklist). Update it when executing; do not open plan-mode changes inside the repo.
4. Code review touchpoints when extending backend: entity validation → Application handler/contracts → Infrastructure configuration + migration → GraphQL/API exposure → UI contract usage. Keep `InternalsVisibleTo OrderHub.Tests` contracts intact.
5. Secrets/credentials never in source; config via user-secrets/env/external `appsettings.Server.json`.
6. Release flow: production releases are created **only** by unused `X.Y.Z` tags on `master` (`docs/web-releases.md`). Agents must not push tags.

## 7. Quick pointers

| Need | Look at |
|---|---|
| Permission system | `Domain/Enums/Permission*`, `Web/Infrastructure/Authorization/*`, `Roles/` pages |
| Orders lifecycle | `Domain/Models/Order.Lifecycle.cs`, `Application/Features/Orders/Lifecycle`, `Web/.../Orders/Workflow/*` |
| Documents (quotations/proforma/invoices) | `Domain/Models/CommercialDocuments/*`, `Application/Features/Documents`, `Web/.../Features/Documents`, `Controllers/Files/*` |
| Pricing/VAT | `Domain/Services/Pricing/*`, `Tests/OrderPricingTests.cs` |
| EF schema | `Infrastructure/AppDbContext`, `Configurations/`, `Migrations/` |
| Auth/cookies | `Web/Controllers/Auth.cs`, `Web/Services/*Identity*` |
