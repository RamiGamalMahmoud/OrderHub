# Inventory (Multi-Warehouse Stock) Feature — Study & Specification

- **Version:** 2.1 (v2.0 + owner round-2 decisions 17–21, 2026-11-12)
- **Status:** Approved specification; **no inventory production code written yet.** Supersedes v2.0/v1.3 entirely — wherever an older file conflicts with this file or with `design_draft.md`, **the newer documents win** (owner directive).
- **Sources of truth (in order):** `concepts_brief.md` (locked scope) · `design_draft.md` (architecture + § قرارات المالك المحسومة + § قرارات الجولة الثانية, 21 decisions) · this file (executable detail) · `AGENTS.md` (house rules).
- **Date:** 2026-11-12
- **Style:** follows `plans/TRIPS-MANAGEMENT-SPEC.md`. All user-facing UI strings are Arabic; identifiers are code names.

---

## 1. Purpose

Add multi-warehouse stock management to OrderHub:

1. Every order item is **allocated** by the sales user per its chosen source (D17): one or more warehouses, a linked supplier, or both, from live per-warehouse availability; warehouse-sourced items are reserved on final save, supplier-sourced quantities stay informational.
2. **Final save** with status «قيد التجهيز» reserves all stock allocations **atomically**; drafts reserve nothing. No issue orders are created automatically.
3. The sales user creates **manual stock issue orders** (`StockIssueOrder`), one per warehouse, in any number of later stages, up to the remaining reserved quantity.
4. The warehouse keeper records **actual issue in batches** (`StockIssueBatch`); only issued quantity leaves the balance; the un-issued remainder stays reserved; a shortfall requires a mandatory reason visible to sales.
5. Stocked delivery is tracked per product (`Delivered ≤ Issued`); **no stocked quantity can be delivered before it is issued**.
6. Stock-in (purchases and opening balance) is a standalone documented operation by the keeper, with optional PDF/JPG/PNG attachments ≤ 5 MB each.
7. Counts (جرد), adjustments, returns (intact/damaged), damaged disposal, and movement reversals are all explicit documented operations; **no balance changes silently**; sensitive operations require Manager approval.
8. Every quantity change goes through the immutable movement ledger; the cached balance is derived and rebuildable.

**Out of scope:** purchase orders for suppliers (the supplier choice stays informational on the order, current behavior) · internal warehouse transfers · units of measure (quantities are integers, product-level only, no SKU/property stock) · cost/valuation · SMS/push/BOM/reorder automation.

---

## 2. Quantity model — invariants (binding)

| # | Invariant |
|---|---|
| Q1 | `Available = StockOnHand − ReservedOnHand` (per product × warehouse), never < 0 |
| Q2 | `ReservedOnHand = Σ(allocations active for this warehouse) − Σ(batches issued against them)` — **issued quantity leaves the reservation the moment it is issued** (prevents double-subtracting issued units from availability) |
| Q3 | `ReservedOnHand ≤ StockOnHand` always |
| Q4 | `Σ(issue-order lines against one allocation, active orders) ≤ allocation.Qty` |
| Q5 | `IssuedQty(allocation) = Σ(batches)` ≤ allocation stock qty |
| Q6 | `Delivered ≤ Issued` per stocked order item (delivered recorded, never re-deducted from balance) |
| Q7 | `Returned ≤ Issued` per order; `Intact + Damaged ≤ Returned-eligible issued qty` |
| Q8 | `DamagedDisposal ≤ StockBalance.DamagedQty` |
| Q9 | Every change to `StockOnHand`/`DamagedQty` produces exactly one ledger movement; `ReservedOnHand` is derived from allocations/batches, not movements |
| Q10 | Ledger rows are immutable (insert-only) except internal `Status → Reversed`; "المتبقي" is always computed, never stored |
| Q11 | `DamagedQty = Σ damaged-affecting movements` (`ReturnDamagedIn` +, `DamagedDisposal` −, `DamagedAdjustment` ±) and is never part of `Available`; a count covers `StockOnHand` and `DamagedQty` as two separate columns (D20) |
| Q12 | A decision applies at most once: the approval/count `Status` transition is a `Version` CAS (`"Conflict"` otherwise) (D21) |

`Actual` (rebuild path) = Σ movements by `EffectiveAtUtc`. `Reserved` (rebuild path) = active allocations − batches. A Higher-Manager rebuild tool (slice 7) recomputes both and records before/after.

---

## 3. Codebase touchpoints (verified 2026-11-12)

| Component | State | Relation |
|---|---|---|
| `UpdateOrderHandler` / `Order.ReplaceOrderItems` | Deletes and re-inserts all items; `OrderItem.Id` unstable today | Slice 5 adds explicit client-sent item `Id`s (pattern: `SynchronizeDeliverySteps`) + FK `Restrict` from allocation to item — **owner-approved behavior change** |
| `OrderItem.SupplierId/SupplierName` | Optional, hard-required by create/update handlers | Stays the supplier-source field. **After S5 (D17):** required only when the item has a `SourceKind = Supplier` allocation; a fully stock-sourced item saves with no supplier |
| `Order.Lifecycle` (`TransitionStep`, `CanArchive`, `DeliveryReadinessError`) | Knows nothing about stock | Slice 5 adds reservation release on Delivered/Cancelled/Archived — **owner-approved** — and leaves readiness stock-free (D19). The per-delivery-record cap lands in slice 8 |
| `DeliveryTrip` / `Shipment` / `ConfirmPickup` | Trips are independent work records; shipping creates `Shipment`; pickup has no trip | Slice 8 adds `StockDeliveryRecord` linked to order, optionally to trip/shipment |
| `NumberSequenceService` | Monthly per-party, rejects unknown kinds; two `CHECK` constraints enumerate kinds and scope (`NumberSequenceConfiguration.cs:15-37`) | `NumberSequenceKind.Warehouse` added to the monthly allow-list (slice 6). The same migration recreates `ck_number_sequences_kind` / `ck_number_sequences_scope` to admit `Warehouse` — the single owner-approved exception to additive-only (D18): no rows touched, `Down` restores the original definitions, regression test proves the four legacy kinds still insert |
| `IWorkRecord` pipeline (`UnitOfWork` auto-numbering, status history) | Generic; `Supply`/`DeliveryTrip` use it | `StockIssueOrder` joins as `WorkRecordType.StockIssueOrder` |
| `PermissionFeature` (14 members, stored as text) / sealed presets (4 roles / 31 cells, `SealedRoleSeedSql.V1` **frozen**) | Roles redesign R0 complete | `Warehouses` + `Inventory` inserted after `PaymentMethods`; `V2` seed migration is additive/idempotent |
| `AttachmentsController` + `LocalFileAccessService` | No ownership/size/type checks; stores by raw name | **Untouched.** Inventory attachments get a separate endpoint + folder + metadata entity (owner decision) |
| `SealedRolePresets` / roles editor / `RoleFeatureCatalog` | 4 sealed roles; catalog switch | 5th sealed role «أمين مخزن»; two new catalog cards |
| Legacy data safety | AGENTS.md rule 7 | All migrations additive; new columns nullable; legacy orders load unchanged; enforced-in-code rules only. **Documented exception (D18):** the issue-order migration recreates the two `number_sequences` CHECK constraints; no data is touched and rollback restores the originals |

---

## 4. Locked decisions (owner)

Carried from `design_draft.md` § قرارات المالك المحسومة (2026-11-12) — restated for one-stop reference:

| # | Decision |
|---|----------|
| D1 | Storekeeper preset: `Warehouses=Read`, `Products=Read`, `Inventory=Write` — **no** `Orders.Read`; order number renders as text (no link) on issue-order screens. |
| D2 | `Inventory.Read` is company-wide (sales see all warehouses in the allocation screen); warehouse-scoped reads apply **only** to the storekeeper inside inventory screens, enforced in handlers. |
| D3 | Seed matrix: مبيعات `Inventory=Read` · مشتريات `Inventory=Read` · مدير `Warehouses=Write + Inventory=Approve` · أمين مخزن (new sealed) per D1 · data-entry unchanged. |
| D4 | `Approver ≠ Requester` for all approval types, with **two exceptions**: `MovementReversal` (Manager may approve a correction he started) and a count he recorded himself (§5.8). |
| D5 | Damaged stock: third column `DamagedQty` on `StockBalance`; damaged-in movement on sorting, disposal movement on destruction; never inside `Available`. |
| D6 | Reversal marks the original `Reversed` (loses effect) + paired opposite movement referencing it. |
| D7 | Ledger has `EffectiveAtUtc` (actual time, future rejected with margin) + `CreatedAtUtc` (recording time). "Book balance at count time" = Σ movements with `EffectiveAtUtc ≤ snapshot moment`. |
| D8 | `StockIssueBatch` entity per execution batch (qty, actor, time, shortfall reason **mandatory when the batch is final and Σ < RequestedQty**). `ExecutedQty` on lines is derived/validated from batches. |
| D9 | **Partial delivery is allowed.** No order-level stock gate: each `StockDeliveryRecord` is capped by `Issued − Delivered` for its allocation (Q6). A stocked quantity that was never issued can never be delivered. Legacy orders without allocations: behavior unchanged. |
| D10 | Re-saving a legacy open order after feature activation requires **every item to gain a source** (stock allocation or supplier) — code-level check only, Arabic error. |
| D11 | Item identity: editor sends explicit `Id` per persisted item (verified same-order, `SynchronizeDeliverySteps` pattern); `OrderInventoryAllocation → OrderItem` FK `Restrict`. No heuristic matching. |
| D12 | Pending order edit: stored as full **payload** (items + new allocations) inside the approval request, applied atomically on approval; original order + reservation stay live until decision. |
| D13 | Concurrency: ordered row locking on PostgreSQL for every balance/reserve/release/issue/adjust path (ascending product/warehouse order to avoid deadlocks) + mandatory `Version` check as second line of defense (`"Conflict"`). SQLite tests prove the CAS/conflict logic; a documented manual PostgreSQL lock proof precedes release. |
| D14 | Inventory attachments: **separate** endpoint + folder + metadata entity (`StockReceiptAttachment`: path, original name, optional description, size, mime, uploader, timestamp); PDF/JPG/PNG ≤ 5 MB enforced inside this endpoint only; current attachments behavior untouched. |
| D15 | One migration per implementation slice; the role-seed lives in its own migration. |
| D16 | Delivery for trip-less paths (pickup, shipping company): standalone `StockDeliveryRecord` linked to the order, optionally to a trip or `Shipment` when one exists. |
| D17 | **Sources are chosen per item by the user:** stock, supplier, or both. There is no "stocked product" flag. An item gains 0..n warehouse allocations and 0..1 supplier allocation, with `Σ warehouse + supplier = item quantity` exactly; a fully stock-sourced item carries no supplier and creates no `Supply` row. The editor lets a product enter the order only after its source is chosen. |
| D18 | Issue-order numbering is a **monthly sequence per warehouse** (reaffirmed). `NumberSequenceKind.Warehouse` with `party_id = WarehouseId`; the same migration recreates the two `number_sequences` CHECK constraints — the single declared exception to additive-only, with original definitions restored in `Down`. |
| D19 | **Partial delivery allowed**; D9 is replaced by the per-record cap. |
| D20 | **Counts cover sound and damaged stock in separate columns:** `BookQty`/`CountedQty`/`Delta` and `BookDamagedQty`/`CountedDamagedQty`/`DamagedDelta`; approval posts `Adjustment` and/or `DamagedAdjustment` independently. A line is approvable only when both counted values are entered. |
| D21 | **Approval protection:** `Version` on the request plus `TargetVersion`; the decision is one CAS (`Status='Pending' AND Version=@v`) so a second decision returns `"Conflict"` with no side effects. On target drift the request becomes `Stale` (system note, no side effects) and the requester resubmits; `InventoryCount` carries its own `Version` so two managers cannot post two adjustments. |

Still in force from the earlier design phase: product-level granularity (v1.3 decision 12), no units of measure, supplier choice informational (no purchase orders), monthly per-warehouse issue-order numbering, the **current** keeper is the execution authority (transfer moves pending work automatically), Manager/Higher-Manager manage warehouses and decide approvals but never execute keeper operations (no `IsAdmin` execution bypass — the newer documents supersede the old HM fallback), sealed roles unchanged by users, users/roles managed exclusively by the Higher Manager, opening stock enters only via a documented stock-in operation, no manual balance editing at any point.

---

## 5. Entities

### 5.1 `Warehouse` — المخزن

- `Name` (owned `EntityName`, unique, ≤ 100), `KeeperUserId` (required, exactly one user; unique index prevents one user keeping two warehouses), `IsActive`.
- Behaviors: `Rename`, `AssignKeeper` (validates active user; transfer happens in one transaction — old binding removed, new added), `Deactivate` (guarded: rejected while any positive `StockOnHand`/`DamagedQty` or pending issue orders/counts exist), `Activate`. No delete.
- Keeper transfer **moves pending issue orders and open counts to the new keeper automatically** (authority follows the current binding); transfer UI shows the affected pending count.

### 5.2 `StockBalance` — الرصيد المخزن (derived, rebuildable)

- `WarehouseId` · `ProductId` (unique pair) · `StockOnHand ≥ 0` · `ReservedOnHand ≥ 0` · `DamagedQty ≥ 0` · `Version`.
- Row created lazily on first movement for the pair. Only domain behaviors mutate: `ApplyIn`, `ApplyOut`, `DamagedIn`, `DamagedDisposal`, `Reserve`, `Release`, `Consume` (stock + reserved decrease together, never below zero); each asserts its invariant and is wrapped in the D13 protocol.

### 5.3 `InventoryMovement` — الدفتر (source of truth, immutable)

| Field | Notes |
|---|---|
| `WarehouseId`, `ProductId`, `Quantity > 0` | product-level only |
| `Direction` | `In = 1 / Out = 2` |
| `MovementType` | `OpeningStock` · `StockIn` · `IssueOut` · `ReturnIn` (intact) · `ReturnDamagedIn` · `DamagedDisposal` · `DamagedAdjustment` · `Adjustment` · `Reversal` |
| Source refs (nullable) | `SourceStockReceiptId` · `SourceIssueBatchId` · `SourceReturnId` · `SourceCountId` · `SourceDeliveryRecordId` — real FKs where the target table exists in the same migration |
| `ReversalOfMovementId` | required iff `MovementType = Reversal`; unique filtered index |
| `Status` | `Active / Reversed` (internal `MarkReversed` only) |
| `EffectiveAtUtc` | actual event time; must not be in the future (small clock-margin allowed) |
| `CreatedAtUtc` · `CreatedByUserId` · `Note?` | note mandatory on Reversal/Adjustment |
- Insert-only; `AppDbContext.ProtectHistory` rejects Modified/Deleted like other history.
- Count-time book balance = Σ `Active` movements with `EffectiveAtUtc ≤ count snapshot moment`.

### 5.4 `StockReceipt` + `StockReceiptLine` + `StockReceiptAttachment` — إضافة / رصيد افتتاحي

- Header: `WarehouseId`, `ReceiptType` (`Opening` / `StockIn`), `SupplierId?` (must be linked to every stocked product when set — same rule as the product-supplier link), `ReferenceNote?` (doc reference), actor/timestamps. Immutable after save; mistakes via approved reversal.
- Lines: `ProductId`, `Quantity > 0`; duplicates aggregate deterministically.
- Opening balance is a distinct `ReceiptType` on the same entity (one operation per warehouse when the feature starts).
- Attachments: 0..N, PDF/JPG/PNG ≤ 5 MB, optional per-file description; **separate** endpoint + folder + metadata entity (D14). Files not yet referenced by a saved receipt are cleaned up if the upload is abandoned.

### 5.5 `OrderInventoryAllocation` — تخصيص مصدر بند الطلب

| Field | Notes |
|---|---|
| `OrderId` · `OrderItemId` | FK `Restrict` to the item (D11) — deleting an allocated item is impossible without cancelling its allocation |
| `ProductId` | denormalized for queries |
| `SourceKind` | `Warehouse` (`WarehouseId` set) / `Supplier` (`SupplierId` set) |
| `Qty` | allocated quantity |
| `IssuedQty` | Σ batches against this allocation (stored, validated against batches on write) |
| `Status` | `Active` / `Cancelled` / `Released` / `Consumed`(fully issued+delivered lifecycle handled by order close) |
- Reservations are counted **only from `Active` allocations minus issued batches** (Q2).
- Sources per item (D17): 0..n `Warehouse` rows + at most one `Supplier` row, and `Σ Qty = item quantity` exactly. `OrderItem.SupplierId` is required only when the supplier row exists; a fully stock-sourced item has no supplier and produces no new `Supply` row.
- Creating/updating allocations happens **only** inside the order-save or edit-approval transactions under the D13 protocol.

### 5.6 `StockIssueOrder` + `StockIssueLine` + `StockIssueBatch` — أمر الصرف

- `StockIssueOrder : ModelBase, IWorkRecord`: `OrderId`, `WarehouseId`, `WorkRecordType.StockIssueOrder`, `NumberSequenceKind.Warehouse` (monthly per warehouse; number assigned by `UnitOfWork`), `RequestedByUserId` (sales), `AssignedUserId` (keeper snapshot, audit only), executor/cancel audit fields.
- Status machine: `Pending` → (`PartiallyExecuted` ⇄) → `Completed`; `Pending/PartiallyExecuted → CancelRequested → Cancelled`. No transition out of `Completed/Cancelled`.
  - `CancelRequested`: set by the **current keeper** with a mandatory reason; **blocks new batches and new issue orders against the same allocations** until the Manager decides; approval releases only the un-issued remainder.
  - Order cancel/archive auto-releases (system actor + reason, history row).
- Lines: `AllocationId` (unique per `(issueOrder, allocation)`), `ProductId`, `RequestedQty` ≤ allocation remaining reservation (validated inside the D13 transaction at creation).
- Batches: `IssueOrderLineId`, `Quantity > 0`, actor, `EffectiveAtUtc`, `ShortfallReason` — mandatory when the batch is final and `Σ batches < RequestedQty`; the reason surfaces on the order page for sales. Each batch writes exactly one `IssueOut` movement + `Consume` in one transaction.
- Only the **current keeper** records batches (`Warehouse.KeeperUserId == currentUser.Id` in-handler; no HM bypass, D4/D1 era).

### 5.7 `InventoryApprovalRequest` — طلبات الاعتماد

- `RequestType`: `AllocationEdit` (order edit touching pending issue orders) · `IssueOrderCancel` · `MovementReversal` · `DamagedDisposal`.
- Polymorphic reference: `ReferenceId` typed per `RequestType` (nullable FKs per target, one populated).
- `Payload` (JSON) used by `AllocationEdit` only: the full proposed items + allocations, applied atomically on approval (D12). While pending: the order's existing items/allocations/reservations stay live and untouched; new edits to the same order are blocked until resubmit/decision.
- `Reason` (mandatory on request), `Status` (`Pending/Approved/Rejected/Stale`), `Version`, `TargetVersion` (snapshot of the target row version at request time), `RequestedBy`, `DecidedBy`, `DecidedAtUtc`, `DecisionNote` (mandatory on reject).
- Approval authority: `Inventory.Approve` (Manager/Higher-Manager) + **`Approver ≠ Requester`** except `MovementReversal` and a self-recorded count (D4).
- **Single-decision guard (D21):** the decision is one CAS — `UPDATE … SET Status='Approved'|<rejected>, Version=Version+1, DecidedBy… WHERE Id=@id AND Status='Pending' AND Version=@v`. Zero rows → `"Conflict"`, no side effects. The target is re-validated inside the same transaction: order state + `Version` and no other pending request for the same order (`AllocationEdit`); still `CancelRequested` at `TargetVersion` (`IssueOrderCancel`); still `Active` (`MovementReversal`, unique filtered index as second guard); unchanged `DamagedQty` version (`DamagedDisposal`). Drift → request moves to `Stale` with a system note and no side effects; the requester resubmits.
- `AllocationEdit` approval semantics (owner §6): approves the edit; the handler processes **only affected items** — issued quantities are preserved (shortfall beyond new demand becomes a «مرتجع مستحق» state on the return queue), the un-issued remainder is released, then the new allocations are reserved under D13; if the new reservation fails, the edit stays pending with the old reservation intact and sales revises. If the reservation succeeded, new issue orders are **not** auto-created — sales creates them manually.

### 5.8 `InventoryCount` + `InventoryCountLine` — الجرد

- `WarehouseId`, scope `Full / Products`, `Status`: `Open` → `UnderReview` → (`Approved` / `Cancelled`), `Version` (D21); one open count per (warehouse, product) — filtered unique index + handler check.
- Lines (D20): `ProductId`, `BookQty`, `BookDamagedQty` (snapshots from movements at line save, by `EffectiveAtUtc`), `CountedQty?`, `CountedDamagedQty?`, `Delta = CountedQty − BookQty`, `DamagedDelta = CountedDamagedQty − BookDamagedQty`, review fields: `LateMovementDecision` (`IncludedInCount / Recounted`), `ReviewNote`. A line is approvable only when both counted values are entered.
- Saving counts never changes balance. Movements remain allowed while a count is open (snapshot is logical); on approval, if a movement with `EffectiveAtUtc` earlier than the snapshot was recorded after the count was saved, approval pauses for Manager review: «شملها العدّ» → recompute both book balances and both deltas; «لم يشملها» → recount (line returns to open). Decision recorded once per line.
- Manager approves, **including a count he recorded himself** (D4 exception); the approval is a `Version` CAS on the count (D21). It creates up to two movements per line — `Adjustment` for `Delta` and `DamagedAdjustment` for `DamagedDelta`, each only if non-zero — applied to the **current** balance. Blocked if the sound result would drop `StockOnHand` below `ReservedOnHand` or the damaged result would go below zero.
- Open counts transfer to the new keeper on keeper change.

### 5.9 `InventoryReturn` + `InventoryReturnLine` — المرتجعات والتالف

- References the originating order (+ issue order/batch when known); recorded by the **current keeper**.
- Line caps: `IntactQty + DamagedQty ≤ issued-and-not-yet-returned qty` for that order item.
- Intact → `ReturnIn` movement (+ balance in). Damaged → `ReturnDamagedIn` movement + `DamagedIn` on the balance (separate column, never available). «مرتجع مستحق» entries (from a shrinking approved edit) appear in the keeper's queue and enter the balance only after physical sorting.
- Damaged disposal: keeper request with mandatory reason → `DamagedDisposal` approval type → Manager approval → movement + `DamagedDisposal` on balance (≤ `DamagedQty`).

### 5.10 `StockDeliveryRecord` — التسليم المخزني

- `OrderId`, `OrderItemId`/`AllocationId`, `ProductId`, `Quantity ≤ Issued − Delivered` for that allocation (D19; re-validated under D13 locks), `TripId?` / `ShipmentId?` (optional link), `RecordedByUserId` (sales), `EffectiveAtUtc`.
- Delivered/remaining per stocked item = aggregation over records (computed, Q6). Recorded from the order workflow; no behavior change for non-stocked items (they keep the current trip path). Partial delivery is expected: un-issued stock lines simply stay undelivered.

### 5.11 `OrderItem` / `Order` changes

- No stock columns on `OrderItem` (allocations own the sources); `SupplierId` stays.
- Editor sends explicit `Id` for persisted items; handler validates same-order ownership; unallocated-item deletion is blocked by the allocation FK (D11).
- Lifecycle: **no order-level stock gate** (D9/D19) — transitions are untouched by stock; the stock cap lives on each delivery record (`Quantity ≤ Issued − Delivered`). Reservation release for the un-issued remainder on `Delivered/Cancelled/Archived` (system history row). Legacy orders without allocations are untouched.

---

## 6. Flows

### 6.1 Stock-in / opening balance
Keeper (`Inventory.Write` + binding + `Products.Read`) → warehouse, type, product lines, optional supplier/reference, optional attachments → one transaction: receipt + per-line `StockIn`/`OpeningStock` movement + `ApplyIn`. Deactivation guard added to warehouse update.

### 6.2 Allocation at order editing
Sales adds a product → chooses its source before it joins the item list: a warehouse (one or more), a supplier, or both (D17). Availability per warehouse (actual/reserved/available, descending) is shown company-wide (`Inventory.Read`) for warehouse-sourced items; with no stock, supplier-only. Draft saves allocate nothing. Re-saving an open legacy order forces every item to gain a source (D10).

### 6.3 Final save
`Create/Update` handler: inside one transaction with ordered row locks — validate every warehouse allocation against live availability (Version check as second defense), create/refresh `StockReservation` accounting via allocations, save order + allocations together, or fail with a clear Arabic message («الكمية لم تعد متاحة…أعد التوزيع»). Drafts and legacy history never touch stock.

### 6.4 Issue orders
Sales picks stocked items/quantities (≤ remaining reservation per allocation) → one order per warehouse, manual, multiple stages allowed → keeper sees them at login (his warehouse only). Batches deduct `Actual` and leave the remainder reserved; shortfall reason required and visible to sales.

### 6.5 Delivery
No order-level stock gate (D19): the sales user records delivered quantities per stocked item, each capped by `Issued − Delivered`, from the trip path or trip-less (D16). Un-issued stock stays undelivered; the rest of the order proceeds. Delivered never re-deducts the balance.

### 6.6 Edit/cancel with reservations
- Edit touching a pending issue order → Manager approval (D12 payload flow, §5.7).
- Order cancelled → un-issued released automatically; issued stays in the ledger; physical returns go through the returns flow.
- Keeper cancel request → Manager approval → release un-issued only.

### 6.7 Returns / damaged / counts / reversals
As §5.9 and §5.8. Reversal (D6/D4): keeper request with mandatory reason → approval (approver ≠ requester **except** reversal) → single transaction: pair movement + `MarkReversed` + balance re-derivation.

---

## 7. Permissions

### 7.1 Features & seed (locked)

`PermissionFeature` gains `Warehouses` (المخازن) and `Inventory` (المخزون) after `PaymentMethods`. `SealedRoleSeedSql.V2` (own migration, additive, idempotent by role_key + feature):

| Role | Warehouses | Inventory |
|---|---|---|
| مبيعات `sales` | — | Read |
| مشتريات `purchasing` | — | Read |
| مدير `manager` | Write | Approve |
| أمين مخزن `storekeeper` (new sealed, 3 cells) | Read | Write |
| مدير أعلى | bypass | bypass |

Storekeeper preset: `Warehouses=Read`, `Products=Read`, `Inventory=Write` — everything else None (D1). New cells added by V2: sales `Inventory=Read` (1) · purchasing `Inventory=Read` (1) · manager `Warehouses=Write` + `Inventory=Approve` (2) · storekeeper (3) = **7 new cells**. Expected totals after V2: **5 sealed roles / 38 cells**. Seed tests assert the exact counts; drift is fixed in the same task.

### 7.2 Action matrix

| Action | Feature gate | Row/data gate (handler) |
|---|---|---|
| Warehouses list/details | `Warehouses.Read` | — |
| Warehouse create/edit/transfer keeper/deactivate | `Warehouses.Write` | Guards (positive stock, pending work); one-keeper uniqueness |
| Keeper candidates | `Warehouses.Write` | Minimal fields (id, name, active, has-inventory-write) |
| Balances / ledger / issue orders / counts / returns (read) | `Inventory.Read` | Company-wide; storekeeper's inventory screens additionally scope to his warehouse |
| Stock-in / opening / batches / returns recording / reversal+cancel requests | `Inventory.Write` | **Current keeper binding**: `warehouse.KeeperUserId == currentUser.Id`; no HM bypass |
| Approvals (edit/cancel/reversal/disposal/counts) | `Inventory.Approve` | `Approver ≠ Requester` except reversal and a self-recorded count; Manager never executes keeper ops |
| Order allocation screen + final save | `Orders.Write` | Availability needs `Inventory.Read`; without it stock sourcing is disabled (supplier-only fallback, Arabic hint) |
| Delivery records | `Orders.Write` | `Quantity ≤ Issued − Delivered` per allocation; no order-level gate |

### 7.3 Keeper binding rule (code-level, every mutating handler)

```
allowed = warehouse.KeeperUserId == currentUser.Id
```
`Result.Failure(..., "Forbidden")` — never thrown, never DB-enforced, never UI-only. Applies to stock-in, batches, returns, reversal requests, count recording, cancel requests. `Inventory.Approve` does **not** bypass it.

---

## 8. Concurrency protocol (D13)

- **Scope:** every transaction writing `StockBalance` (reserve on final save, release on cancel/close, consume per batch, stock-in, returns, adjustment, reversal application, delivery-cap validation) opens it by locking the affected `(product, warehouse)` balance rows in ascending (product, warehouse) order — PostgreSQL `SELECT … FOR UPDATE` via raw SQL where EF lacks it.
- **Canonical lock order** (every path; a row is never locked before a row that precedes it in this list):

```text
number_sequences → orders → stock_issue_orders/lines → inventory_approval_requests
→ inventory_counts → stock_balances (ProductId, WarehouseId asc) → insert movement
```

  This keeps the final-save path (`orders → balances`) and the approval paths (`approval → issue order → balances`) compatible and removes the mixed-order deadlock shape. `number_sequences` is only taken when creating an issue order; movements are inserted last and take no lock.
- **Second defense:** `StockBalance.Version` checked/incremented in the same transaction; stale save → `"Conflict"`. Approval and count decisions use the same CAS discipline on their own `Version` (D21).
- **Allocation accumulation** (Q4) is checked under the same locks, not by unique index alone.
- **Testing:** SQLite tests prove the CAS logic (second over-reserve fails with `"Conflict"`, no partial save). Real locking is proven by a documented manual PostgreSQL script before release (repo has no PG test harness by design).

---

## 9. Screens (Arabic UI)

1. `/warehouses` — list + editor (name, keeper candidates, activity, pending-work hint from real data) · `Warehouses.Read/Write`.
2. `/inventory` — balances (actual/reserved/available/damaged) per warehouse + aggregate view + ledger drill-down; storekeeper scoped to his warehouse · `Inventory.Read`.
3. `/inventory/stock-in` — keeper form (type, lines, supplier?, reference?, attachments) · `Inventory.Write` + `Products.Read`.
4. `/inventory/issue-orders` — list (sales-created, and the keeper's pending queue), details with lines/batches/reasons, create dialog (sales), batch recording + cancel request (keeper) · `Inventory.Read`.
5. `/inventory/approvals` — Manager queue: allocation edits (payload diff view), cancel requests, reversals, disposals, count approvals · `Inventory.Approve`.
6. `/inventory/counts` — keeper create/count/save/complete; Manager review & approve · per matrix.
7. `/inventory/returns` — keeper recording + «مرتجع مستحق» queue + disposal request.
8. Order editor `ProductsStep` — availability columns + allocation inputs; order details/workflow — issue/delivery status per stocked item, `RecordStockDelivery` dialog.
9. Nav group «المخزون» — items per the matrix; no dead links; hidden when nothing is allowed.

All pages: `PageScaffold` + `TableFooter` (exceptions per AGENTS.md), `.oh-btn`, one primary CTA, Arabic RTL, `CultureInfo.InvariantCulture` numbers, no new JavaScript, status tones via the existing four-hue system.

---

## 10. Risks & mitigations

| Risk | Mitigation |
|---|---|
| Double-subtract issued from availability | Q2 equation + batch-driven `Reserved` |
| Two savers race the last units | D13 locks + Version CAS + `"Conflict"` message |
| Item identity lost on update | D11 explicit ids + `Restrict` FK |
| Reservation leak on close | Release on Delivered/Cancelled/Archived + system history |
| Cached balance drift | Ledger is truth; rebuild tool (HM); Version-revalidated writes |
| Late-dated movement breaks count approval | `EffectiveAtUtc` + review decision flow (§5.8) |
| Keeper transfer strands work | Authority follows current binding; open counts transfer too |
| Attachments on shared endpoint | Separate endpoint/folder/metadata (D14) — legacy untouched |
| Self-approval | D4 rule + tests (reversal and self-recorded count exceptions asserted) |
| Partial delivery blocked by an over-strict gate | D19: cap on the delivery record, never on the order transition |
| Behavior change to existing orders | D10/D11 owner-approved; D19 allows partial delivery instead of blocking it; legacy rows without allocations behave exactly as today |
| SQLite can't prove locking | CAS-proofed tests + documented PG proof before release |

---

## 11. Compatibility contracts

- `Result`/`Result<T>` everywhere; codes: `"Unauthorized"`, `"Forbidden"`, `"NotFound"`, `"Conflict"`, `"InvalidOperation"`, `"Persistence"`. Inventory-specific `"Conflict"` cases: stale balance (Q3/Q6 race), second approval decision, drifted approval target, second count approval. Arabic messages are required for: unavailable quantity («الكمية لم تعد متاحة في {المخزن}؛ أعد التوزيع.»), missing source on re-save («يجب اختيار مصدر لكل بند: مخزون أو مورد»), delivery over issued («لا يمكن تسليم كمية مخزنية أكبر من المصروف غير المسلّم لهذا البند.»), and a decided/stale approval («تم البت في هذا الطلب مسبقاً» / «تغيّرت البيانات؛ أعد الإرسال»).
- All migrations additive; new columns nullable; `SealedRoleSeedSql.V1` frozen; V2 idempotent; per-slice migrations descending timestamps after the current latest. **Declared exception (D18):** the issue-order migration recreates `ck_number_sequences_kind`/`ck_number_sequences_scope`; no rows touched, `Down` restores the originals, regression test covers the legacy kinds.
- Tests: SQLite in-memory, snake_case + `EnsureCreated`, identical options to existing suites; `InternalsVisibleTo` intact.
- Legacy orders: load, display, edit-gating and delivery flows unchanged unless allocations exist; no retroactive movements/reservations/notifications.
- No JavaScript, no runtime font fetching, no `Supply`/pricing/documents changes, existing numbering untouched.

---

## 12. Implementation slices (one migration each; seed in its own)

| # | Slice | Migration |
|---|---|---|
| S1 | Permissions (`Warehouses`/`Inventory`), `RoleFeatureCatalog`, nav group, `SealedRolePresets` + `SealedRoleSeedSql.V2` (own migration), keeper-candidate query, drift-test updates | `AddInventoryPermissions` (seed) |
| S2 | `Warehouse` domain/persistence/handlers/screens | `AddWarehouses` |
| S3 | Ledger + `StockBalance` (3 columns) + movements + protect-history + repository + CAS reserve/release/consume + rebuild data path | `AddInventoryLedger` |
| S4 | `StockReceipt` (+lines+attachments) + attachment endpoint/folder/metadata + stock-in handlers/screens + deactivation guard | `AddStockReceipts` |
| S5 | Order item identity (explicit ids + FK), `OrderInventoryAllocation` (D17 sources), availability read, editor UI, atomic reservation on final save, D10 lifecycle rule | `AddOrderAllocations` |
| S6 | `StockIssueOrder` (+lines+batches), `IWorkRecord` join, `WorkRecordType` + `NumberSequenceKind.Warehouse`, **CHECK-constraint recreation (D18)**, execute-batch + cancel-request handlers/screens | `AddStockIssueOrders` |
| S7 | `InventoryApprovalRequest` (+ `Version`/`TargetVersion`, D21) + edit-approval payload flow + issue-cancel approval + reversal flow | `AddInventoryApprovals` |
| S8 | `StockDeliveryRecord` + workflow dialog + returns/damaged/disposal + counts with sound/damaged columns (D20) + count `Version` guard + rebuild tool + Phase acceptance | `AddDeliveryReturnsCounts` |

Each slice: Domain → Application → Infrastructure → Web → restore/build Release/test Release --no-build → manual browser pass. Slice order is strictly sequential.

---

## 13. Test plan (per slice; summary)

1. **S1:** 16 features; seed counts (exact, including name-collision case); storekeeper sealed/reserved; candidate query minimal fields + gating.
2. **S3:** movement immutability (`ProtectHistory`), invariant enforcement, CAS over-reserve conflict, ledger ordering by `EffectiveAtUtc`, no negative columns.
3. **S5:** final-save atomicity (all-or-nothing allocations), race → `"Conflict"`, draft isolation, stock-only/supplier-only/mixed sources (D17), legacy order edit forces sources (D10), item-id validation + FK behavior, release on close, and the **absence** of an order-level delivery gate (D19).
4. **S6:** numbering per warehouse/month + legacy-kind CHECK regression (D18), batch accumulation (Q4/Q5), shortfall reason mandatory on a final short batch, `CancelRequested` blocks, auto-release on order cancel/archive.
5. **S7:** approval matrix (types × requester × approver, self-recorded count exception), reversal exception (D4), single-decision CAS + `"Conflict"` on the second decision (D21), `Stale` on target drift, payload application atomicity, failed reservation keeps old state.
6. **S8:** delivered ≤ issued − delivered (trip + trip-less, D19), returns caps (Q7), damaged column lifecycle (Q8), count snapshot math on both columns (D20), late-movement review on both, dual adjustment, count CAS against double approval (D21), rebuild tool (HM-gated).
7. **Cross:** binding rule — non-keeper with `Inventory.Write` rejected; Manager with `Approve` rejected for execution; keeper-scoped reads for storekeeper UI; seed drift; migration applies to a populated copy + rollback rehearsed before release (documented).

---

*v2.1 supersedes v2.0/v1.3 and v1.0 registry assumptions (auto issue orders, single warehouse per item, full-qty execution, deferred counts/returns). Owner decisions 1–21 are closed; implementation starts with S1 only after the owner starts the task registry.*
