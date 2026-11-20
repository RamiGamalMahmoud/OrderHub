# Domain rules

This document describes invariants enforced by `OrderHub.Domain` and the handlers that
call it. Values are stored in UTC; displayed times are localized elsewhere.

## Orders

- A new order starts in `Processing` with `Version = 1`.
- Header/items are editable only while `OrderStatus == Processing` (`CanEdit`). Every
  edit bumps `Version`, and callers pass the expected version for optimistic concurrency.
- `Delivered` and `Cancelled` are final; no further transitions or edits are allowed.
- Reasons are trimmed, limited to 200 characters, and required for `Cancelled` and
  `DeliveryFailed` transitions and for failed/cancelled work records.
- Cancelling an order also cancels every active delivery step and every pending
  shipment/trip in one save.

### Allowed order transitions

| Current | Delivery method | Allowed next |
| --- | --- | --- |
| `Processing` | `Pickup` | `ReadyForPickup` |
| `Processing` | `ShippingCompany` | `Shipped` |
| `Processing` | `DeliveryMan` / `DeliveryChain` | `OutForDelivery` |
| `ReadyForPickup` | `Pickup` | `Delivered` |
| `OutForDelivery` | `DeliveryMan` | `Delivered`, `DeliveryFailed` |
| `Shipped` | `ShippingCompany` | `Delivered`, `DeliveryFailed` |
| `DeliveryFailed` | `DeliveryMan` | `OutForDelivery` (retry) |
| `DeliveryFailed` | `ShippingCompany` | `Shipped` (retry) |
| any non-final | any | `Cancelled` |

A `DeliveryChain` order does not transition directly from `OutForDelivery`; completing
all steps closes the order, and `DeliverDirectly` closes it with outcomes for the
incomplete steps.

`Processing` can move forward only when delivery readiness passes:

- a delivery method must be chosen;
- `DeliveryMan` needs a deliveryman, `ShippingCompany` needs a carrier;
- `DeliveryChain` needs at least one active step with unique ranks and a valid handler
  per step (exactly one of deliveryman/carrier).

### Delivery chain

- Active steps are those with `StepOrder > 0`. Superseded steps are tombstoned with a
  negative `StepOrder` and status `Cancelled`.
- A step can start (`InProgress`) only after the previous active step is `Completed`.
- Step transitions: `Pending`/`Failed` → `InProgress`; `InProgress` → `Completed` or
  `Failed`. Failure requires a reason.
- When every active step is `Completed`, the order becomes `Delivered` with source
  `LastStepCompleted`.
- Reassigning a step requires `DeliveryChain`, a non-final order, a reason, and a
  `Pending`/`InProgress`/`Failed` step. It cancels the old step and creates a
  replacement at the same rank, recording an `OrderDeliveryMovement` with the old and
  new party. Reassigning to `Pickup` cancels the remaining steps and moves the order to
  `ReadyForPickup`.
- Cancelling a step requires a reason and is rejected when it is the last remaining
  step.
- Direct delivery (`DeliverDirectly`) requires `OutForDelivery` on a delivery chain and
  a reason; it records outcomes for every incomplete step and closes the order as
  `Delivered`.

## Work records and numbering

Work records are supplier supplies, shipments, delivery trips, and stock issue orders.
They share `IWorkRecord`:

- A record is created `Pending`, receives one number, and can then move to `Completed`,
  `Cancelled`, `Failed`, or `PartiallyExecuted`. Only `Pending` records may transition;
  failed/cancelled transitions require a reason.
- Numbers are monthly and scoped by `NumberSequenceKind` + party:
  - supplier supply → supplier;
  - shipment → shipping carrier;
  - delivery trip → deliveryman;
  - stock issue order → warehouse.
- Month/year are evaluated in `Asia/Riyadh`, not UTC.
- The number is assigned once, inside the same transaction that saves the record.
  Retrying a failed attempt creates a new record with a new number; the failed record
  keeps its number.
- Status history rows are append-only and are written atomically with the status change.
- Updating status or reading records never allocates another number.

## Inventory

### Balances

`StockBalance` is a cached projection per warehouse/product:

- `StockOnHand`, `ReservedOnHand`, `DamagedQty`, and computed
  `Available = StockOnHand - ReservedOnHand`.
- `Reserve` cannot exceed `Available`; `ApplyOut` cannot push `StockOnHand` below
  `ReservedOnHand`; `Consume` removes reserved quantity from both reserved and on-hand;
  `Release` only removes reserved quantity; damaged disposal cannot exceed damaged
  quantity.
- Every mutation increments `Version`.

### Immutable ledger

- Every stock change is an `InventoryMovement` with a direction, type, status, actor,
  timestamp, and reference to its source document.
- Movements and stock delivery records are append-only; `AppDbContext` rejects updates
  and deletes.
- A correction is a new `Reversal` movement (or an adjustment), never an edit.

### Order allocations

- Allocations connect an order item to a warehouse (reserved stock) or a supplier
  (expected supply). Statuses are `Active`, `Cancelled`, `Released`, `Consumed`.
- Saving a final order reserves warehouse allocations in the same transaction. Supplier
  allocations record expected supply without touching warehouse balances.
- `IssuedQty` and `PendingReturnQty` track issued and pending-return quantities.
- When an order closes, the unissued remainder is released and a history row with
  source `StockRelease` is recorded.

### Issue, receipt, count, rebuild

- Stock receipts (`Opening`/`StockIn`) record inbound quantities and optional
  attachments.
- Issue orders are per warehouse and numbered monthly per warehouse. Batches cannot
  exceed the remaining reserved quantity; a final short batch requires a written reason.
- Issue-order cancellation, allocation edits, movement reversals, and damaged disposal
  go through `InventoryApprovalRequest` (`Pending` → `Approved`/`Rejected`/`Stale`)
  and require `Inventory.Approve`.
- A requester cannot approve their own request, except for a movement reversal, which is
  the documented exception. Decisions carry the expected version; a stale decision is
  rejected as a conflict.
- Inventory counts are `Open` → `UnderReview` → `Approved`/`Cancelled`; approval writes
  the adjustment movements.
- A higher-manager rebuild recomputes cached balances from the ledger and active
  allocations. It refuses inconsistent reserved quantities without writing and is
  idempotent when balances already match.

## Pricing and commercial documents

- Each order item has a VAT rate. `PricingCalculator` computes per-line subtotal, VAT,
  and total.
- Order discount is `FixedAmount` or `Percentage` (0–100). A fixed discount cannot
  exceed the pre-VAT subtotal.
- `FinalTotal = products total including VAT − discount + delivery cost`; delivery cost
  and discount cannot be negative.
- Quotations, proforma invoices, and invoices store financial snapshots taken when the
  document is issued, so later catalog or order changes do not rewrite history.
- Document numbers are allocated from the `document_sequences` table.

## Roles and permissions

- Permissions are feature/level pairs: `Read`, `Write`, `Approve`.
- Sealed roles are seeded and immutable; custom roles can be renamed and re-permissioned.
- Authorization is enforced from attributes on MediatR requests. A handler called
  directly bypasses it, which is intentional for tests.
