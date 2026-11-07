# Delivery Cancellation and Reassignment — Implementation Specification

## 1. Purpose and implementation status

This document specifies the approved behavior for cancelling an active delivery assignment and routing the order to a valid alternative. It is intended for AI agents implementing the feature in this repository.

**Planning artifact only.** Do not treat this file as authorization to implement the feature. The user explicitly requested planning and has not authorized code changes. When implementation is authorized, follow this document and the repository `AGENTS.md`.

**Design gate RESOLVED (2026-11-07):** the owner answered the final persistence/behavior questions; §11 now carries the locked implementation decisions instead of open items. Phase A has only verification work left.

**Code review round RESOLVED (2026-11-07):** the draft decisions were audited line-by-line against the actual domain/read-model/UI code; eight blockers were confirmed and folded into **§12 (verified domain constraints)**, which supersedes any contradicting wording in earlier sections. Three owner product answers locked: party validation is **exists-only** (no `IsActive` flag), the permission gate is **Orders/Write**, and cancelled steps render **inline at their original rank position**.

**Second review round RESOLVED (2026-11-07):** the amended file was re-audited end-to-end against the code. The first-round blockers are resolved, but one persistence claim was factually wrong and several UI/consistency gaps remained. **§12.1–§12.13 carry the corrected design and are authoritative**: targeted single-row tombstone staging (no full rank sweep), a complete direct route matrix, a rank-based “superseded” marker consumed by readiness/sequencing/completion/counters, one pickup confirmation path, a steps section that survives a conversion to pickup, filtered copyable summaries, and a defined movement-history schema. Where earlier text still contradicts these sections, §12 wins.

The feature must preserve existing order, delivery-step, work-record, history, numbering, and financial data. Do not delete or rewrite existing persisted delivery history.

## 2. Approved product decisions

1. Reassignment is available only after execution has started. It is not a normal order-editor operation.
2. Starting cancellation opens a dialog. Cancellation cannot be confirmed until the user has entered a non-empty reason and selected a valid alternative.
3. Supported alternatives are:
   - Assign to another deliveryman.
   - Assign to a shipping carrier.
   - Customer pickup from the branch/headquarters (`DeliveryMethod.Pickup`, described in Arabic as `استلام من المقر`).
4. For a delivery chain, a replacement step is inserted at the cancelled step's execution position. The old step remains cancelled and historically intact; do not mutate its handler to the new party.
5. For a direct deliveryman assignment, cancel the existing deliveryman work record (only while it is `Pending`; a `Failed` record stays untouched — §12.4) and create the work record appropriate to the chosen new route. Do not update the executing order through the ordinary editor.
6. For branch pickup, move the order to the existing pickup stage (`DeliveryMethod.Pickup` → `ReadyForPickup`) and complete it only when receipt is explicitly confirmed. Do **not** create a pickup work record, numbering kind, or table (§12.11); reassignment alone must not complete the order.
7. Every movement is a distinct historical record. Cancellation and new assignment are separately identifiable: the movement row captures the old route/party snapshot and reason, while the cancelled step/work record keep their own status-history rows and the replacement starts with a Pending step (chain — its work record is created when it starts) or a Pending record (direct). They are linked by the same order version and operation timestamp, and **only one order-level movement row is written per operation** (§12.7.4).
8. The cancelled assignment must not create future financial entitlement as a completed delivery. Keep its cancellation reason and party identity for audit and later financial review. Never erase pre-existing financial records or infer that already-recorded fees should be deleted.
9. The new assignment operation is available only after execution has begun; enforce this in the domain/application layer, not only by hiding UI controls.

## 3. Current implementation facts and constraints

- `Order.Lifecycle.cs` owns order/step transitions, reason validation, order version changes, and creation/closure of work records.
- `OrderDeliveryStep` stores one current method/party and status. Its planning setters are intentionally guarded after the order stops being editable.
- `DeliveryStepStatusHistory` currently stores status transition, reason, actor/time, source and order version. It does not store old/new delivery method or party identity.
- `DeliveryTrip` and `Shipment` are immutable party-attributed work records. Numbering captures a `NumberingPartyId` and refuses transfer to another party. Existing records are linked either directly to the order or to one delivery step.
- The lifecycle repository loads order-level and step-level shipments/trips for mutation. It does not currently load deliveryman/carrier reference entities for validating reassignment choices.
- The workflow read model calculates available step transitions and blockers. Its blocker text and `AllowedStatuses` currently reflect existing strict completion-only sequencing.
- Workflow UI is `OrderWorkflow.razor`; order details also display workflow information in `OrderDetials.razor`. History is rendered by `OrderHistoryPanel.razor` and read through `OrderWorkflowRead.cs`.
- `UpdateOrderHandler` is for editable orders and synchronizes/reorders the delivery-step collection. It must not be used for post-start reassignment. Existing unique `(OrderId, StepOrder)` index requires careful replacement ordering.
- Direct work records are created when order status becomes `OutForDelivery` or `Shipped`; a direct method change must explicitly close the old record and create the appropriate new route records.
- Current enums already include `DeliveryMethod.Pickup`, `WorkRecordStatus.Cancelled`, and `DeliveryStatus.Cancelled`. Reuse semantics only where they match this specification. Do not change enum values or reinterpret legacy rows as part of a migration.
- `DeliverOrderDirectlyCommand` is an administrative/direct-close flow for a delivery chain. It is not the reassignment flow and must retain its existing meaning.

## 4. Domain terminology and rules

### 4.1 Cancellation versus failure

- **Failed** means the current attempt could not complete and may be retried with the same assignment. It continues to block the next chain step.
- **Cancelled** means the current assignment/step is intentionally ended. Its active work record is closed as cancelled with the cancellation reason. It is not equivalent to completion and must not be counted as completed for delivery/financial purposes.
- For chain progression, a cancelled step may be bypassed only when a valid replacement route was selected and successfully recorded as part of the confirmed operation. A cancelled step with no valid replacement must not silently unlock the rest of the chain.

### 4.2 Reassignment outcomes

- Chain + deliveryman/carrier: old step remains cancelled; create a new step for the selected party in the same execution position; new step starts pending; later steps remain blocked until replacement completes or is itself cancelled with another valid alternative. This includes same-method party switches (`InProgress → another deliveryman`, `Failed → another carrier`) — see §12.3.
- Chain + pickup: old step remains cancelled and tombstoned; the order-level route becomes `DeliveryMethod.Pickup` and the order moves to `ReadyForPickup` in the same movement. **No replacement step, no pickup work record, no new numbering kind, and no pickup table** (§12.11); pickup has an explicit confirmation action that completes the order.
- Direct deliveryman + deliveryman/carrier: close the old direct work record per §12.4; persist the route-change/cancellation history; clear the opposite party ID and update the order's active method/party through a dedicated lifecycle behavior; create a new direct work record for the new party/method. Same-method switches to another party follow the same rule and keep the status unchanged (§12.3). Keep the old record attached to the order with its original party attribution and number.
- Direct deliveryman + pickup: close/cancel the old direct work record per §12.4, clear both party IDs, change the active delivery method to `Pickup`, and move the order to `ReadyForPickup` in the same movement. **No pickup record is created**; the order stays pickup-pending until receipt confirmation, then transitions to `Delivered` through the single confirmation path (§12.11).

## 5. Audit and data preservation requirements

1. Never delete an old `OrderDeliveryStep`, `DeliveryTrip`, `Shipment`, or history row as part of cancellation/reassignment.
2. Never reuse an old trip/shipment as the replacement party's record. Never change its `NumberingPartyId`, work-record number, or party attribution.
3. Capture these facts in immutable history for each cancellation/reassignment movement:
   - Order and affected step/work-record identity.
   - Previous route type and previous party ID/name snapshot where available.
   - Selected next route type and new party ID/name snapshot where applicable.
   - Required cancellation reason.
   - Actor ID/name and UTC timestamp.
   - Order version and a stable event/source discriminator.
4. History must remain readable even if a party is later renamed or removed. Prefer party ID plus a name snapshot for audit display; do not rely only on joining the current party row.
5. Represent each movement as one order-level movement row plus its consequential step/work-record status rows (§12.7.4). Cancellation and assignment are confirmed together in one UI action, persisted in one database transaction; failed persistence must not leave only one half of the confirmed movement.
6. A user cannot confirm cancellation without a valid alternative. Therefore no standalone cancellation is committed from this dialog. If product later requires cancellation without reassignment, it needs a separate explicit business operation and revised requirements.
7. Preserve legacy migration history/source and disclaimer behavior. New history must not be mislabeled as legacy or direct-delivery override.
8. Financial consumers must be able to distinguish cancelled work from completed work. Cancellation must not create completed-work compensation. Do not erase or retroactively modify existing finance rows; any fee reversal policy is outside this feature unless separately specified.

## 6. Required architecture

Follow the repository's vertical-slice CQRS and result-pattern conventions:

- Domain: add explicit lifecycle behavior for cancellation/reassignment and pickup confirmation. Validate current order/step state, active execution, required reason, valid route/party combination, and transition eligibility.
- Application: add dedicated request/contract(s) with order ID, expected version, target step/work context, selected method, selected party ID, and reason. Return `Result<OrderOperationReceipt>` with conflict handling.
- Infrastructure: load required aggregate work records and validate selected party existence/eligibility. Persist histories and route changes in the same unit of work. Add EF configuration and a forward-only migration only if the chosen history model requires schema changes.
- Web: add a dialog launched from the workflow page (and direct-order workflow surface where appropriate), never from ordinary order editing. Require reason and valid alternative; load party options through existing CQRS queries. Protect async submit against double-click and stale order versions.
- Read model: expose enough state to show old cancelled assignment, replacement assignment, party snapshots, and pickup pending/confirmed status. Render the history in the order's history view.

Do not call repositories directly from Razor. Do not throw for expected invalid state. Keep authorization on Orders/Write and rely on domain validation as the final guard.

## 7. Detailed implementation sequence

### Phase A — Inspect and decide the persistence model

1. Trace all readers of `OrderStatusHistory`, `DeliveryStepStatusHistory`, `WorkRecordStatusHistory`, `Shipment`, `DeliveryTrip`, order summaries, and financial reports.
2. Trace whether existing work-record histories could represent new-route assignment events — verified answer: they cannot (pure status rows). Per **§11.6**, keep `DeliveryStepStatusHistory`/`WorkRecordStatusHistory` as pure status rows and store route movements in the new dedicated movement history table.
3. ~~Prefer a dedicated immutable delivery-assignment movement history~~ — **Decided (§11.6):** add a dedicated movement history; `OrderHistorySource` gains exactly two new values (`DeliveryRouteReassigned`, `PickupConfirmed`); per-step/per-work-record status rows written in the same movement keep existing sources (genuine status changes); correlate by identical `OrderVersion` + `ChangedAtUtc`, no new schema-heavy correlation column.
4. ~~Decide the durable representation of pickup execution~~ — **Decided (§11.4):** REUSE the order-level Pickup flow (`DeliveryMethod.Pickup` → `ReadyForPickup` → `Delivered`). No new work-record type, no numbering kind, no pickup table.
5. ~~Design replacement-step ordering~~ — **Decided (§11.2, corrected §12.1):** tombstone rewrite with a **targeted single-row** staging operation. Inside the confirmed movement's transaction the cancelled old step's `StepOrder` is rewritten to `-<step.Id>` (collision-free under the unique `(OrderId, StepOrder)` index), the replacement takes the freed rank, and the original rank is copied into the movement history row. Do **not** call `PrepareDeliveryStepReorderingAsync` here: that method is the editor's full-renumber sweep and leaves tracked current values at their old positive ranks. Use a dedicated repository operation that bulk-updates the one affected row **and** sets both `OriginalValue` and `CurrentValue` to `-<step.Id>`. One tombstone slot per cancellation — repeated reassignment repeats the same cancel + replace operation, no pre-reserved spare ranks.
6. Check current migrations and SQLite test model compatibility. Any new required FK/column must work with snake_case and SQLite constraints.

### Phase B — Implement domain behavior

1. Add explicit domain operations for:
   - Reassigning a delivery-chain step.
   - Reassigning a direct deliveryman order.
   - Confirming branch pickup.
2. Validate that the order is active, not archived/final, in an execution state, and that the affected assignment has actually started. Prevent use on editable-but-not-started orders.
   - **New requirement (§11.5):** add a dedicated `internal` lifecycle behavior on `Order` to change the active route (`DeliveryMethod`/`DeliverymanId`/`ShippingCarrierId`) after execution started — these fields are currently only reachable via `UpdateHeader` behind `EnsureEditable()`, so the reassignment flow must use this new validated mutator (Version bump included) and ordinary editing must stay locked.
3. Require a nonblank reason within the existing 200-character reason limit.
4. Validate the alternative method and party pairing. Deliveryman requires exactly one valid deliveryman ID; shipping company requires exactly one valid carrier ID; pickup requires neither.
5. Close the old work record: only if it is `Pending` (transitioning any other status throws — §12.4); a `Failed` record stays `Failed`, untouched, with the movement row narrating the route change. Preserve the record's number and party identity; record the cancellation reason on its status history when it is actually closed.
6. Create a distinct replacement step (chain, pending) or work record (direct, pending), at the same logical chain position or as the new direct order assignment. Never mutate the old step's party/method.
   - **Chain variant (§11.2, §12.1):** the old step is tombstoned by the targeted single-row repository operation — the row is bulk-updated to `-<step.Id>` **and** its tracked current value is set to the same value in the same transaction (setters are dead post-start). The replacement step, constructed and valued *before* `AttachTo`, occupies the freed old rank and is appended to the private step collection from inside `Order` (never via `EnsureEditable`-guarded `AddDeliveryStep`). No other step rank changes, so no other rank writes are required. The original rank is captured in the movement history row. The replacement starts `Pending` and gets **no** work record or number until it transitions to `InProgress`, exactly as today (§4.2) — only the cancelled step's `Pending` record is closed.
   - **Direct variant (§12.3):** resolve the route change through the single matrix resolver — close the old record (only if `Pending`, §12.4), clear the opposite party ID, mutate method/party via the internal mutator, then create the new record for the selected party. Status target per the matrix.
   - **Pickup variant (§11.4, §12.11):** no replacement step and no pickup record — the movement clears both party IDs, sets `DeliveryMethod.Pickup`, moves the order to `ReadyForPickup` in the same version bump, and auto-cancels remaining non-completed chain steps. Their step-history rows keep the existing `ManualAction` source; the movement row with the new source is the authoritative reassignment event (never `OrderCancellation`, never a duplicated `DeliveryRouteReassigned` status row — §12.7.4).
7. Increment order version once per confirmed operation; all related history rows use that version and operation timestamp.
8. **Replace the completion predicate (§11.1, §12.9):** `TransitionStep`'s current `_deliverySteps.All(step => step.Status == DeliveryStatus.Completed)` delivery check is retired. New active-position gate: ignoring **superseded** steps (`StepOrder < 0`), delivery occurs when the remaining steps — ordered by stored rank — are all Completed, quantified over at least one step. Legacy orders without tombstones keep identical behavior. **Also update `AllowedStepStatuses` (§11.3):** compare against the immediate previous **non-superseded** step and require it to be Completed; otherwise a tombstoned step (rank below everything, status ≠ Completed) blocks every successor. A cancelled step that has no replacement (positive rank) still blocks, per §4.1; genuine pending/failed/in-progress steps and a pending replacement still block successors.
9. Ensure an order becomes `Delivered` only through the normal last-stage completion or confirmed pickup; cancellation alone never delivers it.

### Phase C — Implement application commands and persistence

1. Add dedicated commands rather than extending `UpdateOrderCommand`.
2. Check authenticated actor and `ExpectedVersion`; translate stale versions to error code `Conflict`.
3. Validate referenced deliveryman/carrier existence (exists-only — §11.9, no active flag) using application contracts/readers.
4. Load order, delivery steps, work records, and relevant navigations through a dedicated lifecycle repository method/query; include the current party rows needed for name snapshots (the lifecycle query does not load them today).
5. Persist route change, old work-record cancellation, new work/step records, and all movement history in a single unit-of-work transaction. The history remains multiple distinct rows: **one movement row (new source)** carrying the full old/new route, party snapshots and original rank + the genuine status rows with existing sources, all sharing one `OrderVersion` and one `AtUtc`. Do **not** also write a duplicate `OrderStatusHistory` row for the same movement (§12.7.4). The targeted tombstone bulk update, the domain movement and the save must share the same transaction (§12.1).
6. Catch persistence conflicts/failures using established `Result` patterns (`PersistenceConflictException` → `Conflict`, `PersistenceException` → generic failure). On failure, guarantee no partial cancellation/reassignment is visible.
7. Add a dedicated pickup confirmation behavior with expected-version protection and history, and route the existing `ReadyForPickup + Pickup + Delivered` entry point through it (§12.11).

### Phase D — Build UI flow

1. From an active, started direct assignment or active chain step, present an action such as “إلغاء التكليف وإعادة التوجيه”. Do not expose it for unstarted plan rows, completed rows, archived/final orders, or already superseded assignments. Gate it from read-model flags (`CanReassignDirectly`, per-step `CanReassign`, available target methods) exactly as defined in §12.13; the domain re-validates.
2. Dialog fields:
   - Required cancellation reason.
   - Required alternative route: another deliveryman, shipping company, or customer pickup from headquarters.
   - Required party selector for deliveryman/carrier routes; no party selector for pickup.
   - Clear summary of old assignment and selected new route.
3. Disable confirmation until reason and route/party are valid, the selected party differs from the current party, and the affected assignment is started (direct status per §12.13; chain step `InProgress`/`Failed`). Validate again server-side.
4. Submit exactly once, guard busy state, use the displayed order version, and handle `Conflict` by reloading and asking the user to review current state.
5. After a pickup route change, surface the existing pickup confirmation action (`ReadyForPickup → Delivered`). Do not show the order as delivered until it succeeds; it goes through the single path of §12.11 and is not a new dialog.
6. Keep Arabic UI, RTL layout, logical Tailwind spacing, invariant digits where numeric values appear, and existing status tone conventions. Do not add JavaScript.

### Phase E — Read models and history presentation

1. Update `GetOrderWorkflowQuery` with replacement relationships, the currently active route/party, pickup readiness, per-step `IsSuperseded`/`EffectiveRank`, and the action-availability flags used by the UI (`CanReassignDirectly`, per-step `CanReassign`, available target methods).
2. Update blocker text to distinguish failed / cancelled-without-replacement (blocking) / cancelled-with-replacement / completed. Keep legacy-history disclaimer intact.
3. Ensure the order's general history feed merges movement rows under the §12.7 contract (source-qualified entry keys, shared-projection union with a defined total order and correct `TotalCount`, source-based tone/label/display text, movement rows included in the «آخر حدث» and `LatestReason` computation in `OrderWorkflowRead`) so cancellation, reassignment, and pickup confirmation are discoverable in one audit feed (§11.7). Step panels keep per-step events. Legacy disclaimer handling unchanged.
4. Render the delivery-steps section whenever the order **has** steps, regardless of the current route (§12.6). `OrderWorkflow.razor` currently gates it on `Method == DeliveryChain`; after a chain→pickup conversion that would hide the cancelled steps and every per-step history panel. The stage strip and instruction follow the current method; the steps section carries the aborted-chain audit.
5. Sort steps by `EffectiveRank` with a deterministic tie-breaker (effective rank, then movement/step creation order), render it with invariant digits, and never expose the negative stored rank; cancelled steps render inline at their old position (owner decision) with «ملغاة/مُعاد توجيهها» styling. Direct-delivery dialog outcome lists exclude superseded steps and use `EffectiveRank`; a non-superseded cancelled step stays listed (existing contract, §12.2/§12.9).
6. Counters and banners use the non-superseded, non-cancelled step set (§12.9) in both `OrderWorkflow.razor` and `OrderDetials.razor`: completed/total KPI, progress bar, and the “delivered with incomplete steps” notice.
7. Copyable party summaries (`OrderSummaryRead` + `OrderSummaryTextBuilder`) exclude cancelled/superseded work records, resolve the party name from the record's own `NumberingPartyId`, and never print a negative step number (§12.10).
8. Ensure step history can show the cancelled old step even after a replacement becomes active. Do not collapse old and new step IDs into one row.
9. Party names in old history and movement rows use name snapshots so later renames do not rewrite audit meaning.

### Phase F — Migration and compatibility

1. Add a forward-only migration with the repository's timestamp/name convention for the movement-history table defined in §12.12 (indexes `(OrderId, OrderVersion, Id)` and `(OrderId, OrderVersion, ChangedAtUtc)`; no change to `OrderDeliverySteps` — negative ranks need none). Add the EF configuration (FK `Restrict`, required ≤200-char reason, attribution check constraint mirroring the existing history tables) and add the new entity to `AppDbContext.ProtectHistory()` so rows stay immutable. Name pattern: descending timestamps like `202603XX000000_AddDeliveryRouteMovementHistory.cs`.
2. Do not update or delete existing rows. New columns on old rows must be nullable or have a safe, semantics-preserving default.
3. Do not reinterpret existing `Cancelled` statuses as "validly bypassed with replacement"; old data may lack replacement evidence.
4. Verify migration model snapshot and reverse migration behavior without dropping historical data in normal deployment. If rollback is necessary, new feature rows can be removed only under an explicit rollback plan; never delete pre-existing delivery records. **Tests use `EnsureCreated`, so the migration itself is never exercised — verify it manually against a real database (or record an explicit manual-verification note in the implementation).**
5. Test new schema with the identical SQLite in-memory options used by existing tests.

## 8. Acceptance criteria

### Dialog and validation

- Empty/whitespace reason is rejected in UI and domain/application.
- Missing alternative is rejected; missing party for deliveryman/carrier is rejected.
- Invalid or nonexistent party ID is rejected without changing order, step, work record, or history.
- Selecting the current party is rejected.
- Pickup route accepts no deliveryman/carrier ID; every other route clears the opposite party ID.
- Reassignment cannot be confirmed before execution starts; chain steps are reassignable only from `InProgress`/`Failed` (§12.13).
- Repeated clicks cannot create duplicate cancellation or assignment records.

### Delivery chain

- A failed step continues to block later steps until retried/completed or reassigned through the approved dialog.
- Confirmed reassignment leaves the original step and all original work records intact, with old work closed as cancelled and reason preserved.
- Replacement step is at the same logical position, is pending, and is the active step for sequencing; the old step's stored rank is the negative tombstone, and no other rank changes.
- Later steps remain blocked until the replacement is completed or validly rerouted.
- Original party's work number/party attribution is unchanged; replacement receives its own record/number under its own party.
- Cancellation does not count as completion and does not itself deliver the order.
- A cancelled step without a replacement still blocks successors (§4.1); only superseded (negative-rank) steps are invisible to sequencing.

### Direct assignment and pickup

- Direct deliveryman reassignment is only available after execution starts and bypasses ordinary order editing.
- Old direct trip remains attached to the order, retains party/number, and becomes cancelled with the required reason (or stays `Failed` untouched when it was already failed, §12.4).
- New direct deliveryman/carrier route creates a separate work record tied to the selected party.
- Both same-method party switches are supported: `OutForDelivery + DeliveryMan → another deliveryman` (stays `OutForDelivery`) and `Shipped + ShippingCompany → another carrier` (stays `Shipped`), each with a distinct status change/route movement record (§12.3).
- Pickup moves the order to the existing `ReadyForPickup` stage and creates **no** pickup work record; confirmation completes the order and writes history. Route selection alone does not.
- After a chain→pickup conversion the workflow page still renders the cancelled chain steps and their per-step histories (§12.6).

### History and financial safety

- Each operation produces one general-history movement event that identifies the cancelled assignment and the new assignment, while the cancelled step/work record keep their own status events; all rows share one order version and timestamp (§12.7.4).
- The general order history feed includes the movement events (merged feed per §11.7).
- After a replacement step completes, the order delivers via the active-position gate (§11.1) — retained cancelled steps never block delivery, and the gate cannot fire vacuously on an empty active set (§12.5).
- After any chain reassignment that remains a chain, `DeliveryReadinessError()` is null and the direct-delivery override remains available with outcomes covering only non-completed, non-superseded steps (§12.2). After a chain→pickup conversion the override is not applicable because the method is `Pickup`.
- The workflow page shows no negative step numbers anywhere; replaced steps appear inline at their original position with cancelled styling (§12.6).
- Progress counters, the progress bar and the “delivered with incomplete steps” notice use the non-superseded, non-cancelled set; a delivered reassigned order reports 100% (§12.9).
- Copyable party summaries exclude cancelled/superseded work records, show each record's own party, and never print a negative or zero step number (§12.10).
- Pickup confirmation produces source `PickupConfirmed` through every available entry point; no path stamps `ManualAction` for it (§12.11).
- The merged history feed has no rendering-key collisions, stable paging, and includes movement rows in the «آخر حدث» header computation (§12.7).
- One movement produces exactly one general-history event: no duplicate `OrderStatusHistory` row for the same operation (§12.7.4).
- Events include old/new route, party, reason, actor, timestamp, and order version as applicable.
- Cancelled work cannot be counted as completed work for future financial entitlement.
- Existing work-record numbers, parties, history rows, order history, and legacy-migration disclaimers remain unchanged.
- A persistence error or version conflict leaves no partial confirmed operation.

## 9. Test plan

Add focused tests in existing test areas, using SQLite in-memory with `EnsureCreated` and `UseSnakeCaseNamingConvention()` for persistence tests.

1. Domain transition tests for valid/invalid cancellation, reason requirement, started-state requirement, all alternatives, replacement blocking, no completion-on-cancel, and pickup confirmation — plus the new active-position completion gate, the immediate-previous non-superseded predecessor gate (a tombstoned step must not block successors; a cancelled step without a replacement must still block, §4.1), the vacuous-empty-set guard (§12.5), the source-state restrictions of §12.13, and the complete route/status matrix of §12.3 including both same-method party switches, every `DeliveryFailed` origin, and every `ReadyForPickup` landing.
2. Work-record tests proving old trip/shipment handling follows §12.4 (Pending → cancelled with reason; Failed → untouched) and retains numbering-party identity; the new record belongs to the selected party; pickup creates no record.
3. Persistence tests proving old steps, histories, trips, and shipments remain present after reassignment; the old step's `StepOrder` is tombstoned (`-<step.Id>`) **with the tracked current value staged**, the replacement occupies the freed rank, no other step rank changes, and the unique `(OrderId, StepOrder)` index is not violated — including a repeated reassignment of the replacement. This test must exercise the real targeted repository operation, not `PrepareDeliveryStepReorderingAsync`.
4. Handler tests for actor/auth (Orders/Write), missing order/party, selecting the current party, stale expected version, atomic persistence failure, and duplicate submission protection where applicable.
5. Read-model/history tests proving events are visible in order history with source-qualified keys (no `@key` collisions), a single event per movement (no duplicated order-status row), correct merged-feed paging/`TotalCount`/ordering, «آخر حدث» and `LatestReason` freshness after a movement, no negative ranks anywhere in the contract, and old party snapshots remain stable.
6. UI/render tests (or documented manual checks) for: steps rendered after a chain→pickup conversion; KPI/progress and the “incomplete steps” notice computed from non-superseded, non-cancelled steps; copyable summaries excluding cancelled/superseded records and using the record's own party.
7. Pickup tests proving the existing `ReadyForPickup + Pickup + Delivered` entry point yields `PickupConfirmed` (not `ManualAction`) and that route changes to pickup clear both party IDs and render «استلام من المقر».
8. Migration/history-protection tests: movement rows are not updatable/deletable through the context; the configured indexes/check constraint are present; the forward-only migration is verified manually (tests use `EnsureCreated`).
9. UI/component validation where existing test setup supports it; otherwise validate server command and render behavior manually during implementation.
10. Run the repository-required sequence from `AGENTS.md`: restore, Release build, Release tests with `--no-build`, and the Node local-time test. Do not alter CI release/tag behavior.

## 10. Explicit non-goals

- Do not allow arbitrary post-start edits through the ordinary order editor.
- Do not delete or rewrite old steps, trips, shipments, numbering, history, or financial rows.
- Do not use failure as a bypass; failure remains distinct from cancellation.
- Do not automatically complete an order upon reassignment or pickup route selection.
- Do not change the behavior of administrative direct-delivery override except to ensure it remains compatible with new history and replacement state.
- Do not add JavaScript, runtime font fetching, secrets, or unrelated UI redesign.

## 12. Verified domain constraints (code-verified and cross-reviewed 2026-11-07 — supersedes contradicting text above)

Every item below was confirmed against the actual source and re-audited in the second review. **§12.1–§12.13 are the authoritative implementation contract**; implementers must honor them exactly.

### 12.1 Aggregate/API gaps (why new internal mutators + repository ops are mandatory)

- `OrderDeliveryStep.StepOrder{ set }` calls `EnsurePlanning()`, which throws when `Order.CanEdit` is false — i.e. every post-start order. Same guard applies to `DeliveryMethod`, `DeliverymanId`, `ShippingCarrierId` on the step; `Order.AddDeliveryStep`, `ClearDeliverySteps`, `SynchronizeDeliverySteps` all sit behind `Order.EnsureEditable()`. Therefore **the tombstone rank, the replacement-party values on a *new* step, and the new step's attachment must be done by** (a) constructing the new step and setting its rank/method/party **before** `AttachTo(order)` (setters pass while `Order is null`, and `AttachTo` only refuses re-parenting saved steps), then appending it to the private step collection from inside `Order`, and (b) a **repository-level change-tracker operation** for the tombstoned retained step.
- **Do not use `PrepareDeliveryStepReorderingAsync` for reassignment.** That method vacates **every** rank in the database to `-step.Id` and rewrites each tracked entry's `OriginalValue` only. It deliberately leaves the tracked **current** value at the old positive rank and relies on `UpdateOrderHandler` calling `SynchronizeDeliverySteps` afterwards to assign every current rank. A movement that reuses it and skips the tombstoned step (as the first draft suggested) would save the old positive rank back and violate the unique `(OrderId, StepOrder)` index when the replacement inserts at the same rank.
- Required operation — **targeted single-row tombstone**: inside the caller's transaction, bulk-update the one affected row to `-<step.Id>`, then set that tracked entry's `OriginalValue` **and** `CurrentValue` to `-<step.Id>` (the configuration uses `PropertyAccessMode.Field`, so this bypasses the planning guard correctly and EF will not rewrite the positive rank). No other step rank changes, so no other rank writes are required. The replacement is constructed with the freed rank and appended inside `Order` (never via `EnsureEditable`-guarded `AddDeliveryStep`).
- The bulk update, the domain movement and a single `SaveChanges` must share one transaction (the bulk update throws otherwise) and the bulk update executes before EF's INSERT, so the unique index is never violated.

### 12.2 Readiness and direct-override must ignore superseded steps

- `Order.DeliveryReadinessError()` rejects any `StepOrder <= 0` («جهة تنفيذ الخطوة {rank} غير صالحة»). Without a fix, every reassigned chain order would carry a permanent readiness error, which kills `CanDeliverDirectly` (requires readiness error null) and surfaces a phantom error in the workflow read model. Fix (no schema): exclude **superseded** steps (`StepOrder < 0`) from the chain count, the duplicate-rank check and the `ValidStepHandler` loop.
- Use the **negative rank**, not `Status == Cancelled`, as the superseded marker. It is the durable evidence of “cancelled *with* a replacement”, it does not depend on the assumption that cancelled steps only exist on cancelled orders (legacy data), and it preserves the §4.1 rule that a cancelled step **without** a replacement still blocks progression.
- `Order.DeliverDirectly` computes `remaining = _deliverySteps.Where(status != Completed)` — it must exclude **superseded** steps, otherwise the outcomes-count validation rejects the admin's direct close after a reassignment. A non-superseded `Cancelled` step stays in the outcomes list (unchanged existing contract, §12.9). The direct-override contract after a chain reassignment: **remains available**, with outcomes covering only non-completed, non-superseded steps. It is **not** available after a chain→pickup conversion because the method is `Pickup` (see §8).

### 12.3 Route/status matrix (direct + pickup variants) — one dedicated internal movement

`AllowedStatuses` is (current status, current method)-keyed: `OutForDelivery`+`ShippingCompany` offers only `Cancelled` (no path to `Shipped`/`Delivered`), `Shipped`+`DeliveryMan` is equally stranded, `ReadyForPickup` is reachable only from `Processing`+Pickup. `TransitionTo` also early-returns Success when `target == OrderStatus` (line 95) — same-status route switches silently no-op — and every successful transition bumps `Version` and stamps `ManualAction`/`Retry`. **Do not compose `TransitionTo` for the movement.** Implement one internal `Order` movement behavior that, in a single `Version++`, does everything and writes the movement row with the intended source. Because the movement row *is* the event, the behavior sets `OrderStatus` directly and calls `UpdateModificationDate(operation.AtUtc)` instead of `RecordOrder` — no `OrderStatusHistory` row is appended (§12.7.4) — and same-status switches (matrix rows 1 and 3) therefore write no status change at all. Resolve the route through **one** matrix function (not duplicated in the handler/UI):

| Current status (direct routes) | New route | New status | Old record | New record |
|---|---|---|---|---|
| `OutForDelivery` + `DeliveryMan` | `DeliveryMan` (other party) | `OutForDelivery` | close trip → `Cancelled` (Pending only, §12.4) | new `DeliveryTrip` under deliveryman |
| `OutForDelivery` + `DeliveryMan` | `ShippingCompany` | `Shipped` | close trip → `Cancelled` (Pending only, §12.4) | new `Shipment` under carrier |
| `Shipped` + `ShippingCompany` | `ShippingCompany` (other party) | `Shipped` | close shipment → `Cancelled` | new `Shipment` under carrier |
| `Shipped` + `ShippingCompany` | `DeliveryMan` | `OutForDelivery` | close shipment → `Cancelled` | new `DeliveryTrip` under deliveryman |
| `DeliveryFailed` + `DeliveryMan` | `DeliveryMan` (other party) | `OutForDelivery` | old trip stays **`Failed`** (untouched, §12.4) | new `DeliveryTrip` |
| `DeliveryFailed` + `DeliveryMan` | `ShippingCompany` | `Shipped` | old trip stays **`Failed`** | new `Shipment` |
| `DeliveryFailed` + `ShippingCompany` | `DeliveryMan` | `OutForDelivery` | old shipment stays **`Failed`** | new `DeliveryTrip` |
| `DeliveryFailed` + `ShippingCompany` | `ShippingCompany` (other party) | `Shipped` | old shipment stays **`Failed`** | new `Shipment` |
| any of the above | `Pickup` | `ReadyForPickup` | close if Pending, else stays Failed | none (order-level pickup flow, §12.11) |

Matrix rules: selecting the **current party** is rejected; `Processing` (not started), `ReadyForPickup` as a source (out of v1 scope), `Delivered`, `Cancelled` and archived orders are rejected. Chain steps are reassignable only from `InProgress`/`Failed` (§12.13); the chain replacement may target a deliveryman or a carrier, and pickup applies the chain+pickup variant of §11.4.

Direct work records are created by `Order.StartDirectWorkRecord()` (private, invoked only from `TransitionTo` lines 113-115) — the movement must call it explicitly (internal access is fine) **after** mutating the route, because `CloseDirectWorkRecord` picks the collection from the *current* `DeliveryMethod` (lines 251-262): mandatory order of operations inside the movement = close old record (while old method still set) → clear the opposite party ID → mutate route/party → create new record. For pickup, clear **both** party IDs (§12.11). For chain steps the equivalent is `CloseStepWorkRecord` on the cancelled step (its `Pending` record only, §12.4). The movement creates **no** work record or number for the `Pending` replacement — the replacement's record is created exactly as today when it transitions to `InProgress` (`step.StartShipment()/StartDeliveryTrip()`), so the movement's `NewShipmentId`/`NewDeliveryTripId` stay null for chain movements.
- **Pickup auto-cancellation must close work records too.** For every remaining non-completed step the movement records `Cancelled` **and** calls `CloseStepWorkRecord(step, Cancelled, reason, atUtc)`, so no `Pending` record is orphaned on an aborted chain; a `Failed` record stays `Failed` (§12.4). This mirrors `TransitionTo(Cancelled)` lines 105-110 without reusing it.

### 12.4 Work-record cancellation is only possible from `Pending`

`WorkRecordLifecycle.Transition` throws for any `current != Pending` («المحاولة غير معلقة ولا يمكن تغيير حالتها»). Rule: the movement closes the old record only if it is `Pending`; a `Failed` (or `InProgress`-pattern equivalent, or already `Cancelled`/`Completed`) record is left untouched and the movement history row narrates what happened. This matches §4.1 failure-vs-cancellation semantics.

### 12.5 Completion gate must not be vacuously true

`_deliverySteps.All(Completed)` returns `true` for an empty collection. The chain+pickup movement cancels every remaining step; therefore the new active-position predicate (§11.1) must quantify over **at least one non-superseded step**, or be evaluated only on step-completion transitions (`TransitionStep`, where the just-transitioned step guarantees a non-empty set) and by the direct-override flow — never by a cancel/route-change path.

### 12.6 Read model / UI must never leak negative ranks

- `OrderWorkflowRead` sorts steps by raw `StepOrder`, and `OrderWorkflow.razor` renders «الخطوة @step.StepOrder» — tombstones would appear as «الخطوة -12» and jump to the top. Fix: `DeliveryStepWorkflowState` gains `IsSuperseded` (`StepOrder < 0`) and `EffectiveRank` (the original rank read from the movement row's `OriginalStepRank`, falling back to `Math.Abs(StepOrder)` for a tombstone without a movement row). Sort by `EffectiveRank` with a deterministic tie-breaker (effective rank, then movement/step creation order) because repeated cancellation can produce the same original rank more than once; render it with invariant digits. `DirectDeliveryDialog` uses the same filtering/labels.
- **Owner decision:** cancelled/replaced steps render **inline at their original rank position** with «ملغاة/مُعاد توجيهها» styling (existing blocker text «الخطوة ملغاة.» already supports this), with the replacement immediately after it — never in a detached group, never with a negative number.
- **The delivery-steps section must render whenever the order has steps, regardless of the current route.** `OrderWorkflow.razor` currently gates it on `Method == DeliveryChain`; after a chain→pickup conversion the entire cancelled chain and every per-step history panel would disappear. The stage strip/instruction follow the current method; the steps section carries the aborted-chain audit.
- **Counters are part of this contract:** the completed/total KPI, the progress bar and the “delivered with incomplete steps” notice in `OrderWorkflow.razor` and `OrderDetials.razor` use the non-superseded, non-cancelled set (§12.9). Including cancelled steps makes a delivered order report “incomplete steps” forever.

### 12.7 Merged history feed contract

- **12.7.1 Stable key.** `OrderHistoryEntry` is keyed by a per-table `Id` and the panel uses `@key="entry.Id"`; a union over two tables collides. Give the entry a stable string `Key` (`"order:{Id}"` / `"movement:{Id}"`) and use `@key="entry.Key"`.
- **12.7.2 Display contract.** Movement rows must show old route/party → new route/party, the reason and the actor. Extend `OrderHistoryEntry` with a display tag and detail (or equivalent) and set `PreviousStatus`/`NewStatus` from the movement snapshots. `HistoryTone` is status-label based and cannot “learn a source”; add a source-based tone/label mapping and branch the panel on `Source` for the two new values (per §11.8). Without these fields the acceptance criterion “events include old/new route and party” cannot render.
- **12.7.3 Paging.** `GetHistoryAsync` is DB-paged **per table**. The merged feed needs one query over a shared projection (`OrderStatusHistories` concat movement rows with identical column count/types), ordered by `OrderVersion DESC, ChangedAtUtc DESC, source-rank, Id DESC`, with `TotalCount` computed from the same query. Do not page each table independently and merge in memory without a defined order.
- **12.7.4 One event per movement.** Do **not** also write a duplicate `OrderStatusHistory` row for the same movement; the movement row is the authoritative event and the merged feed must show exactly one row per operation. Consequential step/work-record status rows keep existing sources (`ManualAction` for auto-cancelled remaining chain steps; never `OrderCancellation`, never a duplicated `DeliveryRouteReassigned` status row).
- **12.7.5 «آخر حدث».** The header computation (`OrderWorkflowRead` lines 46-52: `stepIsLatest` vs `lastOrderEvent`) must include movement rows in both sides, and `LatestReason`/`IsLegacy` must come from the newest row across both tables, or the header shows stale information after a reassignment.

### 12.8 Miscellaneous verified facts

- `StepOrder` is a plain `int` under a unique `(OrderId, StepOrder)` index — negative tombstones need **no** schema/migration change; the only migration items are the new movement-history table (§12.12) and the two enum string values (safe with `HasConversion<string>`).
- Tests use `EnsureCreated`, so migrations are never exercised by tests — the movement-table migration must be verified manually (or via an explicit migration smoke note in the implementation).
- Authorization is **Orders/Write** (owner decision §11.10) — same gate as all lifecycle commands; no additional role restriction.

### 12.9 One active-step predicate used by every consumer

`Superseded(step) => step.StepOrder < 0` is the single marker; the movement is the only flow that **persists** a negative rank (the editor's `PrepareDeliveryStepReorderingAsync` staging sweep is always renumbered back to positive by `SynchronizeDeliverySteps` before commit), and a step cancelled without a replacement keeps its positive rank. Implement it once on `Order` (e.g. `ActiveSteps => _deliverySteps.Where(step => step.StepOrder > 0)`) and consume it in all five places — never re-derive the predicate in Razor or in the handler.

| Consumer | Current code | Required behavior |
|---|---|---|
| Readiness | `DeliveryReadinessError()` lines 46-53: count, duplicate-rank check, `StepOrder <= 0` and `ValidStepHandler` loop over **all** steps | run the count, duplicate-rank and handler checks over `ActiveSteps` only; the `StepOrder <= 0` branch is then unreachable and can be dropped; an order whose only steps are superseded is invalid («يجب إضافة خطوة توصيل واحدة على الأقل») |
| Sequencing | `AllowedStepStatuses` line 123: any `previous.StepOrder < step.StepOrder && previous.Status != Completed` blocks | predecessors = `ActiveSteps`; return `[]` for a superseded step and let `TransitionStep` fail with «الخطوة ملغاة أو مستبدلة.» before any other check |
| Completion | `TransitionStep` lines 158-159: `_deliverySteps.All(Completed)` (vacuously true when empty) | `remaining = ActiveSteps.Where(status != Completed)`; Delivered only when `remaining.Count > 0 && remaining.All(Completed)`; never evaluate on cancel/route-change paths (§12.5) |
| Direct override | `DeliverDirectly` line 173: `Where(status != Completed)` over all steps | same set minus Completed; a non-superseded **Cancelled** step stays in the outcomes list (existing contract preserved by §4.1) |
| Progress/KPI/UI | `OrderWorkflow.razor`, `OrderDetials.razor` counters, progress bar, “incomplete steps” notice | completed = ActiveSteps with `Completed`; total = ActiveSteps excluding `Cancelled`; expose both counts on `OrderWorkflowState` (e.g. `ActiveStepCount`/`CompletedActiveStepCount`) so the UI never recomputes |

`TransitionTo(Cancelled)` must **not** tombstone: it leaves ranks untouched, so the order's final state keeps an honest step layout for the audit, and no readiness/sequencing gate runs because the order is final.

### 12.10 Copyable summaries and party attribution

- `OrderSummaryRead.GetSummariesAsync` resolves order-level names from `order.Deliveryman`/`order.ShippingCarrier` — the **current** route party — and includes every trip/shipment. After a reassignment the aborted party's records would be copied under the new party's name, and cancelled records would still be printed.
- `OrderSummaryTextBuilder.Build` prints `record.StepOrder` raw (a tombstone prints «خطوة -3»), and the chain `from`/`to` neighbours are computed from the **unfiltered** `steps` list.
- Fix, in `OrderSummaryRead`: (1) resolve each record's party **name** from its own `NumberingPartyId` (trips → deliverymen, shipments → carriers) because the FK is per record; (2) include a record only while its scope still points at that party — order scope: `order.DeliverymanId`/`ShippingCarrierId` equals `NumberingPartyId`; step scope: the step is non-superseded **and** its `DeliverymanId`/`ShippingCarrierId` equals `NumberingPartyId`; (3) drop `WorkRecordStatus.Cancelled` records; keep `Failed` records only under rule (2) so an active failed assignment can still be resent while a replaced one disappears; (4) compute neighbours and print step numbers from the same filtered, effective-rank list.
- Consequence: the copyable summaries (`deliveryman`/`shipping` buckets) contain only currently accountable parties; aborted parties stay visible in the merged history feed and per-step panels (§12.7), which are the audit surface.

### 12.11 Pickup has exactly one confirmation path; route changes clear party IDs

- Conversion to pickup (chain or direct) sets `DeliveryMethod = Pickup` and clears **both** `DeliverymanId` and `ShippingCarrierId` inside the movement. Attribution is not lost: old records keep their own `NumberingPartyId` and the movement row snapshots the old party (§12.12). Leaving stale IDs makes `OrderWorkflowRead` lines 48-49 fall into the carrier branch and display an unrelated party on a pickup order.
- `HandlerName` for `Pickup` must render «استلام من المقر» in the order read model, the step read model and every banner (`OrderWorkflow`, `OrderDetials`); the current ternary yields `null` ⇒ «غير محددة».
- Confirmation stays on the existing entry point: `TransitionTo(Delivered)` with `ReadyForPickup + Pickup`, which `ChangeOrderStatusCommand` and the UI already call. Inside `TransitionTo`, after validation and **before** `Version++`/the generic body, delegate exactly that triplet (target `Delivered`, current status `ReadyForPickup`, method `Pickup`) to a dedicated internal `ConfirmPickup(operation)` that writes `OrderHistorySource.PickupConfirmed` and performs the same `Version++`/`UpdateModificationDate` work (`CloseDirectWorkRecord` is a no-op because no record exists). One path for every caller, no `ManualAction` mislabel, `AllowedStatuses` and validation unchanged.
- Do **not** add a pickup work record, number, table or dialog (§2.6); the movement and the confirmation row are the only pickup-related writes.

### 12.12 Movement-history schema and immutability

- New table `order_delivery_movements` + `OrderDeliveryMovement` model. Columns: `Id`; `OrderId` (FK, `OnDelete(DeleteBehavior.Restrict)`, mapped by `HasMany(x => x.DeliveryMovements).WithOne(x => x.Order).HasForeignKey(x => x.OrderId)` beside `OrderConfiguration` line 61); `DeliveryStepId?` / `ReplacementStepId?` with `DeliveryStep` / `ReplacementStep` navigations (FK to `order_delivery_steps`, `Restrict`; null for direct orders); `OldShipmentId?` / `OldDeliveryTripId?` / `NewShipmentId?` / `NewDeliveryTripId?` with matching navigations (**FK per target table**, `Restrict`, at most one old and one new set; the `New*` pair is null for a chain movement because the replacement's record does not exist yet — §12.3 — while the `Old*` pair always identifies the record that was in effect before the movement, whether it was closed as `Cancelled` or left `Failed`, per §5.3); `PreviousMethod` / `NextMethod` (`DeliveryMethod`, `varchar(30)`, string-converted); `PreviousPartyId?` / `PreviousPartyName` (≤100 — matches the party name columns) / `NextPartyId?` / `NextPartyName` (≤100); `OriginalStepRank?` (int — the effective rank of the superseded step, required by §12.6); `Reason` (≤200, **required**); `ChangedByUserId` (FK `Restrict`) / `ChangedByUserName` (≤150); `ChangedAtUtc` (`timestamp with time zone`); `OrderVersion` (int); `Source` (`varchar(30)`, string-converted — only `DeliveryRouteReassigned` today; a pickup conversion is also a route reassignment, while `PickupConfirmed` is written to `OrderStatusHistory`).
- **Link the replacement step and the new work record by navigation, never by a raw ID.** Both are `Added` in the same `SaveChanges` and hold `Id = 0` until insert; `ReplacementStep` / `NewShipment` / `NewDeliveryTrip` navigations let EF fix the foreign keys up in dependency order and keep the whole movement in one save. Raw `Id` columns would silently persist `0`. The old step/record navigations are set from persisted rows and are unaffected.
- Check constraint `ck_movement_attribution`: `changed_by_user_id IS NOT NULL AND changed_by_user_name IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200` (the entity is never `LegacyMigration`, unlike `OrderStatusHistoryConfiguration`).
- Indexes: `(OrderId, OrderVersion, Id)` for the merged feed (§12.7.3) and `(OrderId, OrderVersion, ChangedAtUtc)` for the «آخر حدث» query (§12.7.5).
- Add `OrderDeliveryMovement` to `AppDbContext.ProtectHistory()` (line 65) and a matching `DbSet`, so a movement row is immutable after insert like the other histories.
- The movement captures `PreviousMethod` / `PreviousPartyId` / `PreviousPartyName` **before** mutating the route, and `OriginalStepRank` from `step.StepOrder` before the tombstone.

### 12.13 Allowed source states

- **Direct order** (`DeliveryMethod` = `DeliveryMan` or `ShippingCompany`): only `OutForDelivery`, `Shipped` and `DeliveryFailed` with the corresponding party set. `Processing` goes through the editor (§12.3); `ReadyForPickup`, `Delivered`, `Cancelled` and archived orders are rejected.
- **Chain step**: the step must be non-superseded and in `InProgress` or `Failed`. A `Pending` step stays planned — it is changed through the editor while the order is still `Processing`, and is not movable in v1 once execution starts; `Completed`/`Cancelled` steps are rejected.
- **Replacement party** must differ from the current one (order-level), or from the step's current party (chain) — equal-party selection is rejected with a domain error, not a silent no-op.
- `OrderWorkflowState.CanReassignDirectly` and `DeliveryStepWorkflowState.CanReassign` are computed from these rules plus `IsArchived`/`IsFinal`, so the UI opens the dialog only when the movement can succeed; the domain re-validates on submit.

## 11. Locked implementation decisions (owner answers, 2026-11-07)

1. **Chain completion rule (active-position gate).** Delivery is decided by an active-position predicate over non-superseded steps, not the current `_deliverySteps.All(step => step.Status == DeliveryStatus.Completed)`. “Superseded” means `StepOrder < 0` (§12.9). The order becomes Delivered only when at least one active step exists and every remaining active step (status ≠ Completed) is Completed; the gate runs only from `TransitionStep`, never from a cancel/route-change path. Orders without tombstones behave exactly as today; a step cancelled **without** a replacement keeps its positive rank and still blocks (§4.1).
2. **Rank strategy = tombstone rewrite.** Inside the confirmed movement's transaction the cancelled old step's persistent `StepOrder` is rewritten to a negative tombstone (`-<step.Id>` — per-order collision-free under the unique `(OrderId, StepOrder)` index), the replacement step occupies the freed old rank, and the original rank is preserved in the movement history row (`OriginalStepRank`). **Do not reuse `OrderRepository.PrepareDeliveryStepReorderingAsync`** — it sweeps every rank, leaves tracked current values positive, and requires `SynchronizeDeliverySteps` afterwards (§12.1). Use the targeted single-row staging there instead: one bulk update plus `OriginalValue` **and** `CurrentValue` on the tombstoned tracked entry, all in the same transaction and before EF's INSERT. One tombstone per cancellation — no pre-reserved spare ranks; re-cancelling replacement #1 repeats the same cancel + replace operation.
3. **Progression gate must be active-steps aware.** `AllowedStepStatuses` and its predecessor filter must ignore superseded steps (§12.9); the blocking predecessor is the **immediate previous non-superseded step** (`ActiveSteps.Where(rank < step.StepOrder).MaxBy(rank)`), which must be Completed. Otherwise a tombstoned row (rank below everything, status ≠ Completed) blocks every successor forever, or a cancelled-and-replaced predecessor is checked twice. Genuine pending/failed/in-progress steps and a pending replacement still block successors, and a superseded step itself is never transitionable.
4. **Pickup = reuse the order-level Pickup flow.** No new `WorkRecordType`, no new `NumberSequenceKind`, no new table. Chain + pickup: tombstone + cancel the affected step, auto-cancel every remaining non-completed step with **`ManualAction`** step-history rows and close its `Pending` work record (§12.4; never `OrderCancellation`, never `DeliveryRouteReassigned`, per §5.7/§12.7.4), set the order route to `DeliveryMethod.Pickup`, clear both party IDs, and move the order to `ReadyForPickup` **inside the dedicated movement behavior** (§12.3) — never by composing `TransitionTo`, which is unreachable from `OutForDelivery`/`Shipped`/`DeliveryFailed` route changes, double-bumps `Version` (line 102), stamps the wrong source, and early-returns Success on same-status targets (line 95). Direct + pickup: same movement (close the active record if Pending + route → `ReadyForPickup` + clear both IDs). Receipt confirmation uses exactly one path: `TransitionTo(ReadyForPickup → Delivered, Pickup)` delegates to the internal `ConfirmPickup` so every entry point records `PickupConfirmed` (§12.11).
5. **Post-start route mutation via a dedicated internal domain behavior.** Order route fields are currently mutable only through `UpdateHeader` behind `EnsureEditable()`. The reassignment flow uses a new internal, fully validated mutator on `Order` (started-state, valid party pairing, clearing the opposite party ID, clearing both on pickup, Version bump) that is never exposed to ordinary editing.
6. **Two new `OrderHistorySource` values: `DeliveryRouteReassigned` and `PickupConfirmed`.** The movement row (new source, schema in §12.12) carries the required reason plus old route type / old party id+name snapshot / old rank (chain) and the new route type / new party id+name snapshot. Step and work-record status rows written in the same movement keep their existing sources (genuine status transitions); correlation is by identical `OrderVersion` + `ChangedAtUtc` inside the movement — no new correlation column. **Do not also write a duplicate `OrderStatusHistory` row for the same movement** (§12.7.4). **Verified caveat:** `WorkRecordStatusHistory` has no `OrderVersion` column at all and carries `CreatedAtUtc` — its only correlation key is the movement's single shared operation timestamp, so every row in one movement must use exactly one `Operation.AtUtc`.
7. **General history feed merges movement rows.** The stepId-less history query unions order-status history with the movement history so the full audit trail is discoverable in one feed; step panels keep per-step events. Never mislabel legacy rows.
8. **UI wording.** Dialog title «إلغاء التكليف وإعادة التوجيه» — a single atomic movement (one submit). Success toast «تم إلغاء التكليف وإعادة التوجيه بنجاح». History display texts: `OrderHistorySource` currently has **no** `[Description]` attributes and the panel maps sources ad hoc — add Arabic `[Description]`s (including for the whole existing enum) *and* extend `OrderHistoryPanel.razor`'s source mapping for the two new values. Storage is string (`HasConversion<string>`), so appending enum values is safe.
9. **Party validity = exists-only (owner decision).** Replacement deliveryman/carrier must merely exist; no `IsActive` flag, no schema change, no selector filtering beyond what the editor queries already do.
10. **Permission gate = Orders/Write (owner decision).** Any order writer may reroute a started delivery, consistent with lifecycle transitions (deliver/fail/retry are also Orders/Write). Domain validation is the final guard.
