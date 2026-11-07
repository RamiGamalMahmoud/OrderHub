# Delivery Cancellation and Reassignment — Implementation Specification

## 1. Purpose and implementation status

This document specifies the approved behavior for cancelling an active delivery assignment and routing the order to a valid alternative. It is intended for AI agents implementing the feature in this repository.

**Planning artifact only.** Do not treat this file as authorization to implement the feature. The user explicitly requested planning and has not authorized code changes. When implementation is authorized, follow this document and the repository `AGENTS.md`.

**Design gate RESOLVED (2026-11-07):** the owner answered the final persistence/behavior questions; §11 now carries the locked implementation decisions instead of open items. Phase A has only verification work left.

The feature must preserve existing order, delivery-step, work-record, history, numbering, and financial data. Do not delete or rewrite existing persisted delivery history.

## 2. Approved product decisions

1. Reassignment is available only after execution has started. It is not a normal order-editor operation.
2. Starting cancellation opens a dialog. Cancellation cannot be confirmed until the user has entered a non-empty reason and selected a valid alternative.
3. Supported alternatives are:
   - Assign to another deliveryman.
   - Assign to a shipping carrier.
   - Customer pickup from the branch/headquarters (`DeliveryMethod.Pickup`, described in Arabic as `استلام من المقر`).
4. For a delivery chain, a replacement step is inserted at the cancelled step's execution position. The old step remains cancelled and historically intact; do not mutate its handler to the new party.
5. For a direct deliveryman assignment, cancel the existing deliveryman work record and create the work record appropriate to the chosen new route. Do not update the executing order through the ordinary editor.
6. For branch pickup, create a pickup/receipt execution record and complete the order only when receipt is explicitly confirmed. Reassignment alone must not complete the order.
7. Every movement is a distinct historical record. At minimum, cancellation and new assignment are separately identifiable events, linked or ordered so the audit trail explains their relationship.
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

- Chain + deliveryman/carrier: old step remains cancelled; create a new step for the selected party in the same execution position; new step starts pending; later steps remain blocked until replacement completes or is itself cancelled with another valid alternative.
- Chain + pickup: old step remains cancelled; replace its execution position with a pickup stage. Define the representation so pickup has an explicit confirmation action and can complete the order. Do not create a deliveryman trip or carrier shipment for pickup.
- Direct deliveryman + deliveryman/carrier: cancel old direct work record; persist the route-change/cancellation history; update the order's active method/party through a dedicated lifecycle behavior; create a new direct work record for the new party/method. Keep old record attached to the order with its original party attribution and number.
- Direct deliveryman + pickup: cancel old direct work record; change the active delivery method to `Pickup`; create a distinct pickup/receipt record; order stays in a pickup-pending state until receipt confirmation, then transitions to `Delivered`.

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
5. Represent each movement as a distinct row/event. If cancellation and assignment are confirmed together in the UI, they may be persisted in one database transaction while remaining distinct historical records. Failed persistence must not leave only one half of the confirmed movement.
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
5. ~~Design replacement-step ordering~~ — **Decided (§11.2):** tombstone rewrite. Inside the confirmed movement's transaction: the cancelled old step's `StepOrder` is rewritten to a persistent negative tombstone (`-<step.Id>` — collision-free under the unique `(OrderId, StepOrder)` index), the replacement takes the freed rank, and the original rank is preserved in the movement history row. Reuse the existing staging precedent `OrderRepository.PrepareDeliveryStepReorderingAsync`. One tombstone slot per cancellation — repeated reassignment repeats the cancel + replace operation, no pre-reserved spare ranks.
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
5. Close only the active old work record as `Cancelled`, preserving its number and party identity. Record the cancellation reason on its status history.
6. Create a distinct replacement step/work record with pending status, at the same logical chain position or as the new direct order assignment. Never mutate the old step's party/method. **Chain variant (§11.2):** the old step is tombstoned — its `StepOrder` becomes `-<step.Id>` in the same transaction — and the replacement step occupies the freed old rank; the original rank is captured in the movement history row. **Pickup variant (§11.4):** no replacement step/work record — the order route becomes Pickup and the order transitions to `ReadyForPickup` within this same confirmed movement, auto-cancelling remaining non-completed chain steps as order-cancellation semantics already do.
7. Increment order version once per confirmed operation; all related history rows use that version and operation timestamp.
8. **Replace the completion predicate (§11.1):** `TransitionStep`'s current `_deliverySteps.All(step => step.Status == DeliveryStatus.Completed)` delivery check is retired. New active-position gate: ignoring tombstoned (cancelled/superseded) steps, delivery occurs when the remaining steps — ordered by effective rank — are all Completed. Legacy orders without tombstones keep identical behavior. **Also update `AllowedStepStatuses` (§11.3):** the predecessor filter must ignore tombstoned steps, otherwise a tombstoned step (rank below everything, status ≠ Completed) blocks every successor. Genuine pending/failed/in-progress steps — and a pending replacement — still block successors.
9. Ensure an order becomes `Delivered` only through the normal last-stage completion or confirmed pickup; cancellation alone never delivers it.

### Phase C — Implement application commands and persistence

1. Add dedicated commands rather than extending `UpdateOrderCommand`.
2. Check authenticated actor and `ExpectedVersion`; translate stale versions to error code `Conflict`.
3. Validate referenced deliveryman/carrier existence and active/selectable status using application contracts/readers.
4. Load order, delivery steps, work records, and relevant navigations through a dedicated lifecycle repository method/query.
5. Persist route change, old work-record cancellation, new work/step records, and all movement history in a single unit-of-work transaction. The history remains multiple distinct rows: one movement row (new source) with full old/new snapshots + the genuine status rows with existing sources, all sharing the movement's version + timestamp.
6. Catch persistence conflicts/failures using established `Result` patterns. On failure, guarantee no partial cancellation/reassignment is visible.
7. Add a dedicated pickup confirmation command with expected-version protection and history.

### Phase D — Build UI flow

1. From an active, started direct assignment or active chain step, present an action such as “إلغاء التكليف وإعادة التوجيه”. Do not expose it for unstarted plan rows, completed rows, archived/final orders, or already superseded assignments.
2. Dialog fields:
   - Required cancellation reason.
   - Required alternative route: another deliveryman, shipping company, or customer pickup from headquarters.
   - Required party selector for deliveryman/carrier routes; no party selector for pickup.
   - Clear summary of old assignment and selected new route.
3. Disable confirmation until reason and route/party are valid. Validate again server-side.
4. Submit exactly once, guard busy state, use the displayed order version, and handle `Conflict` by reloading and asking the user to review current state.
5. Show pickup-specific confirmation action after route change. Do not show the order as delivered until that action succeeds.
6. Keep Arabic UI, RTL layout, logical Tailwind spacing, invariant digits where numeric values appear, and existing status tone conventions. Do not add JavaScript.

### Phase E — Read models and history presentation

1. Update `GetOrderWorkflowQuery` projection with replacement relationships, current active assignment, and pickup readiness.
2. Update blocker text to explain whether a previous step is failed, cancelled with replacement pending, or completed. Keep legacy-history disclaimer intact.
3. Ensure the order's general history feed merges the movement rows: `GetHistoryAsync` (stepId-less) unions `OrderStatusHistories` with the new movement history in version/chronological order so cancellation, reassignment, and pickup confirmation are discoverable in one audit feed (§11.7). Step panels keep per-step events. Legacy disclaimer handling unchanged.
4. Ensure step history can show the cancelled old step even after a replacement becomes active. Do not collapse old and new step IDs into one row.
5. Update order summary and details page to show the currently active route and to make the cancelled route discoverable in history.
6. Ensure party names in old history use snapshots so later renames do not rewrite audit meaning.

### Phase F — Migration and compatibility

1. Add a forward-only migration with the repository's timestamp/name convention if new tables/columns are needed.
2. Do not update or delete existing rows. New columns on old rows must be nullable or have a safe, semantics-preserving default.
3. Do not reinterpret existing `Cancelled` statuses as “validly bypassed with replacement”; old data may lack replacement evidence.
4. Verify migration model snapshot and reverse migration behavior without dropping historical data in normal deployment. If rollback is necessary, new feature rows can be removed only under an explicit rollback plan; never delete pre-existing delivery records.
5. Test new schema with the identical SQLite in-memory options used by existing tests.

## 8. Acceptance criteria

### Dialog and validation

- Empty/whitespace reason is rejected in UI and domain/application.
- Missing alternative is rejected; missing party for deliveryman/carrier is rejected.
- Invalid or nonexistent party ID is rejected without changing order, step, work record, or history.
- Pickup route accepts no deliveryman/carrier ID.
- Reassignment cannot be confirmed before execution starts.
- Repeated clicks cannot create duplicate cancellation or assignment records.

### Delivery chain

- A failed step continues to block later steps until retried/completed or reassigned through the approved dialog.
- Confirmed reassignment leaves the original step and all original work records intact, with old work closed as cancelled and reason preserved.
- Replacement step is at the same logical position, is pending, and is the active step for sequencing.
- Later steps remain blocked until the replacement is completed or validly rerouted.
- Original party's work number/party attribution is unchanged; replacement receives its own record/number under its own party.
- Cancellation does not count as completion and does not itself deliver the order.

### Direct assignment and pickup

- Direct deliveryman reassignment is only available after execution starts and bypasses ordinary order editing.
- Old direct trip remains attached to the order, retains party/number, and becomes cancelled with the required reason.
- New direct deliveryman/carrier route creates a separate work record tied to the selected party.
- Pickup creates a distinct pending receipt workflow; confirmation completes the order and writes history. Route selection alone does not.

### History and financial safety

- Each cancellation and new assignment is a distinct history event, visibly related and queryable from order history.
- The general order history feed includes the movement events (merged feed per §11.7).
- After a replacement step completes, the order delivers via the active-position gate (§11.1) — retained cancelled steps never block delivery.
- Events include old/new route, party, reason, actor, timestamp, and order version as applicable.
- Cancelled work cannot be counted as completed work for future financial entitlement.
- Existing work-record numbers, parties, history rows, order history, and legacy-migration disclaimers remain unchanged.
- A persistence error or version conflict leaves no partial confirmed operation.

## 9. Test plan

Add focused tests in existing test areas, using SQLite in-memory with `EnsureCreated` and `UseSnakeCaseNamingConvention()` for persistence tests.

1. Domain transition tests for valid/invalid cancellation, reason requirement, started-state requirement, all three alternatives, replacement blocking, no completion-on-cancel, and pickup confirmation — plus the new active-position completion gate and the tombstone-aware predecessor gate (a tombstoned step must not block successors).
2. Work-record tests proving old trip/shipment is cancelled with reason and retains numbering-party identity; new record belongs to the selected party.
3. Persistence tests proving old steps, histories, trips, and shipments remain present after reassignment; the old step's `StepOrder` is tombstoned (`-<step.Id>`), the replacement occupies the freed rank, and the unique `(OrderId, StepOrder)` index survives via the reordering staging pathway.
4. Handler tests for actor/auth, missing order/party, stale expected version, atomic persistence failure, and duplicate submission protection where applicable.
5. Read-model/history tests proving events are visible in order history and old party snapshots remain stable.
6. UI/component validation where existing test setup supports it; otherwise validate server command and render behavior manually during implementation.
7. Run the repository-required sequence from `AGENTS.md`: restore, Release build, Release tests with `--no-build`, and the Node local-time test. Do not alter CI release/tag behavior.

## 10. Explicit non-goals

- Do not allow arbitrary post-start edits through the ordinary order editor.
- Do not delete or rewrite old steps, trips, shipments, numbering, history, or financial rows.
- Do not use failure as a bypass; failure remains distinct from cancellation.
- Do not automatically complete an order upon reassignment or pickup route selection.
- Do not change the behavior of administrative direct-delivery override except to ensure it remains compatible with new history and replacement state.
- Do not add JavaScript, runtime font fetching, secrets, or unrelated UI redesign.

## 11. Locked implementation decisions (owner answers, 2026-11-07)

1. **Chain completion rule (active-position gate).** Delivery is decided by an active-position predicate, not the current `_deliverySteps.All(step => step.Status == DeliveryStatus.Completed)`: ignoring tombstoned (cancelled/superseded) steps, the order becomes Delivered when the remaining steps in execution order are all Completed. Orders without tombstones behave exactly as today.
2. **Rank strategy = tombstone rewrite.** Inside the confirmed movement's transaction the cancelled old step's persistent `StepOrder` is rewritten to a negative tombstone (`-<step.Id>` — per-order collision-free under the unique `(OrderId, StepOrder)` index), the replacement step occupies the freed old rank, and the original rank is preserved in the movement history row. Reuse the transaction staging precedent `OrderRepository.PrepareDeliveryStepReorderingAsync` (negative-rank sweep + EF original-value sync) for conflict-free reordering. One tombstone per cancellation — no pre-reserved spare ranks; re-cancelling replacement #1 repeats the same cancel + replace operation.
3. **Progression gate must be active-steps aware.** `AllowedStepStatuses` and its predecessor filter (`previous.StepOrder < step.StepOrder && previous.Status != Completed`) must ignore tombstoned steps; otherwise a tombstoned row (rank below everything, status ≠ Completed) blocks every successor forever. Genuine pending/failed/in-progress steps and a pending replacement still block successors.
4. **Pickup = reuse the order-level Pickup flow.** No new `WorkRecordType`, no new `NumberSequenceKind`, no new table. Chain + pickup: tombstone + cancel the affected step, set the order route to `DeliveryMethod.Pickup`, transition the order to `ReadyForPickup` in the same confirmed movement, auto-cancelling remaining non-completed chain steps (mirroring today's order-cancellation semantics, including per-work-record cancellation). Direct + pickup: cancel the active work record, set the order route to Pickup; `ReadyForPickup` → explicit receipt confirmation → `Delivered` reuses the existing `TransitionTo` lifecycle, and the confirmation movement is recorded with the `PickupConfirmed` source.
5. **Post-start route mutation via a dedicated internal domain behavior.** Order route fields are currently mutable only through `UpdateHeader` behind `EnsureEditable()`. The reassignment flow uses a new internal, fully validated mutator on `Order` (started-state, valid party pairing, Version bump) that is never exposed to ordinary editing.
6. **Two new `OrderHistorySource` values: `DeliveryRouteReassigned` and `PickupConfirmed`.** The movement row (new source) carries the required reason plus old route type / old party id+name snapshot / old rank (chain) and the new route type / new party id+name snapshot. Step and work-record status rows written in the same movement keep their existing sources (genuine status transitions); correlation is by identical `OrderVersion` + `ChangedAtUtc` inside the movement — no new correlation column.
7. **General history feed merges movement rows.** The stepId-less history query unions order-status history with the movement history so the full audit trail is discoverable in one feed; step panels keep per-step events. Never mislabel legacy rows.
8. **UI wording.** Dialog title «إلغاء التكليف وإعادة التوجيه» — a single atomic movement (one submit). Success toast «تم إلغاء التكليف وإعادة التوجيه بنجاح». History display texts derive from the source descriptions (Arabic `[Description]` attributes).
