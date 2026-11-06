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
- `wwwroot/fonts/Tajawal-Regular.ttf` is the **only runtime font** and must ship (its `Content Remove` line was removed from the csproj); `Cairo.ttf` remains intentionally excluded. **No runtime font fetching (no Google Fonts/CDN links) — the app runs locally/offline.**

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

- **Tailwind pipeline is wired, not npm**: csproj properties `TailwindInputFile=Styles/app.css` (tokens + three-piece Tailwind import, see the CSS-order bullet below) → `TailwindOutputFile=wwwroot/css/tailwind.css`, and `builder.UseTailwind()` in `Program.cs`. Razor sources are scanned on build; put custom tokens in `Styles/app.css` `@theme`.
- CSS link order in `Components/App.razor` matters (T-A1, revised to "Option B"): MudBlazor css → `app.css` → `css/mud-blazor-overrides.css` → `css/orderhub-buttons.css` → `css/tailwind.css` (**last**) → `OrderHub.Web.styles.css` (razor-scoped styles, wins last). `Styles/app.css` imports Tailwind in three pieces — `theme.css layer(theme)` + `preflight.css layer(base)` + `utilities.css` **unlayered**: utilities therefore beat MudBlazor's unlayered component CSS and our global defaults wherever a page adds a Tailwind class (this replaced the previous blanket `@import "tailwindcss" important;` workaround — never reintroduce `important`; it would make Tailwind trump the T-A2 override tokens). Preflight staying layered means MudBlazor's own resets still beat preflight — do not "fix" the layered imports back to a single `@import "tailwindcss"`.
- Only one JS file exists (`wwwroot/js/order-local-time.js`, covered by the Node test). **Do not add JavaScript** — Blazor/MudBlazor can express everything the UI needs.
- **App font is local Tajawal, multi-weight** — 7 static files downloaded from the official `google/fonts` upstream (`ofl/tajawal`, OFL license bundled as `wwwroot/fonts/OFL.txt`): `ExtraLight 200 · Light 300 · Regular 400 · Medium 500 · Bold 700 · ExtraBold 800 · Black 900`, each declared with its true `font-weight` in `wwwroot/app.css`. Tailwind classes `font-thin→font-black` all resolve; CSS `600` renders via the real Bold file, `100` via ExtraLight (no synthesis). **Never introduce runtime font fetching** (no CDN `link`/`@import` of fonts) — the app runs locally/offline. `Cairo.ttf` is intentionally excluded from `Content` and unused.
- **MudMenu/MudPopover content renders into the global `MudPopoverProvider`** (both providers are set up in `MainLayout`) → any styling of menus/popovers must be **global CSS**, never component-scoped isolation.
- `MainLayout.razor` and `StandaloneLayout.razor` both reference the shared programmatic theme `OrderHubTheme.Current` (`OrderHub.Web/Services/OrderHubTheme.cs`) — the only place to change the programmatic MudTheme; component-level styling lives in `mud-blazor-overrides.css`, Tailwind tokens in `Styles/app.css` `@theme`.
- `NavMenu.razor` is the single source of nav items (permission-filtered groups; Dropdown + Drawer variants).
- Status tones are centralized in `OrderWorkflowPresentation.StatusTone(...)` → Tailwind `Tag`/`Action`/`Surface`/`IconTile`/`Dot` class sets (used on Orders list, order details and workflow pages).
- **Numbers always `CultureInfo.InvariantCulture`** so digits stay Latin under Arabic culture (e.g. `value.ToString("N2", CultureInfo.InvariantCulture)`).
- Established page patterns: list pages load via `GetAll*Query` with in-memory search + `MudTable`/`MudDataGrid`, permission-gated actions; editors are `*EditorDialog.razor` (MudDialog) driven by `*EditorState.cs` validation + `MudForm`; the order editor is a multi-step page under `Editor/`.
- **Shared scaffold components (redesign)**: `Components/Common/PageScaffold.razor` (`Title` required, `Crumbs` of `CrumbItem(Label, Href?)` — no Href = current page, `Count?` badge, `Actions` fragment) and `Components/Common/TableFooter.razor` (`TotalItems` + **1-based** `@bind-CurrentPage` + `@bind-RowsPerPage` 10/25/50). Every restyled page must use both **except**: `/orders/drafts` follows the owner-supplied `drafts.html` flat header + `DraftsPager.razor` (fixed 8/page), `/orders/{id}` follows the owner-supplied `order-details.html` (separate breadcrumb/actions bar + banner card + native items table + 2:1 financial/invoice grid), and `/products/create` + `/products/{Id:int}/edit` follow the owner-supplied `create-product.html` (merged intro card with built-in breadcrumb + header save action + compact pagers). Do not "fix" either back to the shared scaffold. `TableFooter.CurrentPage` is 1-based — convert from MudDataGrid's 0-based at the call site. New Razor gotcha: `@page` must never be written as member access (`@page.X` — reserved directive).
- **Design language (latest, from the `create-product.html` pass)**: page-level list cards stay `rounded-2xl` under `PageScaffold`; inner section/dialog cards are `rounded-xl border border-slate-200/90 bg-white p-4 sm:p-5 shadow-xs` with a `pb-3 mb-3 border-b border-slate-100` header = 32px tonal icon tile (`bg-<tone>-50 text-<tone>-600 border-<tone>-100`) + `text-sm font-bold text-slate-800` title + `text-[11px] text-slate-600` helper. Tone map: blue = basic data, indigo = categories, teal = contact/suppliers, purple = attributes, amber = required chips; counters are `text-[11px] font-bold` tonal pills. Text: headings `slate-800/900`, body `slate-600`, values `slate-700/800`. Dialogs follow `CityEditorDialog` (icon header + close, `bg-slate-50/70` body with white section cards, footer readiness dot + إلغاء/حفظ) and use inline red/amber banners instead of `MudAlert`. Compact actions: `text-xs font-bold`, `rounded-lg`, brand-600 filled primary with `shadow-sm shadow-brand-500/25`.
- **Unified button system (`wwwroot/css/orderhub-buttons.css`, owner spec)**: every action uses `.oh-btn` — height 36px (`.oh-btn--sm` = 32px), radius 8px, 13px/600 text (sm 12px), inline-flex. Variants are compound (`.oh-btn.oh-btn--primary|secondary|danger|ghost`, `--icon`, `--block`, `--active`). Exactly **one primary CTA per page** (brand-600 #2563eb, white bold); all other buttons are white with `border-slate-300` + near-black (`slate-900`) semibold text; destructive keeps rose text/border; icon buttons are square (`--icon`, `--sm` = 32px) and the `--ghost` variant is only for table-row/toolbar icons where no border is wanted. **Tone buttons** (workflow statuses, DraftCard) use `oh-btn` geometry **without** a variant modifier so page-level Tailwind tone classes (`bg-sky-50`, `hover:bg-sky-100`) still win — never add `--ghost`/`--secondary` to a tone button (its compound specificity would out-rank Tailwind hover). Do not add `!important`; the file is loaded before Tailwind on purpose. Widths stay call-site driven (`--block` only for full-width). System states (`/access-denied`, `/not-found`, `/Error`) use the centered `rounded-2xl` card + one primary CTA; `#blazor-error-ui` styling lives scoped in `MainLayout.razor.css` (rose-50 bar, Tajawal).
- **Standalone auth pages (from the owner-supplied `login.html` pass)** — `/login` and `/setup/admin` deliberately do **not** use `PageScaffold`. They share one shell: full-height `bg-slate-50` with a 32px CSS grid overlay + two blurred blue ambient glows (inline `radial-gradient` styles, no JS), a 64px gradient cube logo (`bg-gradient-to-tr from-blue-700 via-brand-600 to-blue-500`) + OrderHub wordmark, a `rounded-3xl` white card with the template shadow, and a copyright-only footer. `/login` uses **native HTML inputs** (label above, start icon, password eye toggle via `_showPassword` + `InputType`-style `type` switching, `POST /auth/login` with `username/password/rememberMe`), a template-style submit (text + trailing arrow), and renders the server's existing `?error=invalid` redirect as an inline rose banner. Owner decisions: label is «اسم المستخدم» only (no email login), no forgot-password link, no top bar/status pill, no SSO/Azure section, no fake footer links, and the security note is truthful («جلستك محمية بمصادقة كوكيز آمنة») — never claim SSL/256-bit. `/setup/admin` reuses the shell with a `max-w-[34rem]` card and keeps its owner-approved WIP stub code-behind (`CreateAdmin.razor.cs.cs`); it keeps Mud fields for its validation.
- **App shell (T1)**: `MainLayout.razor` = sticky plain `<header>` (logo → `/`, `NavMenu` variants, user-card MudMenu with the POST `/auth/logout` form) + `MudMainContent` body in a `max-w-[1720px]` Tailwind container + site footer. `NavMenu.razor` exposes `NavVariant.Desktop` (dropdown row) and `NavVariant.Mobile` (hamburger flattened menu); the Drawer variant is deleted. Menu/popover styling for the shell is **global CSS in `wwwroot/app.css`** (`.site-nav`, `.site-nav-active`, `.site-nav-section-label`, `.site-user-menu`) because MudMenu popovers render into the global `MudPopoverProvider`. MudBlazor 9.11 gotchas: a custom `ActivatorContent` on **MudMenu** has NO click handler — the content must call `@context.ToggleAsync` (MenuContext); **MudTooltip has no `ActivatorContent`** — its content IS the activator; MudTable is bindable via `@bind-CurrentPage`/`@bind-RowsPerPage` (both 0-based); `DividerType` values are `FullWidth | Inset | Middle` only; `MudCheckBox` has no `LabelClass`.
- MudBlazor 9.11 gotchas: `MudDataGrid` `CurrentPage` is **0-based** and its setter triggers a server reload (`NavigateTo(Page)` takes a `Page` object); guard every async button with busy flags — hover/loading races double-fire.

## House rules for agents

1. **Do not commit unless explicitly asked.** When committing (or asked to commit), commit messages **must always start with a Conventional Commits prefix — `feat:`, `fix:`, `refactor:`, `chore:`, `test:`, `perf:`, or `docs:` — optionally with a scope**, e.g. `feat(orders): add delivery costs`. No plain/unprefixed messages. The working tree may carry uncommitted user work — never discard, stash, or `checkout --` files you did not author.
2. Match the dominant style of each file (CQRS files separated by blank lines; verbose multiline formatting; explicit `>` attribute-tag style on big MudBlazor components).
3. Ongoing redesign plan/task registry lives in the repo at `plans/orderhub-redesign-tasks.md` (plus the other `plans/*.md` specs) — update it when executing; do not open plan-mode changes inside the repo.
4. Backend extension touchpoints in order: entity validation → Application handler/contracts → Infrastructure configuration + migration → UI contract usage. Keep `InternalsVisibleTo("OrderHub.Tests")` intact.
5. Secrets never in source (see Config section).
