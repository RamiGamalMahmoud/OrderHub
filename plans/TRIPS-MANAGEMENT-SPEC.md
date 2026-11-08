# Standalone Trips Management — Specification

- **Version:** 1.1 (readiness review applied)
- **Status:** Draft for approval
- **Date:** 2026-11-08
- **Repository:** OrderHub
- **Source analysis:** `OrderHub_Trips_Management_Analysis.md`
- **Related:** `plans/DELIVERY-REASSIGNMENT-SPEC.md`
- **Implementation:** Not started. This document is planning only; no production code has been written or changed.
- **Changes in 1.1:** fixed phone snapshot width, added client/supplier/carrier deletion guards, added order resolution and paging for step-bound trips, added page-level permission gating, moved navigation to a dedicated group, replaced the non-existent `Validation` error code, documented the numbering divergence from `DeliveryTrip`, added attempt DB constraints and a one-open-attempt partial index, added `TripOperation` and command receipts, and adopted attempts-as-source-of-truth reporting.

---

## 1. Purpose

Introduce **standalone courier trips** (مشاوير مستقلة) that are created and managed independently of orders. An internal user assigns a trip to a deliveryman, records where it starts and ends, schedules it optionally, and tracks its execution through attempts until it is completed, failed, or cancelled.

The feature must not change the behaviour of orders, delivery steps, shipments, supplies, or the shared work-record pipeline.

---

## 2. Scope

### 2.1 In scope

- A standalone trip header with two party ends (from/to), a deliveryman, a title, an optional description and an optional scheduled date/time.
- One execution attempt at a time, with retry after failure.
- Explicit start, complete, fail, cancel and retry actions.
- Reassignment before the first start, with renumbering.
- Trip-level status history with actor, reason and attempt number.
- Monthly per-deliveryman trip numbering using the existing deliveryman sequence.
- A Trips list with server-side search, filtering and paging.
- Trip details with attempts and history timelines.
- Create/edit screens reachable from the Trips list and the deliveryman details page.
- A new deliveryman details page listing standalone and order-bound trips.
- A new `Trips` permission feature with `Read`/`Write`, navigation entry and role-editor entries.
- Deletion guards for a deliveryman, client, supplier or shipping carrier that is referenced by a standalone trip.
- Unit, application and infrastructure tests.

### 2.2 Out of scope

- Deliveryman login, mobile app, or deliveryman-initiated actions.
- Linking a standalone trip to an order.
- Changes to order-bound `DeliveryTrip` or `Shipment` behaviour.
- Changes to `WorkRecordLifecycle`, `WorkRecordStatusHistory`, `IWorkRecord`, `UnitOfWork` numbering, or `AppDbContext` work-record saving.
- Soft delete, hard delete, archive or restore for trips.
- Branch, warehouse or company-location entities.
- GPS tracking, route optimisation, proof-of-delivery attachments, signatures or photos.
- Notifications (SMS, push, WhatsApp).
- Financial settlement, cash collection or courier accounting.
- Editing or deleting historical attempts and history rows.
- Bulk operations.

---

## 3. Locked decisions

| # | Decision |
|---|----------|
| D1 | Standalone trips are modelled as a new `StandaloneTrip` header with `StandaloneTripAttempt` children. The existing `DeliveryTrip` table is not reused or modified. |
| D2 | The new aggregate does **not** implement `IWorkRecord`; numbering and history are explicit and isolated. |
| D3 | Only the four existing `WorkRecordStatus` values are used: `Pending`, `Completed`, `Cancelled`, `Failed`. No `New` or `InProgress` values are added. |
| D4 | "New" and "in progress" are derived: `Pending` with `StartedAtUtc == null` is new; `Pending` with `StartedAtUtc != null` is in progress. |
| D5 | From a new trip, only **start** or **cancel** are allowed. Completion and failure require a started attempt. |
| D6 | A failed trip may be retried, cancelled with a reason, or left failed indefinitely. |
| D7 | Retry creates a new `Pending` attempt and returns the trip to `Pending`; the failed attempt remains `Failed`. |
| D8 | Every attempt must be started explicitly, including retries. |
| D9 | One trip number is allocated at creation from the existing `NumberSequenceKind.Deliveryman` monthly sequence. |
| D10 | Attempts are numbered `1..n` inside the trip; they do not consume sequence numbers. |
| D11 | Trip history lives in a separate immutable table and records actor, reason, attempt number and event type. |
| D12 | Internal users (admin/dispatcher) operate trips. Deliverymen are data records, not system users. |
| D13 | Permissions are a separate `Trips` feature with `Read`/`Write`. |
| D14 | The deliveryman details page lists **all** trips; order-bound trips are read-only with a link to their order. |
| D15 | Trips may be edited only while new (`Pending` and no attempt started). |
| D16 | The assigned deliveryman may change only before the first start; the trip is renumbered under the new deliveryman's sequence. |
| D17 | From/To are polymorphic party references with snapshots: `Client`, `Supplier`, `ShippingCarrier`, or `External`. Any type is allowed at either end. |
| D18 | An external party requires name, address and phone; location is optional. |
| D19 | A standalone trip never references an order. |
| D20 | There is no deletion. Cancellation with a required reason is the only way to close a trip without completing or failing it. |
| D21 | Trips use optimistic concurrency through an integer `Version` token. |
| D22 | Trips can be created from the Trips page and from the deliveryman details page, using the same editor. |
| D23 | List search is server-side and case-insensitive. |
| D24 | Deleting a deliveryman, client, supplier or shipping carrier that is referenced by a standalone trip is blocked with a clear Arabic message. |
| D25 | Required at creation: deliveryman, from, to, and title. Description is optional. |
| D26 | Scheduled date/time is optional, entered in Riyadh time and stored in UTC. |
| D27 | The list shows the current attempt and total attempt count; details show the full attempt timeline. |
| D28 | Trip numbers are displayed as `#12 · 11/2026`. |
| D29 | The trips section on the deliveryman page requires `Trips.Read`; the page itself remains gated by `Deliverymen.Read`. |
| D30 | This chat is planning only; implementation starts only after explicit approval of this specification. |
| D31 | Attempts are the source of truth for reporting. Status filters and courier counters derive from attempt rows, so a trip whose latest attempt failed is reported as failed even when the header later returns to `Pending` or `Cancelled`. |
| D32 | Reassignment renumbers under the new deliveryman using the **creation** month/year; `SequenceYear` and `SequenceMonth` never change after creation. |
| D33 | Reassignment renumbering deliberately diverges from `DeliveryTrip`, which refuses to move a work-record number to another deliveryman. Standalone trips keep one stable identity and are documented as an intentional exception. |
| D34 | The deliveryman page checks `Trips.Read` through `PermissionAccess` **before** dispatching any trip query. Trip queries keep their request-level `[RequirePermission]`, but that guard must never be the first line of defence for a section inside a page the user is otherwise allowed to open. |
| D35 | Navigation uses a dedicated "المشاوير" group whose view-all link points to `/trips`; trips are not added to the "جهات التعامل" group. |
| D36 | Error codes are limited to the codes already used by the application: `Unauthorized`, `NotFound`, `Conflict`, `InvalidOperation`, `Persistence`. There is no `Validation` code; `PersistenceConflictException` maps to `Conflict`. |

---

## 4. Domain model

### 4.1 Aggregate overview

```
StandaloneTrip (root)
├── TripParty From        (owned value object)
├── TripParty To          (owned value object)
├── StandaloneTripAttempt 1..n
└── StandaloneTripHistoryEntry 1..n (immutable)
```

There is no relationship to `Order`, `OrderDeliveryStep`, `Shipment`, or `Supply`.

### 4.2 StandaloneTrip

| Field | Type | Null | Notes |
|---|---|---|---|
| `Id` | `int` | no | Identity key. |
| `DeliverymanId` | `int` | no | FK to `deliverymen`, `Restrict`. |
| `Deliveryman` | `Deliveryman` | no | Navigation. |
| `Title` | `varchar(150)` | no | Trimmed, 1–150 characters. |
| `Description` | `varchar(1000)` | yes | Trimmed; empty becomes null. |
| `From` | `TripParty` | no | Owned type; columns prefixed `from_`. |
| `To` | `TripParty` | no | Owned type; columns prefixed `to_`. |
| `ScheduledAtUtc` | `timestamptz` | yes | Optional; entered in Riyadh, stored UTC. |
| `Status` | `WorkRecordStatus` | no | Stored as string, max 30. Starts at `Pending`. |
| `StartedAtUtc` | `timestamptz` | yes | Set on the first attempt start; never reset. |
| `StatusChangedAtUtc` | `timestamptz` | no | Updated on every status transition. |
| `TripNumber` | `bigint` | no | Monthly sequence number, > 0. |
| `SequenceYear` | `int` | no | Riyadh year, > 0. |
| `SequenceMonth` | `int` | no | Riyadh month, 1–12. |
| `Version` | `int` | no | Optimistic concurrency token; starts at 1 and increments on every change. |
| `CreatedAt` / `ModifiedAt` | inherited | — | `ModelBase` timestamps. |
| `Attempts` | `IReadOnlyCollection<StandaloneTripAttempt>` | — | Ordered by `AttemptNumber`. |
| `History` | `IReadOnlyCollection<StandaloneTripHistoryEntry>` | — | Ordered by `Id`. |

#### Invariants

1. A trip always has exactly one deliveryman and exactly two party ends.
2. `Status` is one of the four defined values and is never set directly outside the trip lifecycle component.
3. `StartedAtUtc` is null until the first attempt is started; once set it is never cleared.
4. `TripNumber`, `SequenceYear` and `SequenceMonth` are assigned once at creation, except for a permitted reassignment renumber.
5. At most one attempt may be open (`Pending`) at any time.
6. Attempt numbers are sequential starting at 1 and are never reused.
7. A new trip has no attempts.
8. A terminal trip (`Completed`, `Cancelled`) accepts no further actions.
9. History rows are append-only.
10. A trip is never deleted.

### 4.3 TripParty (owned value object)

Used twice: `From` and `To`.

| Field | Type | Null | Notes |
|---|---|---|---|
| `PartyType` | `TripPartyType` | no | `Client`, `Supplier`, `ShippingCarrier`, `External`; stored as string, max 30. |
| `PartyId` | `int` | yes | Null only for `External`; FK is not enforced because the reference is polymorphic. |
| `Name` | `varchar(100)` | no | Snapshot; required. |
| `Phone` | `varchar(30)` | yes | Snapshot; required for external. Sized for `PhoneNumber.FullNumber`, which is `{CountryCode} - {NationalNumber}` and can reach 22 characters. |
| `Address` | `varchar(255)` | yes | Snapshot; required for external. Sized for `Address.FullAddress`, which can reach 253 characters. |
| `Location` | `varchar(255)` | yes | Optional free text (map link), consistent with `Client.Location`. |

#### Snapshot rules

| Party type | `PartyId` | Name | Phone | Address | Location |
|---|---|---|---|---|---|
| `Client` | required | `Client.Name.Value` | `Client.Phone.Number.FullNumber` when present | `Client.Address.FullAddress` when present | `Client.Location` |
| `Supplier` | required | `Supplier.Name.Value` | `Supplier.Phone.Number.FullNumber` when present | `Supplier.Address.FullAddress` when present | null |
| `ShippingCarrier` | required | `ShippingCarrier.Name.Value` | `ShippingCarrier.Phone.Number.FullNumber` when present | `ShippingCarrier.Address.FullAddress` when present | null |
| `External` | null | user input, required | user input, required | user input, required | user input, optional |

Rules:

1. For known party types, the entity must exist and match the selected type; otherwise the save fails with a validation error.
2. Snapshots are refreshed whenever the trip is saved while new.
3. After the first start, snapshots are frozen.
4. Display always uses the snapshot, so later master-data changes do not rewrite trip history.
5. The UI may offer a link to the party's master record, but the link is optional and never required for display.
6. Because `PartyId` is polymorphic and has no foreign key, deleting a referenced client, supplier or shipping carrier is blocked by an application guard before removal (see §8.6).

### 4.4 StandaloneTripAttempt

| Field | Type | Null | Notes |
|---|---|---|---|
| `Id` | `int` | no | Identity key. |
| `TripId` | `int` | no | FK to `standalone_trips`, `Restrict`. |
| `AttemptNumber` | `int` | no | 1..n, unique per trip. |
| `Status` | `WorkRecordStatus` | no | `Pending`, `Completed`, `Failed`, `Cancelled`; stored as string. |
| `StartedAtUtc` | `timestamptz` | yes | Null until the attempt is explicitly started. |
| `EndedAtUtc` | `timestamptz` | yes | Set when the attempt reaches a terminal attempt status. |
| `Reason` | `varchar(200)` | yes | Required for `Failed` and `Cancelled`. |
| `CreatedAtUtc` | `timestamptz` | no | When the attempt row was created. |

Rules:

1. The first attempt is created and started atomically on the first **Start** action.
2. A retry creates the next attempt as `Pending` and not started; the operator must press **Start** on it.
3. A `Pending` attempt with `StartedAtUtc == null` displays as "بانتظار البدء".
4. A `Pending` attempt with `StartedAtUtc != null` displays as "قيد التنفيذ".
5. Completing closes the open attempt as `Completed`.
6. Failing closes the open attempt as `Failed` with a reason.
7. Cancelling the trip closes the open attempt as `Cancelled` with the same reason, if an open attempt exists. A retry attempt that was never started is still closed as `Cancelled`: its `StartedAtUtc` stays null and `EndedAtUtc` records the cancellation time.
8. Attempt rows are never edited or deleted after they reach a terminal status.
9. `Completed` and `Failed` require `StartedAtUtc != null`; both also require `EndedAtUtc != null`.
10. `Cancelled` requires `EndedAtUtc != null` but may have `StartedAtUtc == null` when an unstarted retry attempt is abandoned by cancellation.
11. `Pending` requires `EndedAtUtc == null`; a started pending attempt is the open attempt.
12. At most one `Pending` attempt exists per trip; this is enforced by a partial unique index in addition to the domain invariant.

### 4.5 StandaloneTripHistoryEntry

| Field | Type | Null | Notes |
|---|---|---|---|
| `Id` | `int` | no | Identity key. |
| `TripId` | `int` | no | FK to `standalone_trips`, `Restrict`. |
| `EventType` | `StandaloneTripEventType` | no | See event list below; stored as string. |
| `AttemptNumber` | `int` | yes | Set for attempt-related events. |
| `PreviousStatus` | `WorkRecordStatus` | yes | Null for the creation event. |
| `NewStatus` | `WorkRecordStatus` | yes | Null for edit/reassign events that do not change status. |
| `PreviousDeliverymanId` | `int` | yes | Set for reassignment. |
| `NewDeliverymanId` | `int` | yes | Set for reassignment. |
| `PreviousTripNumber` | `bigint` | yes | Set for reassignment. |
| `NewTripNumber` | `bigint` | yes | Set for reassignment. |
| `Reason` | `varchar(200)` | yes | Required for failed, cancelled, and reassigned events. |
| `ChangedByUserId` | `int` | no | FK to users, `Restrict`. |
| `ChangedByUserName` | `varchar(150)` | no | Snapshot of the acting user. |
| `ChangedAtUtc` | `timestamptz` | no | UTC. |
| `Source` | `varchar(30)` | no | `Web` for this phase. |

#### TripOperation

Every mutating command builds one immutable `TripOperation` value object before touching the aggregate:

| Field | Type | Notes |
|---|---|---|
| `UserId` | `int` | Required; must be positive. |
| `UserName` | `string` | `DisplayName ?? UserName`, with `#{UserId}` as a last-resort fallback; matches `OrderOperation`. |
| `AtUtc` | `DateTime` | UTC, from `TimeProvider`. |
| `Source` | `string` | `Web` for this phase. |

`TripOperation.Create(ICurrentUser user, TimeProvider time)` returns a failure when `user.IsAuthenticated` is false or `user.Id` is null or not positive. The domain never accepts a history entry without a valid operation, so an unauthenticated caller cannot mutate a trip even if a request-level guard is misconfigured.

#### Event types

| Event | When |
|---|---|
| `Created` | Trip created; status `Pending`; number allocated. |
| `AttemptStarted` | First attempt started or a pending retry attempt started. |
| `Completed` | Open attempt and trip completed. |
| `Failed` | Open attempt and trip failed. |
| `Cancelled` | Trip cancelled; open attempt cancelled if present. |
| `Retried` | A new pending attempt is created after failure; trip returns to `Pending`. |
| `Reassigned` | Deliveryman changed before start; trip renumbered. |
| `Edited` | Non-reassignment fields changed while new. |

Rules:

1. History is append-only and protected against update/delete.
2. Every status change writes exactly one history row.
3. A reassignment writes one `Reassigned` row, not an additional `Edited` row.
4. A retry writes `Retried` only; the trip status change is included in that row.
5. Reason is required for `Failed`, `Cancelled` and `Reassigned`; it is optional for `Edited` and absent for other events.

### 4.6 Enums

- `TripPartyType`: `Client`, `Supplier`, `ShippingCarrier`, `External`.
- `StandaloneTripEventType`: `Created`, `AttemptStarted`, `Completed`, `Failed`, `Cancelled`, `Retried`, `Reassigned`, `Edited`.
- `TripDisplayState`: `New`, `InProgress`, `AwaitingRetryStart`, `Failed`, `Completed`, `Cancelled`. Derived only; never stored.
- `WorkRecordStatus` is reused for stored values only. The new aggregate has its own lifecycle component; it does not call `WorkRecordLifecycle`.

---

## 5. Lifecycle

### 5.1 Derived display states

| Stored status | Additional condition | Display (Arabic) | Tone |
|---|---|---|---|
| `Pending` | `StartedAtUtc == null` | جديد | slate |
| `Pending` | `StartedAtUtc != null`, open attempt started | قيد التنفيذ | brand |
| `Pending` | `StartedAtUtc != null`, retry attempt not started | قيد التنفيذ — بانتظار بدء المحاولة | brand |
| `Failed` | — | فشل | rose |
| `Completed` | — | مكتمل | emerald |
| `Cancelled` | — | ملغي | slate |

The rows map to `TripDisplayState` as `New`, `InProgress`, `AwaitingRetryStart`, `Failed`, `Completed`, `Cancelled`. Only `Pending` has two in-progress variants; both keep the stored status `Pending`.

The distinction between "in progress" and "awaiting retry start" is: `Pending` with an open attempt whose `StartedAtUtc != null` is `InProgress`; `Pending` with an open attempt whose `StartedAtUtc == null` (a retry that has not started) is `AwaitingRetryStart`.

### 5.2 Transition table

| Action | From | Guards | To | Attempt effect | History event |
|---|---|---|---|---|---|
| Create | — | Valid input, deliveryman exists, parties valid, number allocated | `Pending` | None | `Created` |
| Edit | `Pending`, new | No attempt started | `Pending` | None | `Edited` |
| Reassign | `Pending`, new | No attempt started, new courier differs, reason required | `Pending` | None; renumber | `Reassigned` |
| Start (first) | `Pending`, new | No attempts | `Pending` | Create attempt 1 and start it | `AttemptStarted` |
| Start (retry) | `Pending`, open attempt not started | Open pending attempt exists | `Pending` | Start that attempt | `AttemptStarted` |
| Complete | `Pending`, in progress | Open started attempt | `Completed` | Close attempt `Completed` | `Completed` |
| Fail | `Pending`, in progress | Open started attempt, reason required | `Failed` | Close attempt `Failed` | `Failed` |
| Cancel | `Pending` (new or in progress) | Reason required | `Cancelled` | Close open attempt `Cancelled` if present | `Cancelled` |
| Cancel | `Failed` | Reason required | `Cancelled` | None | `Cancelled` |
| Retry | `Failed` | No open attempt | `Pending` | Create next attempt `Pending` | `Retried` |
| Any | `Completed` / `Cancelled` | — | — | — | Rejected |

Additional rules:

1. `Complete`, `Fail` and `Start` are rejected unless the trip version matches.
2. `Fail` is rejected when the open attempt has not started.
3. `Complete` is rejected when no attempt is open.
4. `Retry` is rejected when an attempt is already open.
5. Cancelling a failed trip does not create an attempt.
6. A failed trip may remain failed indefinitely.
7. A completed or cancelled trip is terminal.

### 5.3 Numbering

1. At creation, allocate one number with `INumberSequenceService.NextMonthlyAsync(NumberSequenceKind.Deliveryman, deliverymanId, createdAtUtc)`.
2. The sequence month/year is the trip's creation month in `Asia/Riyadh`.
3. The trip stores `TripNumber`, `SequenceYear` and `SequenceMonth`.
4. Display format: `#12 · 11/2026`.
5. Attempts are numbered separately and do not consume sequence numbers.
6. On reassignment before the first start, allocate a new number for the new deliveryman in the **original creation month/year** and void the old number. `SequenceYear` and `SequenceMonth` are never rewritten (D32). If the reassignment happens in a later month, the original month's sequence continues from its stored value; it is not reset to 1.
7. Numbers are not guaranteed to be gapless; voided numbers are not reused.
8. All input is validated, and the deliveryman and party records are resolved, **before** the numbering transaction opens. A validation failure must not consume a sequence number.
9. The allocation and the trip insert happen in one database transaction.
10. A unique index on `(deliveryman_id, sequence_year, sequence_month, trip_number)` enforces uniqueness.
11. No new `NumberSequenceKind` is introduced.
12. The reassignment exception is intentional: `DeliveryTrip` refuses to move a number to another deliveryman, while a standalone trip keeps one identity and is renumbered (D33). This divergence must be stated in the code comment and in the pull request.

### 5.4 Reassignment

1. Allowed only while the trip is new: `Status == Pending`, `StartedAtUtc == null`, and no attempts exist.
2. The new deliveryman must exist and differ from the current one.
3. A reason is required, 1–200 characters.
4. The trip keeps its identity and attempts (none yet), but gets a new number under the new deliveryman. The sequence period remains the trip's creation month/year.
5. History records old/new deliveryman IDs and old/new trip numbers.
6. The UI shows a confirmation dialog before saving.
7. Reassignment is part of the edit screen; there is no separate top-level action.
8. The old number is voided and never reused. This differs from `DeliveryTrip`, which forbids moving a number; the difference is a deliberate product decision for standalone trips.

### 5.5 Cancellation and failure

1. Cancelling requires a reason, 1–200 characters.
2. Cancelling is allowed from `Pending` (new or in progress) and from `Failed`.
3. Failing requires a reason and an open started attempt.
4. Cancelling a trip with an open attempt closes that attempt as `Cancelled` with the same reason and timestamp.
5. There is no delete, archive or restore action.
6. Cancelling a failed trip changes the header to `Cancelled` but does not erase the failed attempt. Attempts remain the source of truth: the trip is still counted as failed in the "failed" filter and in courier failure counters (D31).
7. A trip returns to `Pending` after a retry but still has a failed attempt. The same attempts-as-source-of-truth rule applies: it stays in the failed reporting set even after a later attempt completes, while the current display state shows the latest outcome. This is intentional so failure history is never hidden by a later success.

### 5.6 Concurrency

1. Every mutating command loads the trip with its current `Version`.
2. The expected version is supplied by the client; a mismatch returns a conflict result.
3. A database concurrency exception is translated to `PersistenceConflictException` and surfaced as an Arabic conflict message.
4. The message should read: "تم تعديل المشوار من مستخدم آخر. أعد تحميل البيانات ثم حاول مرة أخرى."

---

## 6. Validation rules

### 6.1 Field rules

| Field | Rule |
|---|---|
| Deliveryman | Required; must exist. |
| Title | Required; trimmed; 1–150 characters. |
| Description | Optional; trimmed; max 1000 characters; empty becomes null. |
| From / To | Required; party type must be defined. |
| Known party (`Client`, `Supplier`, `ShippingCarrier`) | `PartyId` required; entity must exist and match the type; snapshot taken from master data. |
| External party | `PartyId` must be null; name required (1–100); address required (1–255); phone required (6–22 characters, at least 6 digits, optional `+` and separators); location optional (max 255). |
| Phone snapshot | Max 30 characters in storage; known-party snapshots use `PhoneNumber.FullNumber` and may be null when the master record has no phone. |
| ScheduledAt | Optional; valid date/time; stored UTC; displayed in Riyadh. |
| Cancel reason | Required; trimmed; 1–200 characters. |
| Fail reason | Required; trimmed; 1–200 characters. |
| Reassign reason | Required; trimmed; 1–200 characters. |
| Edit note | Optional; max 200 characters. |
| Version | Required for update and state commands. |

### 6.2 Business rules

| Rule | Statement |
|---|---|
| BR-01 | A trip always belongs to exactly one deliveryman. |
| BR-02 | A trip always has exactly one From and one To party. |
| BR-03 | A trip is never linked to an order. |
| BR-04 | Only new trips can be edited. |
| BR-05 | Only new trips can be reassigned. |
| BR-06 | A reassigned trip is renumbered under the new deliveryman. |
| BR-07 | A new trip can only be started or cancelled. |
| BR-08 | Completion and failure require a started attempt. |
| BR-09 | A failed trip can be retried, cancelled, or left failed. |
| BR-10 | A retry creates a new pending attempt and returns the trip to `Pending`. |
| BR-11 | Every attempt must be started explicitly. |
| BR-12 | At most one attempt is open at a time. |
| BR-13 | Attempt numbers are sequential and immutable. |
| BR-14 | Completed and cancelled trips are terminal. |
| BR-15 | History is append-only. |
| BR-16 | Trips are never deleted. |
| BR-17 | A deliveryman with standalone trips cannot be deleted. |
| BR-18 | Read-only users can view trips but cannot mutate them. |
| BR-19 | Trip data on the deliveryman page requires `Trips.Read`. |
| BR-20 | Existing order, shipment, supply, delivery-step and work-record behaviour and tests must remain unchanged. |
| BR-21 | A client, supplier or shipping carrier referenced by a standalone trip cannot be deleted. |
| BR-22 | Failed reporting derives from attempt rows; a trip with any failed attempt appears in failed filters and counters. |
| BR-23 | Reassignment keeps `SequenceYear`/`SequenceMonth` at the creation period and only changes the deliveryman and trip number. |
| BR-24 | The deliveryman page checks `Trips.Read` locally before dispatching trip queries; a request-level denial must not redirect the whole page. |
| BR-25 | All create input is validated before the numbering sequence is consumed. |

---

## 7. Application layer

### 7.1 Folder structure

New vertical slice under `OrderHub.Application/Features/Trips/`:

- `Contracts/`
  - `TripListItem`
  - `TripDetails`
  - `TripPartyData`
  - `TripAttemptData`
  - `TripHistoryEntryData`
  - `TripEditorData`
  - `DeliverymanTripListItem`
  - `OrderBoundTripListItem`
  - `CreateTripReceipt`
  - `ITripReader`
  - `IStandaloneTripRepository`
- `GetTripsPage/`
- `GetTripDetails/`
- `GetTripEditor/`
- `GetDeliverymanTripsSummary/`
- `GetDeliverymanStandaloneTrips/`
- `GetDeliverymanOrderBoundTrips/`
- `GetTripLookups/`
- `CreateTrip/`
- `UpdateTrip/`
- `StartTrip/`
- `CompleteTrip/`
- `FailTrip/`
- `CancelTrip/`
- `RetryTrip/`
- `TripInputValidation.cs`

#### Contract fields

`TripListItem` and `TripDetails` expose the stored status together with derived fields, so the UI never recomputes state:

| Field | Type | Notes |
|---|---|---|
| `Status` | `WorkRecordStatus` | Stored header status. |
| `DisplayState` | `TripDisplayState` | `New`, `InProgress`, `AwaitingRetryStart`, `Failed`, `Completed`, `Cancelled`. Computed in the reader. |
| `CurrentAttemptNumber` | `int?` | The open or latest attempt number; null for a new trip. |
| `AttemptCount` | `int` | Total attempts. |
| `HasFailedAttempt` | `bool` | Drives failed filtering and counters (D31). |
| `Version` | `int` | Required by every write command. |
| `IsEditable` | `bool` | True only when the trip is new. The page combines it with the caller's `Trips.Write` check. |

`TripDetails` additionally exposes the header fields (id, trip number and period, deliveryman id and name, title, description, scheduled/created/status timestamps), the party snapshots, the attempt list and the history list. No contract exposes EF entities.

### 7.2 Queries

| Query | Permission | Result |
|---|---|---|
| `GetTripsPageQuery` | `Trips.Read` | `PagedResult<TripListItem>` |
| `GetTripDetailsQuery` | `Trips.Read` | `TripDetails` |
| `GetTripEditorQuery` | `Trips.Write` | `TripEditorData` |
| `GetDeliverymanTripsSummaryQuery` | `Trips.Read` | Counters only (running, failed-by-attempt, completed, cancelled, total) |
| `GetDeliverymanStandaloneTripsQuery` | `Trips.Read` | `PagedResult<DeliverymanTripListItem>` for one deliveryman |
| `GetDeliverymanOrderBoundTripsQuery` | `Trips.Read` | `PagedResult<OrderBoundTripListItem>` for one deliveryman |
| `GetTripLookupsQuery` | `Trips.Write` | Deliverymen plus client/supplier/carrier lookup data |

The three deliveryman queries are separate on purpose: the page must be able to skip all of them when `Trips.Read` is absent, and each section can page independently without loading the whole courier history.

Lookup sources:

- Clients: existing `IClientReader.GetClientsByNameAsync(searchTerm, take)`.
- Deliverymen: existing `IDeliverymanReader.GetDeliverymenByNameAsync(searchTerm, take)`.
- Shipping carriers: existing `IShippingCarrierReader.GetShippingCarriersByNameAsync(searchTerm, take)`.
- Suppliers: the supplier reader currently exposes only an all-suppliers list. Add `GetSuppliersByNameAsync(string searchTerm, int take, CancellationToken)` to `ISupplierReader`, mirroring the client method, instead of loading every supplier into the editor.

All lookups cap the returned rows (for example 20) and are case-insensitive, so the editor stays responsive as master data grows.

### 7.3 Commands

| Command | Permission | Key guards |
|---|---|---|
| `CreateTripCommand` | `Trips.Write` | Validation; deliveryman exists; parties valid; number allocated atomically. |
| `UpdateTripCommand` | `Trips.Write` | Trip exists; new only; version matches; renumber if deliveryman changes. |
| `StartTripCommand` | `Trips.Write` | New trip with no attempts, or pending retry attempt. |
| `CompleteTripCommand` | `Trips.Write` | In progress; open started attempt. |
| `FailTripCommand` | `Trips.Write` | In progress; open started attempt; reason. |
| `CancelTripCommand` | `Trips.Write` | Pending or Failed; reason; closes open attempt. |
| `RetryTripCommand` | `Trips.Write` | Failed; no open attempt; creates pending attempt. |

There is no delete command.

`CreateTripCommand` returns `Result<CreateTripReceipt>` so the client can navigate to the new details page without a second lookup. The receipt contains `Id`, `Version`, `Status`, `TripNumber`, `SequenceYear` and `SequenceMonth`.

`UpdateTripCommand` returns the new `Version`. When the deliveryman changes it performs the reassignment rules in §5.4 in the same transaction as the edit.

### 7.4 Handler conventions

- Use `IRequestExecutor`, `Result`/`Result<T>`, `ICurrentUser` and `TimeProvider`, following `OrderLifecycleHandlers`.
- Use `[RequirePermission(PermissionFeature.Trips, PermissionLevel.Read/Write)]` on requests.
- Map failures to error codes: `Unauthorized`, `NotFound`, `Conflict`, `InvalidOperation`, `Persistence`. Validation failures use `InvalidOperation`; there is no separate `Validation` code (D36). A `PersistenceConflictException` maps to `Conflict`.
- Build a `TripOperation` from `ICurrentUser` and `TimeProvider` for every mutation; reject an unauthenticated or id-less caller inside the handler as well as through the request guard.
- Return Arabic user-facing messages from validation and domain guards.
- Create and reassign flows must validate every field, resolve the deliveryman and party entities, and reject invalid input **before** opening the numbering transaction. Only then begin the explicit unit-of-work transaction, allocate, add the trip and history, save and commit.
- Status commands add the history row in the same `SaveChanges` call as the status and attempt changes.

### 7.5 Repository

`IStandaloneTripRepository`:

- `Add(StandaloneTrip trip)`
- `GetByIdAsync(int id, CancellationToken)`
- `GetForUpdateAsync(int id, CancellationToken)` — includes attempts and history, tracks the entity.
- `GetForEditAsync(int id, CancellationToken)` — includes attempts.
- `HasTripsAsync(int deliverymanId, CancellationToken)` — used by the deliveryman delete guard.
- `HasPartyTripsAsync(TripPartyType partyType, int partyId, CancellationToken)` — matches either the from or the to end; used by the client, supplier and shipping-carrier delete guards.
- `GetOrderBoundTripsPageAsync(int deliverymanId, int page, int pageSize, CancellationToken)` — direct and step-bound trips, newest first.
- `CountOrderBoundTripsAsync(int deliverymanId, CancellationToken)` — paging total.

Implementation lives in `OrderHub.Infrastructure/Features/Trips/`.

The setup delete handlers (client, supplier, shipping carrier, deliveryman) inject this repository only for the guard query. The interface stays under `Features/Trips/Contracts`; the setup features do not otherwise depend on trip types.

Order-bound rows must resolve their order as `trip.OrderId ?? trip.DeliveryStep.OrderId`. Step-bound `DeliveryTrip` rows have `OrderId == null` by design, so reading `OrderId` alone would produce broken order links. The query projects the resolved order id, order number and status from the owning order in one round trip.

---

## 8. Infrastructure

### 8.1 Tables

#### `standalone_trips`

| Column | Type | Notes |
|---|---|---|
| `id` | integer PK | Identity. |
| `deliveryman_id` | integer | FK, `Restrict`. |
| `title` | varchar(150) | Required. |
| `description` | varchar(1000) | Nullable. |
| `from_party_type` | varchar(30) | Required. |
| `from_party_id` | integer | Nullable. |
| `from_name` | varchar(100) | Required. |
| `from_phone` | varchar(30) | Nullable. |
| `from_address` | varchar(255) | Nullable. |
| `from_location` | varchar(255) | Nullable. |
| `to_party_type` | varchar(30) | Required. |
| `to_party_id` | integer | Nullable. |
| `to_name` | varchar(100) | Required. |
| `to_phone` | varchar(30) | Nullable. |
| `to_address` | varchar(255) | Nullable. |
| `to_location` | varchar(255) | Nullable. |
| `scheduled_at_utc` | timestamptz | Nullable. |
| `status` | varchar(30) | Required. |
| `started_at_utc` | timestamptz | Nullable. |
| `status_changed_at_utc` | timestamptz | Required. |
| `trip_number` | bigint | Required. |
| `sequence_year` | integer | Required. |
| `sequence_month` | integer | Required. |
| `version` | integer | Required; concurrency token. |
| `created_at` | timestamptz | Inherited. |
| `modified_at` | timestamptz | Inherited. |

Indexes and constraints:

- Unique: `(deliveryman_id, sequence_year, sequence_month, trip_number)`.
- Index: `(deliveryman_id, created_at)`.
- Index: `(from_party_type, from_party_id)` and `(to_party_type, to_party_id)` — support the party deletion guards.
- Index: `(status, scheduled_at_utc)`.
- Index: `(created_at)`.
- Check: `trip_number > 0`, `sequence_year > 0`, `sequence_month BETWEEN 1 AND 12`.
- Check: `status IN ('Pending','Completed','Cancelled','Failed')`.
- Check: external rows have no `party_id` and have name/address/phone populated.
- Check: known rows have a positive `party_id`.
- Check: `length(trim(title)) BETWEEN 1 AND 150`.

#### `standalone_trip_attempts`

| Column | Type | Notes |
|---|---|---|
| `id` | integer PK | Identity. |
| `trip_id` | integer | FK, `Restrict`. |
| `attempt_number` | integer | Required; > 0. |
| `status` | varchar(30) | Required. |
| `started_at_utc` | timestamptz | Nullable. |
| `ended_at_utc` | timestamptz | Nullable. |
| `reason` | varchar(200) | Nullable; required for failed/cancelled. |
| `created_at_utc` | timestamptz | Required. |

Indexes and constraints:

- Unique: `(trip_id, attempt_number)`.
- Partial unique index: `(trip_id) WHERE status = 'Pending'` — enforces at most one open attempt per trip.
- Index: `(trip_id, status)`.
- Check: `attempt_number > 0`.
- Check: `status IN ('Pending','Completed','Cancelled','Failed')`.
- Check: `status <> 'Pending' OR ended_at_utc IS NULL`.
- Check: `status = 'Pending' OR ended_at_utc IS NOT NULL` — every terminal attempt has an end timestamp.
- Check: `status NOT IN ('Completed','Failed') OR started_at_utc IS NOT NULL` — completed and failed attempts must have been started; a cancelled attempt may be unstarted.
- Check: `(status NOT IN ('Failed','Cancelled')) OR (reason IS NOT NULL AND length(trim(reason)) BETWEEN 1 AND 200)`.
- Check: `ended_at_utc IS NULL OR started_at_utc IS NULL OR ended_at_utc >= started_at_utc`.

#### `standalone_trip_histories`

| Column | Type | Notes |
|---|---|---|
| `id` | integer PK | Identity. |
| `trip_id` | integer | FK, `Restrict`. |
| `event_type` | varchar(30) | Required. |
| `attempt_number` | integer | Nullable. |
| `previous_status` | varchar(30) | Nullable. |
| `new_status` | varchar(30) | Nullable. |
| `previous_deliveryman_id` | integer | Nullable. |
| `new_deliveryman_id` | integer | Nullable. |
| `previous_trip_number` | bigint | Nullable. |
| `new_trip_number` | bigint | Nullable. |
| `reason` | varchar(200) | Nullable. |
| `changed_by_user_id` | integer | Required; FK to users, `Restrict`. |
| `changed_by_user_name` | varchar(150) | Required. |
| `changed_at_utc` | timestamptz | Required. |
| `source` | varchar(30) | Required; `Web`. |

Indexes and constraints:

- Index: `(trip_id, id)`.
- Check: `event_type` in the defined set.
- Check: failed/cancelled/reassigned events have a reason of 1–200 characters.
- Check: reassigned events have both deliveryman IDs and both trip numbers.
- Foreign key to `users(id)` with `Restrict`.

### 8.2 History protection

Add `StandaloneTripHistoryEntry` to the `ProtectHistory()` guard in `AppDbContext` so modified or deleted history rows throw the existing Arabic immutability error.

### 8.3 EF configurations

New configurations:

- `StandaloneTripConfiguration`
- `StandaloneTripAttemptConfiguration`
- `StandaloneTripHistoryConfiguration`

Follow existing conventions: snake_case naming, `HasConversion<string>()` for status/event enums, `timestamp with time zone`, explicit check constraints, `DeleteBehavior.Restrict`, and owned types for `From`/`To`.

Additional configuration requirements:

- `Version` uses `IsConcurrencyToken().HasDefaultValue(1)`, matching `OrderConfiguration`.
- `Status` and `EventType` use `HasConversion<string>()` with `HasMaxLength(30)`.
- The partial unique index on attempts is declared with `HasFilter("status = 'Pending'")`.
- Owned types use fixed column names (`from_*`, `to_*`) and are required.
- An enum/check mismatch must fail fast: the check constraint values mirror `WorkRecordStatus` and the trip event enum exactly.

### 8.4 Migration

- One new migration adds the three tables, indexes and constraints.
- No existing table is altered.
- No seed data is required.
- `PermissionFeature.Trips` is stored as a string; existing role rows are unaffected.
- After deployment, administrators grant `Trips.Read`/`Trips.Write` through the role editor.
- Deployment of the migration is a manual step, consistent with current project practice.

### 8.5 Numbering

- Reuse `INumberSequenceService` with `NumberSequenceKind.Deliveryman`.
- No new sequence kind and no change to `number_sequences`.
- The create/reassign handler opens a transaction, allocates, adds the trip/history, saves and commits.
- On failure, the transaction rolls back the sequence increment as well as the insert.
- `UnitOfWork` automatic numbering does not apply because the aggregate does not implement `IWorkRecord`.

### 8.6 Deletion guards

Because `PartyId` is polymorphic and has no database foreign key, application guards are the only protection against orphaning a trip's master link. Each guard checks before removal, regardless of the trip's status, and returns an Arabic conflict message:

| Handler | Method | Message |
|---|---|---|
| `DeleteDeliverymanCommandHandler` | `HasTripsAsync(deliverymanId)` | "لا يمكن حذف المندوب لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة." |
| `DeleteClientCommandHandler` | `HasPartyTripsAsync(Client, clientId)` | "لا يمكن حذف العميل لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة." |
| `DeleteSupplierCommandHandler` | `HasPartyTripsAsync(Supplier, supplierId)` | "لا يمكن حذف المورد لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة." |
| `DeleteShippingCarrierCommandHandler` | `HasPartyTripsAsync(ShippingCarrier, carrierId)` | "لا يمكن حذف شركة الشحن لارتباطها بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة." |

- The existing order, delivery-step and shipment checks remain unchanged and run before or alongside the new guard.
- The deliveryman FK with `Restrict` remains the final database safeguard. The party references have no FK, so the guard is mandatory, not optional.
- Each guard is a single indexed `EXISTS` query, not a full load.
- Deleting a party that has no trips is unaffected.
- The guard follows the existing delete-handler pattern (check, then remove, then save). A concurrent trip creation can race the check, exactly as the existing order/delivery-step guards can; the window is accepted for internal admin operations. No foreign key is possible for a polymorphic reference, so the guard is the intended control, not a database constraint.

---

## 9. Web UI

### 9.1 UI conventions

- Arabic RTL, Tajawal, existing design system.
- Page layout in HTML + Tailwind; MudBlazor only for functional/data widgets (grid, dialogs, pickers, validation-bound fields) and restyled through `mud-blazor-overrides.css` and `OrderHubTheme`.
- Informative/helper text is at least 12px; 11px only for chips/badges.
- One primary CTA per page.
- Secondary actions use the compact gray-outlined `.oh-btn` style.
- No dead buttons: every action is wired and shows busy/disabled feedback.
- Status tones follow the existing semantic palette: brand, emerald, amber, rose, slate.
- Tables use `PageScaffold` and `TableFooter` with 1-based display paging.
- Inline banners are used instead of `MudAlert`.

### 9.2 Routes and permissions

| Route | Page | Page permission | Additional gate |
|---|---|---|---|
| `/trips` | Trips list | `Trips.Read` | — |
| `/trips/create` | Create trip | `Trips.Write` | `?deliverymanId=` optional preselection |
| `/trips/{id:int}` | Trip details | `Trips.Read` | — |
| `/trips/{id:int}/edit` | Edit trip | `Trips.Write` | New trips only |
| `/deliverymen/{id:int}` | Deliveryman details | `Deliverymen.Read` | Trips section requires `Trips.Read`, checked locally through `PermissionAccess` before any trip query is dispatched |

### 9.3 Navigation

- Add a **dedicated** "المشاوير" group to the group definitions consumed by both the desktop and mobile navigation variants.
- Do **not** add trips to the "جهات التعامل" group. That group's desktop panel always renders "عرض الكل" from `group.ViewAllHref` (`/clients`), so a Trips item placed there would expose a clients link to a trips-only user.
- Group fields:
  - Title: "المشاوير"
  - Subtitle: "مشاوير مستقلة"
  - Icon: `Icons.Material.Outlined.Route`
  - `ViewAllHref`: `/trips`
  - Item: "كل المشاوير", href `/trips`, tone amber, description "إدارة مشاوير المناديب المستقلة".
  - `Create`: "إنشاء مشوار", href `/trips/create`, shown only with `Trips.Write`.
- Visible only with `Trips.Read`; the create affordance additionally requires `Trips.Write`.
- Active marking: only the section title and the exact current link, never the whole group.
- Label copy uses "المشاوير" in navigation and "المشاوير المستقلة" in page headings, so it is distinguishable from order-bound trips.

### 9.4 Trips list (`/trips`)

Columns:

1. Trip number — `#12 · 11/2026`.
2. Title.
3. Deliveryman.
4. From — name plus party-type chip.
5. To — name plus party-type chip.
6. Scheduled — Riyadh date/time or "—".
7. Status chip — جديد / قيد التنفيذ / فشل / مكتمل / ملغي.
8. Attempts — current attempt / total count; `0` for a new trip.
9. Created — Riyadh date/time.
10. Actions — only with `Trips.Write`; otherwise the column is hidden and the row opens details.

Toolbar:

- Search box: trip number, title, courier name, party name, party phone.
- Status filter (multi-select).
- Deliveryman filter.
- Party type filter (from/to).
- Scheduled date range.
- Created date range.
- Clear filters.
- Refresh.
- Primary CTA: "إنشاء مشوار".

Behaviour:

- Server-side paging, default page size 20.
- Default sort: `CreatedAt` descending, then `Id` descending.
- Case-insensitive search in PostgreSQL (`ILike`/`lower`); provider-compatible fallback for SQLite tests.
- Loading, empty, and error states.
- Read-only users see no create/edit/status actions.
- Row click opens details; action buttons do not trigger row navigation.
- The "فشل" status filter is attempt-based: it matches any trip with at least one failed attempt, even when the header is `Pending`, `Completed` or `Cancelled`. The status chip still shows the current display state, so the two are not contradictory (D31).
- Status filter predicates:

| Filter | Predicate |
|---|---|
| جديد | `status = Pending AND started_at_utc IS NULL` |
| قيد التنفيذ | `status = Pending AND started_at_utc IS NOT NULL` |
| فشل | `EXISTS` a failed attempt for the trip |
| مكتمل | `status = Completed` |
| ملغي | `status = Cancelled` |

- Filters are not mutually exclusive; a trip can match more than one when selected together.

- Numeric values (trip number, attempt counters, dates) render with `dir="ltr"` and `CultureInfo.InvariantCulture`; Arabic labels stay RTL.

Actions per state (write users):

| State | Actions |
|---|---|
| New | Details, Edit, Start, Cancel |
| In progress (started) | Details, Complete, Fail, Cancel |
| In progress (retry pending) | Details, Start, Cancel |
| Failed | Details, Retry, Cancel |
| Completed | Details |
| Cancelled | Details |

### 9.5 Trip details (`/trips/{id:int}`)

Sections:

1. **Header** — trip number, status chip, title, courier, scheduled date/time, created date/time, last status change.
2. **Route** — From card and To card: party type, name, phone, address, location link when present, external badge.
3. **Actions** — per the state matrix; all wired with confirmation dialogs where a reason is required.
4. **Attempts** — table/timeline: attempt number, status, started, ended, reason.
5. **History** — timeline: event, actor, timestamp, attempt number, reason; old/new courier and trip number for reassignment; paged server-side at 20 rows.

Numbers and dates in the header, attempts and history use `dir="ltr"` and invariant formatting. The status chip uses the reader's `DisplayState`, never a locally recomputed state.

Action dialogs:

- **Start** — confirmation; explains that the attempt starts now.
- **Complete** — confirmation; optional note.
- **Fail** — reason required (1–200).
- **Cancel** — reason required (1–200).
- **Retry** — confirmation; creates a pending attempt.
- **Edit** — navigates to the edit page.
- **Reassign** — inside the edit page; courier picker plus required reason.

No delete action.

### 9.6 Create/Edit screen

Fields:

- Deliveryman (required; preselected from `?deliverymanId=`).
- Title (required; max 150).
- Description (optional; max 1000).
- From party (required): type selector, entity picker for known types, external fields when `External`.
- To party (required): same.
- Scheduled date/time (optional; Riyadh).
- Edit note (optional; max 200) on edit.
- Reassign reason (required when the courier changes).

Behaviour:

- One primary CTA: "حفظ المشوار".
- Secondary: "إلغاء" returning to the previous page.
- External party fields appear only for `External`; name, address and phone are required; location is optional.
- Known party pickers load from clients, suppliers and shipping carriers.
- On save, the snapshot is written server-side.
- Server validation errors are displayed inline next to fields.
- A rejected save consumes no trip number; numbering starts only after validation passes.
- Success navigates to details using the create receipt `Id` and shows a snackbar.
- Editing is rejected server-side if the trip is no longer new.

### 9.7 Deliveryman details (`/deliverymen/{id:int}`)

- Page permission: `Deliverymen.Read`.
- Header: name, city, phone, edit action when `Deliverymen.Write`.
- **Trips permission check:** after the page's own permission passes, call `PermissionAccess.IsAllowedAsync(PermissionFeature.Trips, PermissionLevel.Read)`. Only when it returns true dispatch `GetDeliverymanTripsSummaryQuery`, `GetDeliverymanStandaloneTripsQuery` and `GetDeliverymanOrderBoundTripsQuery`.
- Never let the request-level `Trips.Read` guard be the gate for this section. `AuthorizationBehavior` throws and the middleware redirects the whole page to `/access-denied`, which would hide a deliveryman the user is allowed to view.
- If the local check fails, render the deliveryman's basic data only; the trips area is absent, not disabled, and no trip request is sent.
- Counters (standalone trips only, not mutually exclusive):
  - جارية: header `Pending` (new or in progress).
  - فاشلة: trips with at least one failed attempt (D31).
  - مكتملة: header `Completed`.
  - ملغاة: header `Cancelled`.
  - الإجمالي: all standalone trips.
- **Standalone trips** section: paged server-side (default 10 rows); number, title, route summary, status, attempts, scheduled; actions follow the state matrix when `Trips.Write`; create button prefilled with the deliveryman.
- **Order-bound trips** section: paged server-side (default 10 rows); direct order trips and delivery-step trips; read-only; columns include number, resolved order number, route context, status.
- Each order-bound row links to `/orders/{orderId}` using `trip.OrderId ?? trip.DeliveryStep.OrderId`. When neither resolves, show "—" and render no link instead of a broken URL.
- Both sections show their own loading, empty and error states; one failing section must not blank the other.

### 9.8 Status chips

| Display | Arabic | Tone |
|---|---|---|
| New | جديد | slate |
| In progress | قيد التنفيذ | brand |
| Awaiting retry start | قيد التنفيذ — بانتظار البدء | brand |
| Failed | فشل | rose |
| Completed | مكتمل | emerald |
| Cancelled | ملغي | slate |

Attempt chips:

| Attempt state | Arabic | Tone |
|---|---|---|
| Pending, not started | بانتظار البدء | amber |
| Pending, started | قيد التنفيذ | brand |
| Completed | مكتمل | emerald |
| Failed | فشل | rose |
| Cancelled | ملغي | slate |

### 9.9 Permission wiring

- Add `PermissionFeature.Trips`; no schema change is needed because `RolePermissionConfiguration` stores the feature as a string.
- Add `RoleFeatureCatalog` entries in all four switch methods: `Name` → "المشاوير", `Description` → "إدارة مشاوير المناديب المستقلة", `Icon` → `Icons.Material.Outlined.Route`, `Tone` → amber.
- The role editor picks the feature up automatically from the catalog; no manual page change.
- Pages use `[PermissionAuthorize(PermissionFeature.Trips, PermissionLevel.Read/Write)]`.
- Requests keep `[RequirePermission]`; write commands require `Write`.
- Navigation and the deliveryman-page section use `PermissionAccess.IsAllowedAsync`.

---

## 10. Tests

### 10.1 Domain tests

1. Creation starts `Pending`, sets `StartedAtUtc` null, creates no attempts, and writes `Created` history.
2. Derived state is new when `StartedAtUtc` is null and in progress when set.
3. Start creates attempt 1, starts it, sets trip `StartedAtUtc`, and writes `AttemptStarted`.
4. Starting twice is rejected.
5. Complete closes the open attempt and sets the trip to `Completed`.
6. Fail requires a reason, closes the attempt, and sets the trip to `Failed`.
7. Retry from `Failed` creates attempt 2 as `Pending` and sets the trip to `Pending`.
8. A retry attempt must be started explicitly.
9. Cancel from new, in progress and failed requires a reason and closes the open attempt if present.
10. Completed and cancelled trips reject every action.
11. Edit is rejected after start.
12. Reassign is rejected after start or when attempts exist.
13. Reassign renumbers under the new deliveryman and records old/new values.
14. At most one open attempt invariant holds.
15. Attempt numbers are sequential and immutable.
16. History rows are append-only.
17. A mutation without an authenticated `TripOperation` is rejected.
18. A completed or failed attempt cannot be created without `StartedAtUtc`.
19. A `Pending` attempt cannot carry `EndedAtUtc`; a `Cancelled` attempt may have null `StartedAtUtc` but requires `EndedAtUtc`.
20. An external party without name, address or phone is rejected; a known party without a matching entity is rejected.

### 10.2 Numbering tests

21. Numbers are scoped by deliveryman and Riyadh month/year.
22. One trip number is allocated per trip; attempts do not consume numbers.
23. Reassignment allocates a new number and leaves the old one void.
24. Concurrent allocations for one deliveryman and month are unique (file-based SQLite test, following `WorkRecordNumberingTests`).
25. A failed create rolls back both the trip and the sequence increment.
26. Validation failure consumes no sequence number.
27. Reassignment keeps the original `SequenceYear`/`SequenceMonth`.

### 10.3 Application tests

28. Requests without `Trips.Write` are rejected.
29. Validation errors return Arabic messages with the `InvalidOperation` code.
30. Missing trip returns `NotFound`.
31. Stale version returns `Conflict`.
32. The deliveryman delete guard returns the Arabic conflict message.
33. The client delete guard returns the Arabic conflict message.
34. The supplier delete guard returns the Arabic conflict message.
35. The shipping-carrier delete guard returns the Arabic conflict message.
36. Create returns a receipt with id, version, status and trip number; update returns the new version.
37. The failed filter includes a trip whose header is `Pending` after retry but has a failed attempt.
38. The failed filter includes a trip whose header is `Cancelled` or `Completed` but has a failed attempt.
39. Courier counters count failed attempts independently of the header status.
40. A step-bound trip resolves its order id through `DeliveryStep.OrderId`.
41. The order-bound courier list is paged and returns stable totals.

### 10.4 Infrastructure tests

42. Unique index on `(deliveryman_id, sequence_year, sequence_month, trip_number)`.
43. Unique index on `(trip_id, attempt_number)`.
44. Partial unique index rejects a second `Pending` attempt for the same trip.
45. FK `Restrict` on deliveryman, trip and user.
46. Check constraints for status, reason and attempt numbers.
47. Check constraints reject a completed/failed attempt without a start, a pending attempt with an end, and a terminal attempt without an end; a cancelled unstarted attempt is accepted.
48. `ProtectHistory()` rejects updates and deletes of trip history.
49. A 22-character `PhoneNumber.FullNumber` snapshot round-trips without truncation.
50. Migration applies cleanly to an empty database.

### 10.5 Manual UI checks

51. RTL layout, Arabic labels, 12px informative floor.
52. Search is case-insensitive for Arabic and English.
53. Server-side paging and filters.
54. Read-only users see no write actions.
55. Every dialog shows busy state and prevents double submission.
56. Mobile layout for the list, details and editor.
57. A user with `Deliverymen.Read` but without `Trips.Read` sees the deliveryman page, not `/access-denied`, and no trip requests are sent.
58. Order-bound trips link to the correct order for both direct and step-bound trips.
59. The "المشاوير" nav group's view-all goes to `/trips`, and no Trips item leaks a `/clients` link.
60. Numbers and dates render LTR with invariant formatting inside the RTL page.
61. A retried or cancelled trip with a failed attempt appears in the failed filter while showing its current display state.

---

## 11. Corrections to the source analysis

| # | Source claim | Codebase reality | Specification outcome |
|---|---|---|---|
| 1 | A standalone trip is a `DeliveryTrip` with `OrderId == null`. | `OrderId == null` also applies to delivery-step trips; `DeliveryTrip` requires exactly one parent. | New isolated aggregate; `DeliveryTrip` untouched. |
| 2 | Lifecycle is New → InProgress → Failed → Completed/Cancelled. | `WorkRecordStatus` has only Pending/Completed/Cancelled/Failed. | Four statuses; new/in-progress derived from `StartedAtUtc`. |
| 3 | Retry returns the same trip to InProgress. | Work records are immutable attempts; retry creates a new record/row. | New attempt row; trip returns to `Pending`. |
| 4 | History records user, reason and attempt. | `WorkRecordStatusHistory` has no user or attempt. | Separate trip history with actor, reason and attempt number. |
| 5 | Trips support soft delete. | The project has no soft-delete pattern. | No deletion; cancellation with a required reason. |
| 6 | From/To are locations. | No branch/warehouse entity; parties are clients, suppliers and carriers. | Polymorphic party references with snapshots, plus external persons. |
| 7 | A deliveryman can operate the feature. | `Deliveryman` is not a `User` and has no login. | Internal users operate trips. |
| 8 | Trip numbering is independent. | Trips already use the deliveryman monthly sequence. | Reuse `NumberSequenceKind.Deliveryman`; no new kind. |
| 9 | A `Trips` permission exists. | `PermissionFeature` has no `Trips`. | Add `Trips.Read`/`Write`, nav entry and role catalog entries. |
| 10 | Read-only users see disabled actions. | The deliverymen page hides the action column for read-only users. | Hide write actions; rows remain openable. |
| 11 | Standalone trips appear in order summaries. | Order summaries only read order/step-bound trips. | Standalone trips never appear in order summaries. |
| 12 | History and status updates are atomic. | Existing work-record atomicity does not automatically cover a new aggregate. | Explicit transaction for numbering; single-save atomicity for status, attempt and history. |
| 13 | Party phone snapshots fit `varchar(15)`. | `PhoneNumber.FullNumber` is `{CountryCode} - {NationalNumber}` and can reach 22 characters; known-party snapshots use it directly. | Snapshot columns are `varchar(30)`. |
| 14 | Deleting a client, supplier or carrier is safe because the trip stores a snapshot. | `PartyId` is polymorphic with no FK, so deletion would leave a dangling master link even though display still works. | Block deletion of any referenced party with a clear Arabic message (§8.6). |
| 15 | A courier's order-bound trips can be linked through `trip.OrderId`. | Step-bound `DeliveryTrip` rows have `OrderId == null`; the order is reached through `DeliveryStep.OrderId`. | Resolve `trip.OrderId ?? trip.DeliveryStep?.OrderId`; never build links from `OrderId` alone. |
| 16 | A page can protect a section by relying on the request guard. | `AuthorizationBehavior` throws and the middleware redirects the whole page to `/access-denied`. | Check `PermissionAccess` before dispatching section queries; keep request guards as defence in depth. |
| 17 | Trips can sit inside the existing "جهات التعامل" nav group. | That group's desktop panel always renders "عرض الكل" from its own `ViewAllHref` (`/clients`), leaking a forbidden link. | Dedicated "المشاوير" group with `ViewAllHref = /trips`. |
| 18 | Failures can use a `Validation` result code. | The application only uses `Unauthorized`, `NotFound`, `Conflict`, `InvalidOperation`, `Persistence`. | Validation failures return `InvalidOperation`; concurrency exceptions map to `Conflict`. |
| 19 | Renumbering a trip follows the existing delivery-trip rules. | `DeliveryTrip.AssignWorkRecordNumber` throws when the numbering party changes. | Standalone trips intentionally allow renumbering; the divergence is documented in §5.3/§5.4 and must be repeated in code comments and the PR. |
| 20 | Adding a unique open-attempt rule in the domain is sufficient. | Concurrent commands can each pass the in-memory check. | Add a partial unique index on `(trip_id) WHERE status = 'Pending'` as the database backstop. |
| 21 | The order summary courier selector might pick up standalone trips. | The "سجل المهمة" selector reads order-bound `DeliveryTrip`/step work records; standalone trips have no order. | Standalone trips never enter order summaries or the courier task selector; no change to `OrderSummaryDialog`. |

---

## 12. Implementation phases (after approval)

| Phase | Content | Exit criteria |
|---|---|---|
| 0 | Approve this specification. | Owner sign-off. |
| 1 | Domain aggregate, enums, lifecycle, history, numbering hooks, domain tests. | Domain tests green. |
| 2 | EF configurations, migration, history protection, infrastructure tests. | Migration applies; infra tests green. |
| 3 | Application contracts, repository, queries (list, details, courier summary/standalone/order-bound), commands, validation, attempt-based reporting, application tests. | Application tests green. |
| 4 | Trips list, details, create/edit, dialogs, dedicated navigation group, permissions. | Manual UI checklist passes. |
| 5 | Deliveryman details page with local permission gating, order-bound trips and resolved order links, deletion guards for deliveryman/client/supplier/carrier, polish. | Full regression suite green. |

No implementation begins before Phase 0 approval.

---

## 13. Assumptions

1. `Location` is free text up to 255 characters, matching `Client.Location`; it is not a coordinate type.
2. External phone numbers are stored as a single trimmed string of 6–22 characters with at least 6 digits and an optional `+`; the column is `varchar(30)`. Known-party phones use `PhoneNumber.FullNumber`, which can reach 22 characters.
3. Known-party addresses use `Address.FullAddress`, which can reach 253 characters and fits `varchar(255)`; missing phone or address is stored as null even when the master record lacks them.
4. Snapshots are refreshed on every save while the trip is new and frozen after the first start.
5. The list default sort is `CreatedAt` descending; no saved user preferences.
6. Default page size is 20 for the trips list and 10 for each deliveryman-page section.
7. Failed trips remain visible until retried or cancelled.
8. Trip numbers may have gaps after reassignment; the old number is voided and never reused, and the sequence period stays the creation month.
9. No notifications, attachments, GPS, payments or courier settlements are included.
10. No automatic role grants; permissions are assigned through the role editor after deployment.
11. The migration is a manual deployment step.
12. Failed filtering and courier failure counters are attempt-based, so a trip can appear in more than one counter (for example failed and completed after a retry).
13. Reassignment renumbering is a deliberate exception to the `DeliveryTrip` numbering invariant; it is not a bug to be "fixed" later.
14. A user without `Trips.Read` sees the deliveryman page normally; the trips section is omitted and no trip request is dispatched, so there is no `/access-denied` redirect.
15. Navigation uses a dedicated "المشاوير" group; the existing "جهات التعامل" group is unchanged.

---

## 14. Acceptance criteria

1. A user with `Trips.Write` can create a trip with a deliveryman, title, from, to and optional description/schedule; a monthly trip number is assigned and displayed as `#n · MM/yyyy`.
2. A new trip can be edited, reassigned with a reason and renumbering, started, or cancelled with a reason.
3. Starting creates attempt 1; completing or failing closes it; failing requires a reason.
4. A failed trip can be retried, cancelled, or left failed; retry creates a pending attempt that must be started explicitly.
5. Completed and cancelled trips are terminal and expose no write actions.
6. The list supports case-insensitive server-side search, status/deliveryman/party/date filters and server-side paging.
7. Trip details show the route parties, attempts and full history with actor and reason; all party snapshots (including 22-character phone numbers) persist without truncation.
8. The deliveryman details page shows standalone trips and read-only order-bound trips, with working order links for both direct and step-bound trips.
9. `Trips.Read` is required to see trip data anywhere. On the deliveryman page the check is local, so a user without it still sees the courier and is not redirected to `/access-denied`.
10. Read-only users never see create, edit, start, complete, fail, cancel, retry or reassign actions.
11. Concurrent updates produce a clear Arabic conflict message and do not overwrite data.
12. Deleting a deliveryman, client, supplier or shipping carrier with standalone trips is blocked with a clear Arabic message.
13. A failed trip that was retried or cancelled still appears in failed reporting; courier failure counters count failed attempts.
14. Reassignment before start renumbers under the new courier in the original creation month and never rewrites the sequence period.
15. Invalid create input is rejected before numbering, so failed validation never consumes a trip number.
16. Navigation shows a dedicated "المشاوير" group whose view-all points to `/trips`; no Trips-only user can reach `/clients` through a group link.
17. Existing order, shipment, supply, delivery-step and work-record tests remain green.
