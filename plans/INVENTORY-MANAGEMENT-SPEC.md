# Inventory (Stock) Feature — Study & Specification

- **Version:** 1.3 (permission integration locked 2026-11-09; roles prerequisite complete)
- **Status:** Approved study; **no inventory production code written yet.** The roles prerequisite (R0) is implemented. Implementation starts with slice A1 (Warehouses) after final approval.
- **Date:** 2026-11-09 (supersedes v1.2, 2026-11-08)
- **Repository:** OrderHub
- **Style:** follows `plans/TRIPS-MANAGEMENT-SPEC.md`. All user-facing UI strings are Arabic; identifiers below are code names.
- **v1.3 changes:** §3 and §7 rewritten against the implemented roles redesign (`Approve`, sealed presets, Higher Manager gates); D9 and D20 amended; D22–D29 added; preset seed extended via a frozen-V1 + new-V2 migration; keeper-binding rules added; build order updates R0 as done.

---

## 1. Purpose

Add product stock (مخزون المنتجات) to OrderHub:

1. Every order item declares its fulfillment source: **stock** when the selected warehouse can supply (part or all of) the quantity, otherwise (or for the surplus) a **supplier linked to the product**.
2. Stock balances derive exclusively from explicit movements (purchases/stock-in, issue-order execution, customer returns, reversals). A cached column is display-only; there is **no manual editing of stock numbers**.
3. Stock-sourced items generate **Stock Issue Orders** (أوامر إخراج من المخزن) per warehouse, and the warehouse's keeper is **notified in-app** (visible to him only) and can be messaged on **WhatsApp** (copy / send link, `OrderSummaryDialog` pattern).
4. The keeper records execution in-app. The operation links both **who requested it** (order creator) and **who executed it** (the keeper).
5. Nothing is added or removed except through an explicit operation with recorded data.
6. Customer **returns** are recorded with an intact/damaged state (only intact units rejoin stock).
7. A **reversal** workflow fixes mistakes (reversal entries, never deletion), initiated by the keeper and **approved by a user holding `Inventory = Approve`** (Manager or Higher Manager).
8. A **cancel** with a mandatory reason closes an issue order before execution.
9. **Multiple warehouses**, each with exactly one keeper user; balances are per warehouse per product, with an aggregated totals view.

---

## 2. Domain background — standard inventory practice observed

1. **The stock ledger is the source of truth.** Every balance change passes through an explicit `InventoryMovement` row carrying its source-document reference, actor, and time. Balance = sum of active movements.
2. **No silent operations.** A sale deducts because an issue order was executed; inbound increases because a stock-in was recorded; a return increases because a return was registered. Nothing changes implicitly behind ordinary edits.
3. **Cached balances are derived, never authoritative** — updated transactionally with each movement, rebuildable from the ledger.
4. **Reversals, not deletion** — the accounting reversal-entry pattern: the mistaken movement stays flagged; a paired opposite movement references it.
5. **Cancel ≠ reverse** — cancellation applies only to what has not drawn stock; executed operations go through reversal.
6. **Explicit approval for sensitive operations** — here: reversals require `Inventory = Approve`.
7. **Returns** are distinct operations referencing the original document, with quantities ≤ what was issued.
8. **Audit trail** — every movement carries its creator, timestamps, and a mandatory reason for cancels/reversals. Historic rows are never deleted or edited.

---

## 3. Current codebase touchpoints

| Component | State | Relation |
|---|---|---|
| `OrderItem.SupplierId` / `SupplierName` | Optional; no stock-vs-supplier concept | Gains stock source fields (§5.9) |
| `Product.Suppliers` | Exists (`AddSupplier`, `HasSupplier`, …) | "Supplier linked to the product" ready |
| `Supply` | Order-bound, full `WorkRecordStatus` lifecycle, **no item quantities** | Untouched: purchases are a new independent operation (D2) |
| `PermissionFeature` | 14 features; no warehouses/inventory entries | Adds `Warehouses` + `Inventory`, Arabic descriptions, catalog entries, nav group, preset V2 |
| `PermissionLevel` / `AuthorizationService` | **Implemented:** `None < Read < Write < Approve`; `Read` ← R/W/A, `Write` ← W/A, `Approve` ← A | Reversal approval uses `Approve`; keeper binding is still required (D20) |
| `Role` / sealed presets | **Implemented:** `IsSealed`, `RoleKey`, 4 presets / 31 cells; `SealedRoleSeedSql.V1` is **frozen** | V2 migration adds inventory cells + the `storekeeper` preset |
| Higher Manager | **Implemented:** `HigherManagerAuthorizeAttribute`, `IsHigherManagerAsync` (`IsAdmin` bypass) | Rebuild tool; legal fallback for execution/cancel |
| Users directory | `GetUsersQuery` requires `Users.Read`; Manager has `Users = None` | New minimal candidate query gated by `Warehouses.Write` (D23) |
| Notifications | None exist | New page-agnostic, user-targeted `Notification` entity + header bell |
| `User` | No `Phone` field | Nullable `Phone` (D19); edited on the Higher-Manager-only Users page (D28) |
| WhatsApp | `OrderSummaryDialog` + `WhatsAppMessaging.TryNormalize` | Reused for the keeper message |
| Numbering | `NumberSequenceKind` monthly per-party sequences | New warehouse numbering kind (D8) |
| Order drafts (`OrderDrafts`) | Draft-flow exists | Issue orders + notifications occur on final save, aligned with the drafts flow |

**Binding architecture notes:** MediatR (`RequestExecutor`) + `Result/Result<T>`; Domain entities private-setter/private-ctor/internal-ctor/factory validation; new EF columns must survive snake_case + SQLite tests; **feature-level** authz via `[RequirePermission]` / `PermissionAccess.IsAllowedAsync`; **warehouse-binding is a handler-level data check** returning `Result.Failure` (never a DB constraint, never only a UI check). `PermissionFeature` is stored as text, so inserting enum members before the Administration block does not migrate data.

---

## 4. Locked decisions (owner)

| # | Decision |
|---|----------|
| D1 | Stock is **not** enabled per-product by a flag: an item takes a stock source when the product has balance in a warehouse; otherwise — or for the surplus beyond the balance — it goes to a supplier linked to the product. |
| D2 | Purchases/stock-in are a **standalone stock-in operation**, independent of the existing order-bound `Supply` entity, which stays unchanged. |
| D3 | The per-product "stock value" column is a **units count** balance (رصيد), not money. No cost valuation in this feature. |
| D4 | **Multiple warehouses**: a full `Warehouses` feature (create/edit/deactivate, keeper assignment). Stock balance is tracked **per (product, warehouse)** with an aggregated totals view. |
| D5 | Exactly **one keeper user per warehouse**; notifications target the keeper's user directly. |
| D6 | When adding an item within stock quantity rules, the order creator **explicitly selects the warehouse**; the item reserves the available balance there; the surplus beyond the balance stays an open remainder directed to a linked supplier (one item, flagged split: stock portion + supplier remainder). |
| D7 | Once an issue order exists for an item, the item is **locked against edits**; changes require cancelling the (not-yet-executed) issue order with a mandatory reason first. |
| D8 | Issue orders get **monthly numbering** per warehouse following the existing per-party sequence pattern. |
| D9 | **Reversal approval (amended v1.3):** the keeper (or a Higher Manager) initiates a reversal with a mandatory reason; it takes effect only after approval by a user whose role has **`Inventory = Approve`** (Manager or Higher Manager). **The approver must not be the requester.** The mandatory reason is recorded. |
| D10 | Cancel reason and reversal reason are **mandatory**. |
| D11 | Returns carry an **intact/damaged** state: intact units rejoin stock; damaged units are recorded but do not increase the balance. |
| D12 | Movement granularity is **product-level only** (no property/SKU-level stock). |
| D13 | Notifications surface as a **bell in the header** (counter + panel) following the `new-nav.html` patterns; accessible only to the recipient. |
| D14 | Implementation runs in **two phases** (§12): A) warehouses + stock-in + item sourcing + issue orders + notifications + WhatsApp; B) returns + reversal approval + totals/reporting refinements. |
| D15 | Execution must not exceed the live warehouse balance at execution time (`"Conflict"`-style `Result.Failure` on race). Requests beyond it were already resolved at item level by D6. |
| D16 | `Supply`, delivery, documents, pricing, and the shared work-record pipeline are not modified. |
| D17 | New rules apply only from new saves forward. Historic orders are never backfilled or touched. When an old, still-open order is edited and re-saved, **every item must gain a source** (stock/supplier) before the save completes — enforced in code only, never via DB constraints, so legacy rows stay loadable. |
| D18 | When an order is **cancelled or archived** while its issue order is still `Pending`, the system **auto-cancels** the issue order, releases the reservation, and records a system-generated `Cancelled` history entry with a system reason. No manual keeper action required. |
| D19 | The `User` entity gains an **optional, nullable `Phone`** (user profile) used for the WhatsApp-to-keeper action; when absent or un-normalizable, the always-available copy action and recipient-picker remain the fallback. |
| D20 | **Execution/cancel authority (amended v1.3):** only the **current** warehouse keeper (`Warehouse.KeeperUserId`) or a **Higher Manager** (`IsAdmin` bypass) may execute or cancel an issue order, enforced inside the handler. **`Inventory = Approve` does not grant execution.** `StockIssueOrder.AssignedUserId` is request-time audit only; authority follows the current keeper. Deactivated keepers are resolved by a Higher Manager, fully audited. |
| D21 | Opening stock enters **only** through an explicit stock-in operation (ordinary stock-in or a flagged initial-opening entry) recorded as visible ledger movements with actor/time. **No manual balance editing exists at any point** — the same rule governs opening stock. |
| D22 | **`storekeeper` preset (locked 2026-11-09):** a 5th sealed role «أمين مخزن» is seeded with `Warehouses=Read`, `Inventory=Write`, `Products=Read`, `Orders=Read`, all else `None`. Its name is reserved; it is changeable only by a future owner-approved migration. |
| D23 | **Manager inventory authority (locked 2026-11-09):** Manager gets `Warehouses=Write` and `Inventory=Approve`; keeper assignment uses a minimal `Warehouses.Write`-gated candidate query, not `Users.Read`. |
| D24 | **Returns actor (default):** customer returns are recorded by the **current warehouse keeper or a Higher Manager** (`Inventory.Write` + binding). Sales cannot post returns. |
| D25 | **Order-editor fallback (default):** a user with `Orders.Write` but no `Inventory.Read` can save supplier-sourced items only — availability is hidden, stock sourcing is disabled with an Arabic hint, and the handler rejects a stock portion. |
| D26 | **Stock-in prerequisite (default):** the stock-in product picker requires `Products.Read`; the `storekeeper` preset includes it. `Inventory.Write` alone does not expose the catalog. |
| D27 | **Routing (default):** all inventory screens live under `/warehouses` and `/inventory`; reversal approval is `/inventory/reversals`, never an `[Authorize(Roles = "Admin")]` page. |
| D28 | **Phone ownership (default):** `User.Phone` is edited on the Higher-Manager-only Users page; the WhatsApp action always offers copy and the recipient picker when it is missing or invalid. |
| D29 | **Read scope (default):** `Inventory.Read` is company-wide so sales can see availability while sourcing; every mutation remains keeper-bound. Balances are not per-user scoped. |

---

## 5. Entities (proposed)

### 5.1 `Warehouse` — المخزن (phase A)

- `Name` (EntityName), `KeeperUserId` (one user), activity flag for soft deactivation (not delete while stock or orders reference it — deletion guards, like trips spec).
- EF configuration in `Configurations/WarehouseConfiguration.cs`; migration with descending-timestamp naming.
- Keeper assignment validates that the user exists and is active. If the selected keeper's role lacks `Inventory.Write` (and is not a Higher Manager), the editor shows an amber warning; saving is allowed and the issue-order page surfaces the access gap.

### 5.2 `InventoryMovement` — the ledger (source of truth)

| Field | Notes |
|---|---|
| `Id` | PK |
| `WarehouseId` | per-warehouse ledger |
| `Direction` | `In = 1 / Out = 2` |
| `MovementType` | `StockIn` · `IssueOut` (issue-order execution) · `CustomerReturnIn` · `Reversal` |
| `ProductId` | product-level only (D12) |
| `Quantity` | ≥ 1 |
| `SourcePurchaseId` / `SourceOrderIssueId` / `SourceReturnId` | real-document references |
| `ReversalOfMovementId` | only with `MovementType = Reversal` |
| `Status` | `Active / Reversed` (never deleted/edited) |
| `Note` | mandatory on reversal |
| `CreatedByUserId` · `CreatedAtUtc` · `Version` | audit + optimistic concurrency |

### 5.3 `StockItem` — per (Product × Warehouse) cached balance (display)

- `WarehouseId` · `ProductId` · `StockOnHand` (units, D3) · `ReservedOnHand` (reserved by pending issue orders, D6) · `Version`.
- Updated transactionally with each movement; admin-only rebuild-from-ledger tool (phase B). Never edited manually (D21).

### 5.4 `StockPurchase` — standalone stock-in (phase A)

- Header: warehouse, optional supplier, optional note, user/timestamp/`Version`; item lines: product + quantity; generates one `StockIn` movement per line. Has a minimal, immutable lifecycle (no cancel-after-record; mistakes via reversal in phase B).

### 5.5 `StockIssueOrder` — أمر الإخراج (phase A)

| Field | Notes |
|---|---|
| `OrderId` · `WarehouseId` | one order per (Order × Warehouse); aggregates that warehouse's stock items |
| `Status` | `Pending → Completed / Cancelled` (existing `WorkRecordStatus` values) |
| Number | monthly per warehouse (D8) |
| `RequestedByUserId` · `RequestedAtUtc` | order creator of the stock-sourced item |
| `AssignedUserId` | **request-time snapshot** of the keeper for audit/notification; not the execution authority (D20) |
| `ExecutedByUserId` · `ExecutedAtUtc` | executor and time |
| `CancelledByUserId` · `CancelReason` | mandatory reason (D10); system auto-cancel (D18) records a system actor/reason |
| Item lines | `ProductId` · `ProductName` · `RequestedQty` (stock portion) · `ExecutedQty` |

- Created automatically on final order save containing stock items — never manually.
- Execution validates **`IsAdmin || warehouse.KeeperUserId == currentUser.Id`** against the current warehouse keeper, records executed quantities (full qty in phase 1), generates `IssueOut` movements, updates `StockItem`, and returns a notification to the order creator.

### 5.6 `InventoryReversal` — reversal with approved authority (phase B)

- Request by the current keeper or a Higher Manager with a mandatory reason + target movement (`Inventory.Write` + binding).
- Status `PendingApproval → Approved / Rejected`; records `RequestedByUserId`, `DecidedByUserId`, `DecidedAtUtc`.
- Approval requires **`Inventory = Approve`** and **`DecidedByUserId != RequestedByUserId`**.
- On approval: single transaction generates the paired `Reversal` movement, flags the original `Reversed`, and re-derives the affected `StockItem`/issue order. Never deletes.

### 5.7 `CustomerReturn` — مرتجع (phase B)

- References the original order; line items with quantities ≤ what was issued; per-line `Intact / Damaged` (D11) — intact generates `CustomerReturnIn` movements; damaged records without balance effect. Recorded by the current keeper or a Higher Manager (D24); actor + timestamps.

### 5.8 `Notification` (phase A)

- `RecipientUserId` (single user), `Type` (`StockIssueAssigned`; later `Completed/Cancelled/ReversalRequested`), payload snapshot, `IssuedAtUtc`, `ReadAtUtc`. Handlers filter strictly by recipient; no feature permission is required to read one's own notifications.

### 5.9 `OrderItem` changes (phase A)

- Stock portion: `WarehouseId` (nullable); the stock quantity is the issue-order line's `RequestedQty`. Supplier remainder: existing `SupplierId`, restricted to `Product.Suppliers` (D6).
- Item saving produces no movement and no implicit deduction; only the pending issue order accounts for the reservation in `StockItem.ReservedOnHand`.
- A stock portion requires `Inventory.Read` at save time (D25); supplier-only savings need only `Orders.Write`.
- New columns are nullable; D17's "every item must gain a source" is enforced in code on save, never in the migration.

---

## 6. Flows

### 6.1 Issue-order lifecycle (phase A)

```
stock-sourced item (final order save)
   ─► per (order × warehouse) issue order: Pending  + reservation on StockItem
        ├─ execute (current keeper / Higher Manager) ─► Completed ─► IssueOut movements + balance update + notify creator
        └─ cancel (current keeper / Higher Manager, mandatory reason) ─► Cancelled ─► reservation released, item unlocked
```
`Inventory = Approve` alone does not appear in either branch (D20).

### 6.2 Item sourcing at order editing (phase A)

1. Creator adds a product and picks quantity.
2. Each warehouse's live availability (balance minus reservations) is shown, gated by `Inventory.Read`.
3. Creator picks the warehouse (D6) for the stock portion (≤ availability); the surplus beyond it is a supplier remainder, restricted to `Product.Suppliers`.
4. Once the issue order exists, the item is locked (D7).
5. **No `Inventory.Read` (D25):** availability is hidden, stock sourcing is disabled with an Arabic hint, and the item must be supplier-sourced; the save handler rejects any stock portion.

### 6.3 Reversal workflow (phase B)

Keeper or Higher Manager initiates with a mandatory reason (`Inventory.Write` + binding) → a user with `Inventory = Approve`, who is **not the requester**, approves/rejects → on approval: paired opposite movement + original flagged `Reversed` + derived records updated in one transaction. An already-reversed movement cannot be reversed again. Approval UI lives at `/inventory/reversals` (D27).

### 6.4 Notifications (phase A)

- Bell in the header (counter + panel on the `new-nav.html` pattern) listing the recipient's notifications; each entry links to the relevant page (issue order / order). Read-time is set on open. All queries filter by recipient in handlers.

### 6.5 WhatsApp message to the keeper (phase A)

- Same pattern as `OrderSummaryDialog`: message built strictly from real data (order number, warehouse, stock items and quantities). Primary action `wa.me/{keeper phone}?text=…` after `TryNormalize`, disabled with a hint when the stored number rejects; always-enabled copy and `wa.me/?text=…` recipient picker.
- The keeper phone comes from `User.Phone`, set by the Higher Manager (D28); absence never blocks the copy/picker path. No unbacked claims, no extras.

---

## 7. Permissions (v1.3 — authoritative)

### 7.1 Features

Add two members to `PermissionFeature` between `PaymentMethods` and the Administration block (stored as text; the numeric shift is data-safe):

| Enum | Arabic (Description) |
|---|---|
| `Warehouses` | المخازن |
| `Inventory` | المخزون |

Required wiring in slice A1: `RoleFeatureCatalog` name/description/icon/tone for both, `NavMenu` group, `SealedRolePresets` + V2 seed, drift-test counts. The policy factory and role-editor enumeration pick the members up automatically.

### 7.2 Level semantics

`Read` ← Read|Write|Approve · `Write` ← Write|Approve · `Approve` ← Approve only.

Because `Approve` satisfies `Write`, **a feature gate alone is never execution authority**. The keeper-binding rule (§7.5) is mandatory in every mutating handler.

### 7.3 Preset matrix delta (locked)

| Role | Warehouses | Inventory | Rationale |
|---|---|---|---|
| مبيعات `sales` | Read | Read | Sees per-warehouse availability while sourcing order items |
| مشتريات `purchasing` | Read | Write | Records stock-in purchases |
| إدخال البيانات `data-entry` | — | — | No stock responsibility |
| مدير `manager` | Write | Approve | Warehouse control + reversal approval |
| أمين مخزن `storekeeper` **(new, sealed)** | Read | Write | Executes/cancels issue orders, records stock-in/returns for his warehouse |
| مدير أعلى `IsAdmin` | bypass | bypass | Unchanged |
| Existing custom roles | — | — | Additive default `None`; owner grants manually |

`storekeeper` full preset: `Warehouses=Read`, `Inventory=Write`, `Products=Read`, `Orders=Read`, everything else `None`.

**Seed mechanics:** V1 is frozen (`20261108214724_SeedSealedRoles`, 4 roles / 31 cells). Add `SealedRoleSeedSql.V2` in a new migration timestamped after it: insert the `storekeeper` row + its 4 cells, and 2 cells each for `sales`, `purchasing`, `manager`. Every insert is idempotent (`WHERE NOT EXISTS` by `role_key` + feature) and name-collision-safe. Expected totals: **5 sealed roles / 41 permission cells**; update `SealedRolePresets`, the reserved-name list, and all seed-test counts.

### 7.4 Action → gate matrix

| Action | Feature gate | Row / data gate |
|---|---|---|
| Warehouses list & details | `Warehouses.Read` | — |
| Warehouse create / edit / deactivate | `Warehouses.Write` | Deletion/deactivation guards (stock or pending issue orders) |
| Keeper candidate list | `Warehouses.Write` | Minimal fields only (D23) |
| Inventory balances / ledger | `Inventory.Read` | Company-wide read (D29) |
| Record stock-in | `Inventory.Write` | Product picker needs `Products.Read` (D26) |
| Issue orders list / details | `Inventory.Read` | — |
| Execute / cancel issue order | `Inventory.Write` | **`IsAdmin` OR current warehouse keeper** (D20) |
| Request reversal | `Inventory.Write` | **`IsAdmin` OR current warehouse keeper** |
| Approve / reject reversal | `Inventory.Approve` | **Approver ≠ requester** (D9) |
| Record customer return (B) | `Inventory.Write` | **`IsAdmin` OR current warehouse keeper** (D24) |
| Rebuild balances from ledger (B3) | `[Authorize(Roles = "Admin")]` | Higher Manager only (data-repair tool) |
| Item sourcing in order editor | `Orders.Write` to save; `Inventory.Read` for availability | Live availability + reservation; conflict on race (D15/D25) |
| Notification bell | authenticated | Recipient filter only |

### 7.5 Keeper-binding rule (code-level, never a DB constraint)

```
allowed = currentUser.IsAdmin
       || warehouse.KeeperUserId == currentUser.Id
```

- Evaluated **inside the Application handlers**, returning `Result.Failure("...", "Forbidden")` — not thrown, not DB-enforced.
- Applies to execute, cancel, request reversal, and record return (phase B).
- Uses the **current** keeper, so a keeper transfer immediately moves pending-work authority (D20).
- Higher-Manager overrides are audited (`ExecutedByUserId` / `CancelledByUserId`).
- The Manager's `Approve` level does **not** bypass this rule.

### 7.6 Keeper candidate query (D23)

`GetWarehouseKeeperCandidatesQuery` with `[RequirePermission(Warehouses, Write)]`, returning only `Id`, `DisplayName`, `IsActive`, `HasInventoryWrite` (true for Higher Managers or roles with `Inventory ∈ {Write, Approve}`). It never exposes usernames, roles, or phones. The warehouse editor uses it instead of `GetUsersQuery`.

### 7.7 UI conventions

All pages follow the established conventions (`PageScaffold` + `TableFooter` where applicable, `mud-blazor-overrides.css`, one primary CTA per page, no new JavaScript). Status tones via `OrderWorkflowPresentation` (brand = pending, emerald = completed, amber = awaiting, rose = cancelled/reversed/damaged). Inventory pages are **not** under `[Authorize(Roles = "Admin")]` (D27).

---

## 8. Screens (Arabic UI)

1. **المخازن** (`/warehouses`) — list + editor (name, keeper candidate selector, activity); gated `Warehouses.Read/Write`.
2. **المخزون** (`/inventory`) — per warehouse: products × balance (and reserved), plus an all-warehouses aggregated view; product ledger drill-down (movements with actor/reason); `Inventory.Read`.
3. **تسجيل توريد/شراء** (`/inventory/stock-in`) — warehouse + product lines + quantities (+ supplier + note optional); `Inventory.Write` + `Products.Read`.
4. **أوامر الإخراج** (`/inventory/issue-orders`) — list with status filter; details with items, requested/executed quantities, targeted keeper, actor trail; execute / cancel (mandatory reason) only for the current keeper or Higher Manager.
5. **المرتجعات** (`/inventory/returns`, phase B) — from the order page: lines with intact/damaged state; keeper/Higher Manager.
6. **العكس** (`/inventory/reversals`, phase B) — request list + approval queue; request = `Inventory.Write` + binding, decision = `Inventory.Approve`, approver ≠ requester.
7. **Header bell** — new in `MainLayout`, permission-agnostic, recipient-filtered.
8. **Order editor** — per-item warehouse picker + supplier remainder handling; item locking per D7; availability hidden when `Inventory.Read` is absent (D25).

Nav group «المخزون» (icon `Warehouse`, subtitle «المخازن والأرصدة وأوامر الإخراج») with items: المخازن (`Warehouses.Read`), المخزون (`Inventory.Read`), أوامر الإخراج (`Inventory.Read`), المرتجعات (`Inventory.Read`, phase B), العكس (`Inventory.Approve`, phase B); create CTA «تسجيل توريد» (`Inventory.Write`).

---

## 9. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Execution exceeds live balance (race) | Reject at execution time (D15) with `Result.Failure`; `Version` on `StockItem` + `"Conflict"` code |
| Wrong warehouse selection keeps issuing against an empty one | Availability shown live at selection; requirement to pick explicitly (D6) |
| Ledger tampering | Movements immutable; remediation only via cancel/reverse with reasons |
| Notifications leak beyond recipient | Strict per-recipient handler filtering |
| `Supply` impact | Untouched by D2/D16 |
| Item locks block editing after issue order | Resolved by the documented cancel-then-edit path (D7/D10) |
| SQLite + snake_case migration pitfalls | Standard conventions from the repo testing model; test options identical to existing tests |
| Permission drift / over-privilege | Keeper-binding handler checks; V2 seed drift test; permission-specific risks R17–R24 in §13.2 |

---

## 10. Assumptions for phase scope

- **Prerequisite (done):** roles redesign R0 — `Approve`, sealed presets, Higher Manager gates. Inventory A1 depends on it.
- **Phase A (in scope):** Warehouses feature, `StockItem`, stock-in purchases, item sourcing rules + locking, issue orders + numbering + execute/cancel (mandatory reason), reservations accounting, `Notification` + header bell, WhatsApp message to keeper, permission wiring (features, catalog, nav, V2 seed, candidate query).
- **Phase B (in scope):** customer returns (intact/damaged), reversal with `Inventory.Approve`, admin rebuild-from-ledger tool, aggregate reporting refinements (total per product across warehouses).
- **Documented prerequisites:** stock-in needs `Products.Read` (D26); availability needs `Inventory.Read` (D25); `storekeeper` includes `Products.Read` + `Orders.Read` (D22).
- **Out of scope (both phases):** periodic stocktakes, FIFO/FEFO/monetary valuation, supplier returns, SMS/push/Bot, BOM, property-level (SKU) stock, automated reorder, per-user balance scoping (D29).

---

## 11. Compatibility contracts verified

- `Result`/`Result<T>` returns across new handlers (no throwing for expected failures, `"Conflict"` on concurrency); handler-level auth failures return `Result.Failure`.
- `PermissionFeature` grows two entries with Arabic `Description` attributes (المخازن، المخزون); roles editor 14 → 16 cards; `NavMenu` gains the group; feature gates via `[RequirePermission]`/`PermissionAccess`.
- `Approve` now exists end-to-end; reversal approval uses it (roles R1 guard-line fix verified).
- `SealedRoleSeedSql.V1` stays frozen; V2 is additive and idempotent; `SealedRolePresets` grows to 5 roles / 41 cells; seed tests updated accordingly.
- Nothing in `Supply`, documents, pricing, delivery, or numbering of existing parties is modified (D16).
- SQLite tests inherit the exact existing option set (snake_case naming, `EnsureCreated`).
- UI language: Arabic, RTL, no runtime font fetching, no new JavaScript files, Tailwind + overrides discipline.

---

## 12. Implementation plan (vertical slices)

### Phase A
0. **R0 — Roles & Permissions redesign** — ✅ **DONE** (Domain `Approve`/`IsSealed`/`RoleKey`; Application hierarchy + guards; `20261108214724_SeedSealedRoles`; Web 4-level matrix, sealed read-only, Higher Manager gates; tests).
1. **A1 — Warehouses domain + Infrastructure + UI + permission wiring** — `Warehouse` entity/configuration/migration; `PermissionFeature.Warehouses`/`Inventory`; `RoleFeatureCatalog` entries; `NavMenu` group; `SealedRolePresets` + `SealedRoleSeedSql.V2` migration; `GetWarehouseKeeperCandidatesQuery`; warehouse screens; drift-test updates.
2. **A2 — InventoryMovement + StockItem + stock-in purchases** — ledger, cached balance, stock-in screen; `Products.Read` prerequisite.
3. **A3 — Order item sourcing** — `OrderItem.WarehouseId`, availability query, supplier remainder, validation, item locking, no-`Inventory.Read` fallback.
4. **A4 — StockIssueOrder** — auto-creation on final save, numbering, execute/cancel flows with keeper-binding guard, audit fields, keeper contact query for WhatsApp.
5. **A5 — Notification + header bell + WhatsApp keeper message.**
6. **A6 — Tests** (unit: domain rules; app: handlers with SQLite; UI conventions; permission matrix).

### Phase B
1. **B1 — Customer returns** (intact/damaged; ledger effects; keeper/Higher Manager actor).
2. **B2 — Reversal requests + `Inventory.Approve` approval** (requester ≠ approver; `/inventory/reversals`).
3. **B3 — Rebuild tool (Higher Manager) + aggregated stock reporting.**

Each slice: Domain → Application → Infrastructure → Web, then `dotnet restore / build Release / test Release --no-build`. **Migration ordering:** the inventory migration must be timestamped after `20261108214724_SeedSealedRoles`; never edit V1.

---

## 13. Data safety, legacy compatibility & risk register

### 13.1 Governing constraint policy (owner directive)

Wherever old data could conflict with a new rule, **the constraint lives in code** (Domain validation + handler checks), never in a breaking migration or `NOT NULL`/`CHECK`/FK change on existing tables. Concretely:

| Situation | Policy |
|---|---|
| New rule needing `NOT NULL` on an existing table (`OrderItem` source/warehouse) | Nullable column + code-level validation at save/handler time (D17) |
| New tables (all stock tables) | Free to carry strict PKs and unique indexes — no legacy rows exist |
| Existing tables | **Additive-only migrations** (new tables, nullable columns); no type changes on `Order`/`OrderItem`/`Product` |
| Permission rows | Additive seed only; V1 frozen; V2 idempotent by `role_key` + feature |
| Deletion guards | Application-level (trips-spec pattern); no destructive cascades added |

### 13.2 Risk register

| # | Risk | Class | Mitigation |
|---|---|---|---|
| R1 | Legacy `OrderItem` rows lack source/warehouse | Data | Nullable columns; code-only validation; D17 re-save capture |
| R2 | Historic orders affected by new logic | Data | No retroactive issues/deductions/notifications; ledger starts empty; opening stock only via D21 |
| R3 | Double reservation race (two creators, last units) | Concurrency | Reservation increments inside the order-save transaction against `StockItem.Version`; second saver gets `"Conflict"` |
| R4 | Reservation leak (order cancelled/archived with `Pending` issue order) | Lifecycle | Auto-cancel + release + system history (D18) |
| R5 | Execution-time stock race | Concurrency | Re-validate live balance at execution; explicit `Result.Failure`; no negative ledger (D15) |
| R6 | Cached balance drift | Data | `StockOnHand`/`ReservedOnHand` updated in the same transaction as movements; ledger wins; Phase-B rebuild tool |
| R7 | Deleting a warehouse/product/supplier in use | Integrity | Application-level deletion guards; no silent cascades |
| R8 | Keeper deactivated / loses access | Lifecycle | Higher-Manager fallback execute/cancel with audit (D20); assignment warns on missing `Inventory.Write` |
| R9 | Item locking vs order lock interplay | Logic | Locked order (`CanEdit = false`) never spawns/mutates issue orders; `Pending` issue order locks the item (D7) |
| R10 | Damaged returns silently changing totals | Logic | Damaged units recorded without balance effect, displayed separately (D11) |
| R11 | Reversal half-applied states | Data | Ledger untouched until approval; on approval a single transaction applies the pair (D9) |
| R12 | Drafts touching stock | Logic | Issue orders + notifications only on final save; drafts never reserve |
| R13 | WhatsApp needs a phone that doesn't exist yet | Flow | Optional nullable `User.Phone` (D19) + copy/picker fallback; HM-owned setup (D28) |
| R14 | Abusing/executing by unbound users | Security | Execution bound to current keeper or Higher Manager only (D20); handler-level `Result.Failure` |
| R15 | Never-ending `Pending` orders | Flow | Allowed to age, always cancellable with mandatory reason; nothing auto-destructs |
| R16 | RTL number rendering | UI | All quantities formatted with `CultureInfo.InvariantCulture` |
| R17 | Manager `Approve` over-privileges execution | Security | Handler keeper-binding check; test that a non-bound Manager is rejected (D20) |
| R18 | Sealed presets silently get zero inventory access | Data/UX | V2 seed + drift test asserting 5 roles / 41 cells |
| R19 | Keeper transfer strands pending work | Lifecycle | Authority resolved against the current keeper; transfer UI shows affected pending count |
| R20 | Self-approved reversal | Security | `Approver != Requester` guard + test (D9) |
| R21 | Manager cannot assign keepers (`Users = None`) | Flow | Minimal `Warehouses.Write`-gated candidate query (D23) |
| R22 | New enum members render as English generic cards | UX | `RoleFeatureCatalog` entries in A1 |
| R23 | Storekeeper/preset count drift breaks seed tests | Test | Update `SealedRolePresets`, reserved names, role count 4→5, cells 31→41 |
| R24 | Keeper phone never set | Flow | HM-owned Users page field (D28); copy/picker always available |

### 13.3 Follow-through

- Amendments to `User` (phone) and orders (item lock, lifecycle hook) are additive and minimal, listed in the A3/A4 slices.
- The constraint policy above supersedes any generic "strict DB constraints" reading of §5; field tables there stay accurate for **new tables only**.
- Permission changes are additive and code-enforced; the only DB additions are the two enum values (text rows) and the V2 seed, all owner-approved.

---

## 14. Permission test plan

**Application (SQLite, `EnsureCreated`, snake_case):**
1. Authorization matrix: `Read` ← R/W/A, `Write` ← W/A, `Approve` ← A (including the former silent-deny case).
2. Execute/cancel: current keeper succeeds; `Inventory.Write` non-keeper fails with `Result.Failure`; Manager-with-`Approve` non-keeper fails; Higher Manager succeeds and is audited.
3. Keeper transfer: old keeper rejected, new keeper accepted on the same pending order.
4. Reversal: request requires `Inventory.Write` + binding; approval requires `Inventory.Approve`; requester ≠ approver; double reversal rejected.
5. Preset drift: 5 sealed roles × their exact cells (41 total); idempotent V2; reserved `storekeeper` name.
6. Candidate query: requires `Warehouses.Write`; returns minimal fields; `HasInventoryWrite` correct (including Higher Managers).
7. Order save: stock portion requires `Inventory.Read`; supplier-only allowed without it; reservation under `Orders.Write`; race returns `"Conflict"`.
8. Stock-in: requires `Inventory.Write` + `Products.Read`.
9. Seed guards: sealed rename/delete/set-permission rejected; V1 untouched by V2.

**Web/UI:**
- Nav group and items hidden/shown by feature level (including the Approve-only reversal item).
- Role editor renders 16 cards with 4 segments; sealed roles read-only; `storekeeper` shows as «دور جاهز».
- Issue-order actions hidden for non-bound users; handler still rejects if forced.
- Reversal queue visible only with `Inventory.Approve`.
- Warehouse editor warns when the selected keeper lacks inventory access.
