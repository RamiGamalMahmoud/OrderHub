# Goldfish Concepts Brief — "OrderHub Role-Derived Home Dashboards"

**Mode:** BRAINSTORM · **Date:** 2026-11-13 · **Target:** `/` route (`OrderHub.Web/Components/Pages/Home.razor`), Application/Infrastructure summary queries, Domain attribution model

## Reviewer Status

| Reviewer | Status |
| --- | --- |
| `egm-design` | not run — runtime blocked (subagent depth limit; nested spawns forbidden from child session). Analysis performed by synthesizer directly, read-only. |
| `egm-codebase-details` | not run — same runtime block; verified against code by synthesizer. |
| `egm-zoom-out` | not run — same runtime block; YAGNI lens applied by synthesizer. |
| `egm-comprehension` | not run — same runtime block; ambiguity audit applied by synthesizer. |
| `egm-implementation-readiness` | not run — same runtime block; readiness audit applied by synthesizer. |
| `egm-structural` | not run — same runtime block; structural recommendations by synthesizer. |

Every factual claim below was verified by reading the actual source files (paths cited inline). Nothing is assumed from the hypothesis text.

---

## Overview

The concept — replace the placeholder home page with permission-derived dashboards — is directionally sound and well-aligned with existing architecture (`CheckPermissionQuery`, `PermissionAccess.IsAllowedAsync`, sealed role presets, MediatR summary queries). But as specified it contains four blocking gaps: (1) the **"Super Admin" variant has no backing concept in the codebase** — `IsAdmin` is the ceiling; (2) the **"system logs" widget has no data source** — the Administration area contains only Users and Roles pages, no log store exists; (3) the three load-bearing semantic terms — *orders needing attention*, *delayed*, *stock shortfall* — have **no existing definition anywhere in the Application layer**, so v1 would be inventing product semantics under a UI feature; and (4) the **Sales dashboard's claim that a new `Order.CreatorUserId` column is required is overstated** — creation attribution already exists in `OrderStatusHistory.ChangedByUserId` (initial history row stamped by `CreateOrderCommandHandler`), and the hypothesis's "no named job roles exist" premise is contradicted by `SealedRolePresets` (مبيعات / مشتريات / إدخال البيانات / مدير / أمين مخزن), which already includes a purchasing preset the concept declares out of scope.

## Analysis

### Verified facts (codebase)

| # | Fact | Evidence |
| --- | --- | --- |
| F1 | `Home.razor` (`/`) is `[Authorize]`, uses `PageScaffold` + `PermissionView`, renders hardcoded stats (35/56/18) with `@* placeholder: dashboard totals are not backed by summary queries yet *@` comments, plus an `Illustrative` `MudChart` (7-day bars) and a static "متابعة الطلبات" card. | `OrderHub.Web/Components/Pages/Home.razor:43,91,127-158` |
| F2 | `CheckPermissionQuery(Feature, RequiredLevel) : IRequest<bool>` already exists as a MediatR permission check — a ready-made dispatch primitive. | `OrderHub.Application/Authorization/CheckPermissionQuery.cs:9-15` |
| F3 | `AuthorizationService.IsAllowedAsync`: admin full bypass; `Read` satisfied by Read/Write/Approve, `Write` by Write/Approve, `Approve` by Approve only. | `OrderHub.Application/Authorization/AuthorizationService.cs:11-31` |
| F4 | **Sealed role presets already exist** with Arabic display names: `sales` (مبيعات), `purchasing` (مشتريات), `data-entry` (إدخال البيانات), `manager` (مدير), `storekeeper` (أمين مخزن) — each a concrete permission matrix. | `OrderHub.Domain/Authorization/SealedRolePresets.cs:19-96` |
| F5 | **"Super Admin" does not exist.** The ceiling is `User.IsAdmin` (bool column + "Admin" role). `IsHigherManagerAsync` returns exactly `currentUser.IsAdmin`. | `AuthorizationService.cs:33-37`; `OrderHub.Domain/Models/User.cs`; `OrderHub.Web/Services/AuthenticatedUser.cs:35` |
| F6 | `Order` has **no** creator column. Attribution exists only via `OrderStatusHistory.ChangedByUserId` (nullable int), stamped at creation: `order.InitializeHistory(new OrderOperation(_currentUser.Id.Value, _currentUser.DisplayName ?? _currentUser.UserName, order.CreatedAt))`. Legacy migration rows carry `Source == OrderHistorySource.LegacyMigration` and may have `ChangedByUserId == null` → a natural "unattributed" bucket. | `OrderHub.Domain/Models/Order.cs:23-29`; `OrderStatusHistory.cs:10-34`; `CreateOrderCommandHandler.cs:192-196` |
| F7 | `ICurrentUser` (`Id, UserName, DisplayName, IsAuthenticated, IsAdmin`) is already injected into Application handlers, including `CreateOrderCommandHandler`. | `OrderHub.Application/Context/ICurrentUser.cs`; `CreateOrderCommandHandler.cs:28,69` |
| F8 | `StockIssueOrder`: `RequestedByUserId`, `AssignedUserId` (int, **not nullable**, snapshot of `warehouse.KeeperUserId` *at creation* — the code comment says execution follows the *current* keeper binding), `Status` ∈ `WorkRecordStatus` (Pending / PartiallyExecuted / CancelRequested / Completed / Cancelled), `RequestedTotal`/`IssuedTotal`, monthly per-warehouse numbering. **No stock-issue summary query exists in Application** (grep for `GetStockIssue*` returned nothing). | `OrderHub.Domain/Models/StockIssueOrder.cs:25-39,80-81,107-113`; `Warehouse.cs:27` |
| F9 | Administration pages = Users (Index/Create/Edit) + Roles (Index/Editor). **No logs page, no settings page, no log store.** `PermissionFeature.Logs` and `PermissionFeature.Settings` exist in the enum but are unused by any page. | glob `Features/Administration/**/*.razor`; `PermissionFeature.cs:56-64` |
| F10 | **No "delayed/overdue/attention" semantics exist anywhere in Application** (grep `متأخر|Overdue|IsLate|Delayed` → zero hits). `OrderListItem` carries no delivery-date field; `GetOrdersPageQuery` filters by date range / status / payment but computes nothing. | `GetOrdersPageQuery.cs:10-19`; `OrderListItem.cs:9-22` |
| F11 | A self-scoped summary precedent exists: `GetDeliverymanTripsSummaryQuery(DeliverymanId) : IRequest<DeliverymanTripsSummary>` — the pattern a warehouse/sales self-scoped dashboard should copy. | `Features/Trips/GetDeliverymanTripsSummary/*` |
| F12 | Migrations follow descending timestamps (`20261113001132_AddStockIssueOrders.cs` is latest); recent columns like `CreatedByUserId` on `InventoryMovement`/`StockReceipt` already survive snake_case + SQLite FK (`HasIndex("CreatedByUserId")`, FK to Users). | `Migrations/20261113001132_AddStockIssueOrders.Designer.cs:2335,3447` |

### Findings

#### 🔴 Blocker 1 — "Super Admin" dashboard has no backing concept
* **Lens:** design / zoom-out
* The system's top privilege is `IsAdmin` (F5). There is no higher tier, no "Super Admin" role, no separate signal to dispatch variant 2 vs variant 1. Building "two distinct dashboards" would require inventing a privilege concept the codebase does not have — a product decision the hypothesis has not made. The alternative that matches the existing model: **one admin dashboard whose admin widgets (Users count/setup health) are individually gated by `PermissionFeature.Users/Write`**, exactly like NavMenu gates its items. A user with `Users/Write` sees the widgets; one without sees only the operational base.
* **Recommendation:** collapse variants 1 and 2 into one Admin dashboard with permission-gated admin sections. If the owner truly wants a visually separate "Super Admin" home, that is a naming + dispatch-predicate product decision requiring an explicit answer (what permission predicate defines it — `Users/Write`? `Settings/Write`?).

#### 🔴 Blocker 2 — "System logs" widget is unbacked
* **Lens:** codebase-details / comprehension
* There is no log store and no Logs page (F9). The hard repo rule is *zero fake data*. A logs widget would either be fabricated or require building a whole audit-log subsystem — far beyond dashboard scope.
* **Recommendation:** cut "system logs" from v1. If system health is wanted, restrict it to what is real (e.g., Users count from `GetUsersQuery`, `/health` endpoint exists for the service probe — but rendering `/health` on the home page adds nothing for an admin already in the app). "Setup/system health" likewise has no summary query; cut or define against real data.

#### 🔴 Blocker 3 — "Attention", "delayed", "stock shortfall" are undefined terms
* **Lens:** comprehension / implementation-readiness
* No existing definition exists (F10). Each term is a product-semantics decision: delayed by *what field* (order-level `CreatedAt`? `OrderDeliveryStep` planned dates — do planned dates even exist as a queryable column?)? Shortfall against *what* (allocated-but-unfulfilled quantities from `OrderInventoryAllocation` vs `StockBalance`? A reorder point — none exists in the domain)? Attention by *which* status transition?
* **Recommendation:** the concept cannot be planned until the owner answers, per term: the data source, the comparison rule, and the Arabic label. Without answers, implementers would silently invent semantics — a direct violation of the "no unbacked behavior" spirit of house rule 7.

#### 🔴 Blocker 4 — The `CreatorUserId` premise is overstated; a zero-migration alternative already exists
* **Lens:** codebase-details / zoom-out
* The hypothesis asserts Sales *requires* a new nullable `Order.CreatorUserId`. In fact creation attribution already persists in `OrderStatusHistory` (F6): the initial history row of every order created through the app carries `ChangedByUserId = creator`, and legacy/unattributed orders correspond to history rows with `ChangedByUserId == null` — the "unattributed bucket" falls out for free. Caveats of the history-based approach: it needs a careful query (`Source != LegacyMigration` + initial row semantics, i.e., first row per order), and filtering/paging by creator through history is less clean than a column with an index.
* **Recommendation:** this is a genuine trade-off decision, not a settled fact:
  * **Option A (no migration):** query `OrderStatusHistory` initial rows. Zero schema change, works today, legacy unattributed = `null` user. Cost: heavier query, no covering index, semantics coupled to history row structure.
  * **Option B (new column):** nullable `Order.CreatorUserId` + FK + index; `CreateOrderCommandHandler` stamps `_currentUser.Id.Value` (mechanism already present, F7). Additive, survives SQLite/snake_case (proven by F12), legacy rows stay null. Cost: migration on populated DB + stamping the create command is a data-shape behavior change needing owner sign-off under house rule 7.
  * Do **not** present B as the only path. If B is chosen, keep `ChangedByUserName` snapshot usage unchanged and note that drafts-converted orders attribute to whoever executed the save command.

#### 🟡 Major 1 — Dispatch mechanism is underspecified
* **Lens:** design / structural
* The concept says "selected by permissions" but never states: where the selection runs, what the predicate per variant is, or the fallback. Missing decisions:
  * **Location:** in-page composition vs. redirect to per-variant routes. Deep-linking and mid-session permission changes favor in-page composition on the single `/` route (the repo has 28 established routes; a family of `/dashboard/*` routes adds navigation surface and PageScaffold breadcrumb questions for no gain).
  * **Predicates:** a variant cannot be "the sales user" — it must be a permission test. E.g., Sales variant = `Orders/Read` **and** self-scoping applies only when the user lacks broader signals; Warehouse variant = `Warehouses/Read` (+ the user is a keeper of ≥1 warehouse via `Warehouse.KeeperUserId`). A user matching several predicates (a manager with Orders/Approve who also keeps a warehouse) needs a **priority order**, and a user matching none needs a **fallback**.
  * **Fallback:** mandatory. Proposal: a minimal generic dashboard — real counts the user is permitted to see (orders/products/clients totals from real summary queries) + the existing `PermissionView` action buttons. A user with no relevant permissions must still land on a non-empty, non-error page.
* **Recommendation:** a `DashboardResolver` (Web service) or a plain `OnInitializedAsync` permission pass on Home.razor using `CheckPermissionQuery`/`PermissionAccess`, selecting an ordered list of section components; sections themselves are individually `PermissionView`-gated so odd mixes degrade gracefully (a sales user who also has `Inventory/Read` simply sees both sections).

#### 🟡 Major 2 — The "no named roles" premise conflicts with `SealedRolePresets`
* **Lens:** codebase-details
* The hypothesis states "the system has no named job roles for sales/purchasing/warehouse". Sealed presets for exactly those names exist (F4) — they are permission-matrix templates rather than assignment-level roles, but they are named, they carry Arabic labels, and they define what "a sales user" and "a storekeeper" mean in this system. Any dashboard taxonomy must reconcile with them: the sales preset holds `Orders/Write`, the storekeeper preset holds `Warehouses/Read + Inventory/Write`, `data-entry` holds only catalog/reference-data writes (→ **no dashboard-relevant data; lands on fallback**), and `purchasing` holds `Orders/Read + Suppliers/Write`.
* **Recommendation:** declare the variant taxonomy *in terms of the presets* (each preset → which dashboard/sections it yields), and either include a purchasing view or state explicitly why the existing purchasing preset is excluded while sales is not — otherwise the exclusion looks arbitrary. Note `data-entry` explicitly as the fallback case.

#### 🟡 Major 3 — Warehouse dashboard must key on current keeper binding, not `AssignedUserId`
* **Lens:** codebase-details / design
* `StockIssueOrder.AssignedUserId` is a **creation-time snapshot for audit only** — the code's own comment says execution follows the current warehouse-keeper binding (`StockIssueOrder.cs:80-81`). A keeper who is reassigned to another warehouse must see his *current* warehouses' pending issues, so the query must join `Warehouse.KeeperUserId = currentUser` → active `StockIssueOrder` rows for those warehouses. Using `AssignedUserId` alone silently shows stale assignments after reassignment.
* **Recommendation:** scope by `Warehouse.KeeperUserId` (current), optionally display `AssignedUserId` snapshot in row detail for audit. Add the "pending" definition explicitly: `Status ∈ {Pending, PartiallyExecuted}` (i.e., `AcceptsBatches`), excluding `CancelRequested`? — owner must decide whether cancel-requested items appear as "needs attention" (they probably should).

#### 🟡 Major 4 — Query performance design is undecided
* **Lens:** design / readiness
* Every page load re-aggregates. Today no dashboard-grade summary query exists (F10, F8); the reusable patterns are `GetOrdersPageQuery` filters (F10) and the `DeliverymanTripsSummary` shape (F11). Blazor Server handlers run sequentially per section unless composed.
* **Recommendation:** one dedicated summary query per section (e.g., `GetAdminOrdersSummaryQuery`, `GetWarehouseKeeperSummaryQuery`, each a single round trip with multiple aggregates via conditional SQL counts), matching the `Features/<Area>/<UseCase>` layout; restrict lists to 5–10 rows with existing pager discipline; index only what the queries filter on (`CreatedAt`, `OrderStatus`; `CreatorUserId` if Option B; `WarehouseId+Status` for stock issues). **Do not build caching in v1** — YAGNI until a real load problem exists; at single-server scale with counts-only SQL this is a non-issue. All counts through `CultureInfo.InvariantCulture`.

#### 🟡 Major 5 — House rule 7 exposure: replacing `/` is a behavior change to an existing screen
* **Lens:** implementation-readiness
* Every existing user's landing experience changes. Rule 7 requires the plan to list the change, its impact, and rollback **before implementation**. Also inert-until-enabled applies: the new behavior cannot regress anyone — the fallback dashboard for permission-poor users must be at least as informative as today's placeholder-free minimal state, and the removal of the illustrative chart (F1) is itself part of the change to disclose.
* **Recommendation:** the approval checklist should name: (a) the old page disappears for all users; (b) the chart is removed in v1 (scope says no charts); (c) Option B migration stamps future orders only — legacy rows untouched; (d) rollback = revert to current Home.razor (no data rollback needed if Option A).

#### 🟡 Major 6 — "Recent activity" is definable but must be pinned to real sources
* **Lens:** comprehension / zoom-out
* Real sources exist: `OrderStatusHistory` and `DeliveryStepStatusHistory` (owned by the lifecycle; already rendered on the workflow page via `GetOrderHistoryQuery`, paged 5/page). A dashboard "recent activity" list can reuse exactly those, filtered to the last N events the user may see — but the concept must state: which entity's history, which window (today? last 10 events?), and whose visibility (row-level: an admin sees all; a sales user sees events on his own orders only).
* **Recommendation:** v1 = last 5–10 order-status events visible to the user, reusing the existing history query/contracts. Do not invent a new activity feed abstraction.

#### 🟢 Nit 1 — "Team totals" has no team concept
* No team entity or grouping exists. Either cut the phrase or replace with "unattributed + mine" totals. Not resolvable by implementers.

#### 🟢 Nit 2 — Arabic labels and structural conventions must be specified now
* Variant/section titles are unstated. Proposed: `لوحة التحكم` (page title stays), sections `طلبات اليوم`, `بحاجة إلى متابعة`, `أوامر الصرف`, `أرصدة مخازني`, `نشاط حديث`, `المستخدمون`. Sections use the established inner-card pattern (`rounded-xl border border-slate-200/90 bg-white p-4 …` + 32px tonal icon tile), `PageScaffold` with a single crumb, exactly one primary CTA (the existing `إضافة طلب` for users with `Orders/Write`; warehouse keeper's primary CTA becomes his top pending-issue action), logical properties, 12px text floor, `oh-btn` geometry for tone buttons without variant modifiers.

#### 🟢 Nit 3 — Existing tests and conventions to preserve
* `AuthorizationServiceTests` and all SQLite tests must stay green; any new query needs a SQLite in-memory test with the same options; `InternalsVisibleTo("OrderHub.Tests")` stays. Dispatch logic should be testable without a DB (pure permission-predicate function) — keep it out of the razor component.

---

## Goals vs. Non-Goals

### Goals (v1)
1. `/` shows **real** job-relevant data, derived from permissions, with a defined fallback for every user — no placeholder numbers, no illustrative chart.
2. **Additive composition**: each dashboard section is an independent, permission-gated component backed by one dedicated MediatR summary query; adding a section or variant never rewrites the page.
3. **Defined dispatch**: ordered permission predicates (checked via `CheckPermissionQuery`/`PermissionAccess`) + fallback; reconciliation with `SealedRolePresets`.
4. Attribution decision made explicitly (history-based vs. new column), with the unattributed bucket defined.
5. RTL/Arabic correctness under `PageScaffold`/`oh-btn`/one-primary-CTA; no new JavaScript; InvariantCulture numbers.

### Non-Goals (v1)
- Charts (remove the illustrative `MudChart`).
- Caching or performance infrastructure beyond sane SQL/indexes.
- System-logs / setup-health widgets (no data source — cut).
- Purchasing dashboard (existing preset acknowledged; exclusion must be justified or reversed).
- Team concepts ("team totals"), notification systems, saved dashboard layouts, per-user customization.
- Any non-additive migration (drops/renames/alterations).

---

## Alternatives Weighed

| # | Alternative | Verdict |
| --- | --- | --- |
| A1 | **Four distinct dashboard pages** (`/dashboard/admin`, `/dashboard/sales`, …) with redirect dispatch from `/` | Rejected for v1: more routes, breadcrumb/deep-link maintenance, redirect churn when permissions change mid-session; no compensating benefit since composition gives the same modularity. |
| A2 | **Single `/` route + composition of permission-gated section components + ordered predicate resolver (in-page)** | **Recommended.** Additive, one route, graceful degradation for odd mixes, fallback trivially renderable, matches existing `PermissionView` patterns and NavMenu philosophy. |
| A3 | **Sales attribution via `OrderStatusHistory` initial rows (no migration)** | Viable today; zero schema risk; heavier query, coupled to history semantics. Strong candidate if the owner resists the migration. |
| A4 | **Sales attribution via new nullable `Order.CreatorUserId` + FK + index** | Cleanest for filter/paginate-by-creator; additive and SQLite-proven pattern (F12); but stamps the create command → needs house-rule-7 sign-off. Present as Option B, not as a requirement. |
| A5 | **One universal dashboard, all sections permission-gated (no variants at all)** | Simplest possible design; degrade naturally. Risk: page grows long for admins. Reasonable middle: sections composed per user, with only the *headline layout* differing by predicate. Keep as the fallback of last resort if the owner wants minimal build. |
| A6 | **Keep static home; rely on NavMenu** ("one click from work screen" is already served by permission-filtered nav) | Rejected by the concept's premise, but record it: the dashboard's unique value over NavMenu is *aggregates that answer "what needs me today?"* — so v1 should prioritize the "needs attention"/"my pending" sections over generic counts, or it adds little beyond the nav. |

---

## Key Decisions & Staging for Next Steps

### Decisions resolved (owner, 2026-11-13)
1. **Admin/Super Admin:** COLLAPSED — one admin dashboard; admin widgets individually permission-gated (no "Super Admin" tier exists; `IsAdmin` is the ceiling).
2. **Attribution:** Option B — new nullable `Order.CreatorUserId` + FK + index; `CreateOrderCommandHandler` stamps it. Legacy rows stay unattributed.
3. **Semantics:** *needs attention* = non-terminal statuses (Processing / OutForDelivery / ReadyForPickup / DeliveryFailed) + orders with no status change for X days (default 3 for Processing, 7 for out-for-delivery states — tunable constants) + stock-issue orders with `CancelRequested`; *stock shortfall* = `OrderInventoryAllocation` vs current `StockBalance`. "Delayed" has no delivery-date basis and is folded into the stall rule.
4. **Dispatch:** in-page composition on `/` — IsAdmin → admin dashboard; else current `Warehouse.KeeperUserId` of ≥1 warehouse → warehouse sections; else `Orders/Read` → sales sections scoped to `CreatorUserId = current user`; else fallback generic dashboard (real permitted counts + existing action buttons). Sections individually `PermissionView`-gated.
5. **Purchasing:** stays out of v1 — documented deliberate gap (preset users land on the fallback until a dedicated view ships with a purchase-order feature).
6. **House rule 7:** owner approved the full change set — `/` replaced for all users, placeholder chart/numbers removed, create-command stamping approved (future orders only), rollback = revert to current `Home.razor`; additive column needs no data rollback.

### Proposed staging (dependency-ordered)
1. **Phase 0 — Definition lock.** Owner answers 1–6; write the widget contract table (variant × section × query × permission × fallback) and the Arabic label list. No code.
2. **Phase 1 — Query layer.** Add per-section summary queries in `Features/<Area>/<UseCase>` with SQLite tests (snake_case-safe), Result-pattern returns, InvariantCulture formatting in contracts. No UI change yet — inert.
3. **Phase 2 — Admin/operational overview.** Replace placeholder stats + chart with the operational sections (today's orders by status, attention list per agreed semantics, recent activity from existing history). This is the behavior change approved in decision 6; old flows verified in browser after.
4. **Phase 3 — Warehouse keeper section.** Scope by current `Warehouse.KeeperUserId`; statuses `Pending`/`PartiallyExecuted` (plus `CancelRequested` if owner agrees it is "attention"); balances from existing `GetWarehouseStockQuery` data; reuse `DeliverymanTripsSummary` shape for the summary contract.
5. **Phase 4 — Sales section.** Per attribution decision (A3 or A4). If A4: additive migration (nullable column + FK + index, descending timestamp), stamp `CreateOrderCommandHandler` via `ICurrentUser` (mechanism exists, F7), SQLite FK tests, legacy rows unattributed.
6. **Phase 5 — Fallback + polish.** Generic fallback dashboard, empty states, mobile folding (`lg:hidden` patterns), final RTL/typography audit, existing test suite green, populated-DB migration rehearsal (if A4) with rollback rehearsed.

**Explicitly do not build yet:** caching, charts, logs widget, team totals, purchasing view (pending decision 5), dashboard settings/customization, any `/dashboard/*` routes.
