# Inventory (Stock) Feature — Study & Specification

- **Version:** 1.2 (data-safety findings + owner decisions D17–D21 locked 2026-11-08)
- **Status:** Approved study; no production code written yet. Implementation starts with slice A1 (Warehouses) only after final approval.
- **Date:** 2026-11-08
- **Repository:** OrderHub
- **Style:** follows `plans/TRIPS-MANAGEMENT-SPEC.md`. All user-facing UI strings are Arabic; identifiers below are code names.

---

## 1. Purpose

Add product stock (مخزون المنتجات) to OrderHub:

1. Every order item declares its fulfillment source: **stock** when the selected warehouse can supply (part or all of) the quantity, otherwise (or for the surplus) a **supplier linked to the product**.
2. Stock balances derive exclusively from explicit movements (purchases/stock-in, issue-order execution, customer returns, reversals). A cached column is display-only; there is **no manual editing of stock numbers**.
3. Stock-sourced items generate **Stock Issue Orders** (أوامر إخراج من المخزن) per warehouse, and the warehouse's keeper is **notified in-app** (visible to him only) and can be messaged on **WhatsApp** (copy / send link, `OrderSummaryDialog` pattern).
4. The keeper records execution in-app. The operation links both **who requested it** (order creator) and **who executed it** (the keeper).
5. Nothing is added or removed except through an explicit operation with recorded data.
6. Customer **returns** are recorded with an intact/damaged state (only intact units rejoin stock).
7. A **reversal** workflow fixes mistakes (reversal entries, never deletion), initiated by the keeper and **approved by an admin**.
8. A **cancel** with a mandatory reason closes an issue order before execution.
9. **Multiple warehouses**, each with exactly one keeper user; balances are per warehouse per product, with an aggregated totals view.

---

## 2. Domain background — standard inventory practice observed

1. **The stock ledger is the source of truth.** Every balance change passes through an explicit `InventoryMovement` row carrying its source-document reference, actor, and time. Balance = sum of active movements.
2. **No silent operations.** A sale deducts because an issue order was executed; inbound increases because a stock-in was recorded; a return increases because a return was registered. Nothing changes implicitly behind ordinary edits.
3. **Cached balances are derived, never authoritative** — updated transactionally with each movement, rebuildable from the ledger.
4. **Reversals, not deletion** — the accounting reversal-entry pattern: the mistaken movement stays flagged; a paired opposite movement references it.
5. **Cancel ≠ reverse** — cancellation applies only to what has not drawn stock; executed operations go through reversal.
6. **Explicit approval for sensitive operations** — here: reversals require admin approval.
7. **Returns** are distinct operations referencing the original document, with quantities ≤ what was issued.
8. **Audit trail** — every movement carries its creator, timestamps, and a mandatory reason for cancels/reversals. Historic rows are never deleted or edited.

---

## 3. Current codebase touchpoints

| Component | State | Relation |
|---|---|---|
| `OrderItem.SupplierId` / `SupplierName` | Optional; no stock-vs-supplier concept | Gains stock source fields (§6.3) |
| `Product.Suppliers` | Exists (`AddSupplier`, `HasSupplier`, …) | "Supplier linked to the product" ready |
| `Supply` | Order-bound, full `WorkRecordStatus` lifecycle, **no item quantities** | Untouched: purchases are a new independent operation (D3) |
| `PermissionFeature` | No warehouses/inventory entries | Adds `Warehouses` + `Inventory` features, nav entries, role-editor cards |
| Notifications | None exist | New page-agnostic, user-targeted `Notification` entity + header bell |
| `User` / roles | Exists | `KeeperUserId` on warehouse; `RequestedByUserId` / `ExecutedByUserId` audit |
| WhatsApp | `OrderSummaryDialog` + `WhatsAppMessaging.TryNormalize` | Reused for the keeper message |
| Numbering | `NumberSequenceKind` monthly per-party sequences | New warehouse numbering kind (D8) |
| Order drafts (`OrderDrafts`) | Draft-flow exists | Issue orders + notifications occur on final save, aligned with the drafts flow |

**Binding architecture notes:** MediatR (`RequestExecutor`) + `Result/Result<T>`; Domain entities private-setter/private-ctor/internal-ctor/factory validation; new EF columns must survive snake_case + SQLite tests; row-level authz via `PermissionAccess.IsAllowedAsync`; expected failures return `Result.Failure`.

---

## 4. Locked decisions (owner, 2026-11-08)

| # | Decision |
|---|----------|
| D1 | Stock is **not** enabled per-product by a flag: an item takes a stock source when the product has balance in a warehouse; otherwise — or for the surplus beyond the balance — it goes to a supplier linked to the product. |
| D2 | Purchases/stock-in are a **standalone stock-in operation**, independent of the existing order-bound `Supply` entity, which stays unchanged. |
| D3 | The per-product "stock value" column is a **units count** balance (رصيد), not money. No cost valuation in this feature. |
| D4 | **Multiple warehouses**: a full `Warehouses` feature (create/edit/deactivate, keeper assignment). Stock balance is tracked **per (product, warehouse)** with an aggregated totals view across warehouses. |
| D5 | Exactly **one keeper user per warehouse**; notifications target the keeper's user directly. |
| D6 | When adding an item within stock quantity rules, the order creator **explicitly selects the warehouse**; the item reserves the available balance there; the surplus beyond the balance stays an open remainder directed to a linked supplier (one item, flagged split: stock portion + supplier remainder). |
| D7 | Once an issue order exists for an item, the item is **locked against edits**; changes require cancelling the (not-yet-executed) issue order with a mandatory reason first. |
| D8 | Issue orders get **monthly numbering** per warehouse following the existing per-party sequence pattern. |
| D9 | **Reversal approval**: the keeper may initiate a reversal; it takes effect only after **admin approval**. The mandatory reason is recorded. |
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
| D20 | Issue-order execution/cancel rights: the **warehouse-bound keeper user** (the user assigned to that warehouse), or the **admin** as fallback. An issue order is never assigned to — nor executable by — a user not bound to that warehouse. Deactivated keepers are resolved by an admin (override execute/cancel), fully audited. |
| D21 | Opening stock enters **only** through an explicit stock-in operation (ordinary stock-in or a flagged initial-opening entry) recorded as visible ledger movements with actor/time. **No manual balance editing exists at any point** — the same rule governs opening stock. |

---

## 5. Entities (proposed)

### 5.1 `Warehouse` — المخزن (phase A)

- `Name` (EntityName), `KeeperUserId` (one user), activity flag for soft deactivation (not delete while stock or orders reference it — deletion guards, like trips spec).
- EF configuration in `Configurations/WarehouseConfiguration.cs`; migration with descending-timestamp naming.

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
- Updated transactionally with each movement; admin-only rebuild-from-ledger tool (phase B). Never edited manually (D: no manual edits).

### 5.4 `StockPurchase` — standalone stock-in (phase A)

- Header: warehouse, optional supplier, optional note, user/timestamp/`Version`; item lines: product + quantity; generates one `StockIn` movement per line. Has a minimal, immutable lifecycle (no cancel-after-record; mistakes via reversal in phase B).

### 5.5 `StockIssueOrder` — أمر الإخراج (phase A)

| Field | Notes |
|---|---|
| `OrderId` · `WarehouseId` | one order per (Order × Warehouse); aggregates that warehouse's stock items |
| `Status` | `Pending → Completed / Cancelled` (existing `WorkRecordStatus` values) |
| Number | monthly per warehouse (D8) |
| `RequestedByUserId` · `RequestedAtUtc` | order creator of the stock-sourced item |
| `AssignedUserId` | the warehouse keeper (D5) — notification target |
| `ExecutedByUserId` · `ExecutedAtUtc` | executor and time (D: audit) |
| `CancelledByUserId` · `CancelReason` | mandatory reason (D10) |
| Item lines | `ProductId` · `ProductName` · `RequestedQty` (stock portion) · `ExecutedQty` |

- Created automatically on final order save containing stock items — never manually.
- Execution: records executed quantities (full qty in phase 1), generates `IssueOut` movements, updates `StockItem`, returns a notification to the order creator.

### 5.6 `InventoryReversal` — reversal with admin approval (phase B)

- Request by the keeper with mandatory reason + target movement; status `PendingApproval → Approved / Rejected`; on approval generates the paired `Reversal` movement, flags the original `Reversed`, and re-derives the affected `StockItem`/issue order. Never deletes.

### 5.7 `CustomerReturn` — مرتجع (phase B)

- References the original order; line items with quantities ≤ what was issued; per-line `Intact / Damaged` (D11) — intact generates `CustomerReturnIn` movements; damaged records without balance effect. Actor + timestamps.

### 5.8 `Notification` (phase A)

- `RecipientUserId` (single user), `Type` (`StockIssueAssigned`; later `Completed/Cancelled/ReversalRequested`), payload snapshot, `IssuedAtUtc`, `ReadAtUtc`. Handler filters strictly by recipient.

### 5.9 `OrderItem` changes (phase A)

- Stock portion: `WarehouseId` (nullable), with reserved quantity derived from the issue order; supplier portion: existing `SupplierId` for the surplus remainder (D6) — both per `Product.Suppliers` only for the supplier part.
- Item saving produces no movement and no implicit deduction; only the pending issue order accounts for the reservation in `StockItem.ReservedOnHand`.

---

## 6. Flows

### 6.1 Issue-order lifecycle (phase A)

```
stock-sourced item (final order save)
   ─► per (order × warehouse) issue order: Pending  + reservation on StockItem
        ├─ execute (keeper) ─► Completed ─► IssueOut movements + balance update + notify creator
        └─ cancel (keeper/admin, mandatory reason) ─► Cancelled ─► reservation released, item unlocked
```

### 6.2 Item sourcing at order editing (phase A)

1. Creator adds a product and picks quantity.
2. Each warehouse's live availability (balance minus reservations) is shown.
3. Creator picks the warehouse (D6) for the stock portion (≤ availability); the surplus beyond it is a supplier remainder, restricted to `Product.Suppliers`.
4. Once the issue order exists, the item is locked (D7).

### 6.3 Reversal workflow (phase B)

Keeper initiates with mandatory reason → admin approves/rejects → on approval: paired opposite movement + original flagged `Reversed` + derived records updated. An already-reversed movement cannot be reversed again.

### 6.4 Notifications (phase A)

- Bell in the header (counter + panel on the `new-nav.html` pattern) listing the recipient's notifications; each entry links to the relevant page (issue order / order). Read-time is set on open. All queries filter by recipient in handlers.

### 6.5 WhatsApp message to the keeper (phase A)

- Same pattern as `OrderSummaryDialog`: message built strictly from real data (order number, warehouse, stock items and quantities). Primary action `wa.me/{keeper phone}?text=…` after `TryNormalize`, disabled with a hint when the stored number rejects; always-enabled copy and `wa.me/?text=…` recipient picker. No unbacked claims, no extras.

---

## 7. Permissions

| Access | Who | What |
|---|---|---|
| `Warehouses` (Read/Write) | admins | warehouse CRUD + keeper assignment |
| `Inventory` (Read/Write) | keeper + admins | stock pages, stock-in recording, issue orders (execute/cancel), returns, reversal initiation |
| Reversal approval | admins | approve/reject `InventoryReversal` requests |
| within `Orders.Write` | order creators | item source + warehouse selection |

All pages follow the established conventions (`PageScaffold` + `TableFooter` where applicable, `mud-blazor-overrides.css`, one primary CTA per page, no new JavaScript). Status tones via `OrderWorkflowPresentation` (brand = pending, emerald = completed, amber = awaiting, rose = cancelled/reversed/damaged).

---

## 8. Screens (Arabic UI)

1. **المخازن** — list + editor (name, keeper user selector, activity).
2. **المخزون** — per warehouse: products × balance (and reserved), plus an all-warehouses aggregated view; product ledger drill-down (movements history with actor/reason).
3. **تسجيل توريد/شراء** — warehouse + product lines + quantities (+ supplier + note optional).
4. **أوامر الإخراج** — list with status filter; details with items, requested/executed quantities, targeted keeper, actor trail; execute / cancel (mandatory reason).
5. **المرتجعات** — from the order page (phase B): lines with intact/damaged state.
6. **العكس** — reversal request + admin approval list (phase B).
7. **Header bell** — new in `MainLayout`, permission-gated, recipient-filtered.
8. **Order editor** — per-item warehouse picker + supplier remainder handling; item locking per D7.

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

---

## 10. Assumptions for phase scope

- **Phase A (in scope):** Warehouses feature, `StockItem`, stock-in purchases, item sourcing rules + locking, issue orders + numbering + execute/cancel (mandatory reason), reservations accounting, `Notification` + header bell, WhatsApp message to keeper.
- **Phase B (in scope):** customer returns (intact/damaged), reversal with admin approval, admin rebuild-from-ledger tool, aggregate reporting refinements (total per product across warehouses).
- **Out of scope (both phases):** periodic stocktakes, FIFO/FEFO/monetary valuation, supplier returns, SMS/push/Bot, BOM, property-level (SKU) stock, automated reorder.

---

## 11. Compatibility contracts verified

- `Result`/`Result<T>` returns across new handlers (no throwing for expected failures, `"Conflict"` on concurrency).
- `PermissionFeature` grows two entries with Arabic Description attributes (المخازن، المخزون); roles editor matrix and `NavMenu` gain entries; row-level gating through `PermissionAccess.IsAllowedAsync`.
- Nothing in `Supply`, documents, pricing, delivery, or numbering of existing parties is modified (D16).
- SQLite tests inherit the exact existing option set (snake_case naming, `EnsureCreated`).
- UI language: Arabic, RTL, no runtime font fetching, no new JavaScript files, Tailwind + overrides discipline.

---

## 12. Implementation plan (vertical slices)

### Phase A
1. **A1 — Warehouses domain + Infrastructure + UI** (entity, configuration, migration, Permissions, screens, keeper assignment).
2. **A2 — InventoryMovement + StockItem + stock-in purchases** (ledger, cached balance, purchase screen).
3. **A3 — Order item sourcing** (warehouse picker, supplier remainder, validation, item locking rules).
4. **A4 — StockIssueOrder** (auto-creation on final save, numbering, execute/cancel flows, audit fields).
5. **A5 — Notification + header bell + WhatsApp keeper message.**
6. **A6 — Tests** (unit: domain rules; app: handlers with SQLite; UI conventions).

### Phase B
1. **B1 — Customer returns** (intact/damaged; ledger effects).
2. **B2 — Reversal requests + admin approval.**
3. **B3 — Rebuild tool + aggregated stock reporting.**

Each slice: Domain → Application → Infrastructure → Web, then `dotnet restore / build Release / test Release --no-build`.

---

## 13. Data safety, legacy compatibility & risk register (v1.1 re-review)

### 13.1 Governing constraint policy (owner directive)

Wherever old data could conflict with a new rule, **the constraint lives in code** (Domain validation + handler checks), never in a breaking migration or `NOT NULL`/`CHECK`/FK change on existing tables. Concretely:

| Situation | Policy |
|---|---|
| New rule needing `NOT NULL` on an existing table (`OrderItem` source/warehouse) | Nullable column + code-level validation at save/handler time (D17) |
| New tables (all stock tables) | Free to carry strict PKs and unique indexes — no legacy rows exist |
| Existing tables | **Additive-only migrations** (new tables, nullable columns); no type changes on `Order`/`OrderItem`/`Product` |
| Deletion guards | Application-level (trips-spec pattern); no destructive cascades added |

Verified code facts supporting this: `OrderItem.SupplierId` is already nullable; orders are never hard-deleted (archive exists, `ChangeOrderStatus` → `Cancelled`); `User` has no phone field today; SQLite tests use `EnsureCreated` and inherit the same nullable shape.

### 13.2 Risk register

| # | Risk | Class | Mitigation |
|---|---|---|---|
| R1 | Legacy `OrderItem` rows lack source/warehouse | Data | Nullable columns; code-only validation; D17 re-save capture |
| R2 | Historic orders affected by new logic | Data | No retroactive issues/deductions/notifications; ledger starts empty; opening stock only via D21 |
| R3 | Double reservation race (two creators, last units) | Concurrency | Reservation increments inside the order-save transaction against `StockItem.Version`; second saver gets `"Conflict"` |
| R4 | Reservation leak (order cancelled/archived with `Pending` issue order) | Lifecycle | Auto-cancel + release + system history (D18) |
| R5 | Execution-time stock race (balance moved after item save) | Concurrency | Re-validate live balance at execution; explicit `Result.Failure`; no negative ledger (D15) |
| R6 | Cached balance drift | Data | `StockOnHand`/`ReservedOnHand` updated in the same transaction as movements; ledger wins; Phase-B rebuild tool |
| R7 | Deleting a warehouse/product/supplier in use | Integrity | Application-level deletion guards; no silent cascades |
| R8 | Keeper deactivated / loses access | Lifecycle | Admin fallback execute/cancel with audit (D20); assignment restricted to warehouse-bound users |
| R9 | Item locking vs order lock interplay | Logic | Locked order (`CanEdit = false`) never spawns/mutates issue orders; `Pending` issue order locks the item (D7) |
| R10 | Damaged returns silently changing totals | Logic | Damaged units recorded without balance effect, displayed separately (D11) |
| R11 | Reversal half-applied states | Data | Ledger untouched until admin approval; on approval a single transaction applies the pair (D9) |
| R12 | Drafts touching stock | Logic | Issue orders + notifications only on final save; drafts never reserve |
| R13 | WhatsApp needs a phone that doesn't exist yet | Flow | Optional nullable `User.Phone` (D19) + copy/picker fallback |
| R14 | Abusing/executing by unbound users | Security | Execution bound to warehouse keeper or admin only (D20); `Inventory` permission gates pages |
| R15 | Never-ending `Pending` orders | Flow | Allowed to age, always cancellable with mandatory reason; nothing auto-destructs |
| R16 | RTL number rendering | UI | All quantities formatted with `CultureInfo.InvariantCulture` |

### 13.3 Follow-through

- Amendments to `User` (phone) and orders (item lock, lifecycle hook) are additive and minimal, listed in the A3/A4 slices.
- The constraint policy above supersedes any generic "strict DB constraints" reading of §5; field tables there stay accurate for **new tables only**.

