# Requirements Document

## Introduction

### Feature Overview

**Feature Name:** Standalone Trips Management (إدارة المشاوير المستقلة)

**Purpose:** Enable internal users to create and manage standalone courier trips that are independent of orders. These trips track deliveryman assignments, execution attempts, and lifecycle status from creation through completion, failure, or cancellation.

**Target Users:** Internal dispatchers and administrators who manage courier operations.

## Requirements

### Functional Requirements

### FR-1: Trip Creation and Basic Management

**FR-1.1** Users with `Trips.Write` permission shall be able to create a standalone trip by specifying:
- A deliveryman (required)
- A title describing the trip (required, 1-150 characters)
- A "from" party (required)
- A "to" party (required)
- An optional description (max 1000 characters)
- An optional scheduled date/time

**FR-1.2** Each trip shall be assigned a unique monthly sequence number scoped to the assigned deliveryman, using the existing `NumberSequenceKind.Deliveryman` numbering system.

**FR-1.3** Trip numbers shall interleave naturally with order-bound delivery trip numbers for the same deliveryman/month, maintaining strict sequential ordering where standalone trips and order deliveries consume consecutive numbers from the shared sequence.

**FR-1.4** Users shall be able to edit trip details (title, description, parties, schedule, deliveryman) only while the trip is "new" (status is Pending, no StartedAtUtc timestamp, and zero attempts exist).

**FR-1.5** Once a trip has been started (first attempt created), all trip fields shall become permanently read-only and no further editing shall be allowed.

### FR-2: Party Management

**FR-2.1** The "from" and "to" parties can each be one of:
- An existing Client
- An existing Supplier
- An existing ShippingCarrier
- An External party (free-form entry)

**FR-2.2** For known party types (Client/Supplier/ShippingCarrier), the system shall capture a snapshot including:
- Entity name
- Full phone number (if present)
- Complete address with city (if present)
- Location (for Clients only)

**FR-2.3** For External parties, users must provide:
- Name (required, 1-100 characters)
- Phone (required, 6-22 characters with at least 6 digits)
- Address (required, 1-255 characters)
- Location (optional, max 255 characters)

**FR-2.4** Party snapshots shall be frozen after the first attempt starts and shall not change even if the master record is later modified.

### FR-3: Trip Lifecycle and Attempts

**FR-3.1** All trips shall start in "Pending" status with no attempts.

**FR-3.2** Users shall explicitly start a new trip, which creates attempt #1 with a started timestamp.

**FR-3.3** Users shall be able to:
- **Complete** an in-progress trip (closes the current attempt as Completed)
- **Fail** an in-progress trip with a required reason (closes the current attempt as Failed)
- **Cancel** a trip with a required reason (closes any open attempt as Cancelled)

**FR-3.4** From a Failed status, users shall be able to:
- **Retry** (creates a new Pending attempt that must be explicitly started)
- **Cancel** with a reason (trip becomes Cancelled)
- Leave it in Failed status indefinitely

**FR-3.5** Each attempt shall be numbered sequentially (1, 2, 3...) and shall have:
- A status (Pending, Completed, Failed, Cancelled)
- Optional started timestamp
- Optional ended timestamp
- Optional reason (required for Failed and Cancelled)

**FR-3.6** At most one attempt may be open (Pending) at any time.

**FR-3.7** Completed and Cancelled trips shall be terminal (no further actions allowed).

### FR-4: Reassignment

**FR-4.1** Users shall be able to reassign a trip to a different deliveryman only while the trip is new (Pending, no StartedAtUtc, zero attempts).

**FR-4.2** Reassignment shall require a reason (1-200 characters).

**FR-4.3** When reassigned, the trip shall receive a new number from the new deliveryman's sequence in the **original creation month/year**, and the old number shall be voided (never reused).

**FR-4.4** The system shall record both the old and new deliveryman IDs, names, and trip numbers in history.

### FR-5: History and Audit

**FR-5.1** The system shall maintain an immutable, append-only history of all trip events including:
- Trip creation
- Attempt started
- Trip completed
- Trip failed (with reason)
- Trip cancelled (with reason)
- Trip retried
- Trip reassigned (with old/new courier info and reason)
- Trip edited

**FR-5.2** Each history entry shall record:
- Event type
- Actor (user ID and name)
- Timestamp (UTC)
- Attempt number (when applicable)
- Reason (when applicable)
- Status transitions (when applicable)
- Source system ("Web" for this phase)

**FR-5.3** History rows shall never be updated or deleted.

### FR-6: Permissions and Authorization

**FR-6.1** A new `Trips` permission feature shall be added with two levels:
- **Read**: View trips list, details, and courier trip sections
- **Write**: Create, edit, reassign, and perform lifecycle actions

**FR-6.2** The permission shall be ungranted by default; administrators shall see trips immediately through the admin bypass.

**FR-6.3** All trip data and actions shall be hidden from users without `Trips.Read`.

**FR-6.4** The deliveryman details page shall check `Trips.Read` locally before rendering trip sections, so users without the permission see the courier data normally without being redirected to access-denied.

### FR-7: Trips List View

**FR-7.1** Users with `Trips.Read` shall access a list view showing all trips regardless of source — standalone trips and order-bound trips (direct order trips and delivery-step trips) — with columns:
- Trip number (formatted as `#12 · 11/2026`)
- Title (for order-bound rows: a «طلب» badge and the order number in place of the title)
- Deliveryman name
- From party (name + type chip) — "—" for order-bound rows
- To party (name + type chip) — "—" for order-bound rows
- Scheduled date/time or "—"
- Status chip (جديد / قيد التنفيذ / فشل / مكتمل / ملغي)
- Attempt count (current / total) — "—" for order-bound rows
- Created date/time

**FR-7.2** The list shall support server-side, case-insensitive search on:
- Trip number (exact match when numeric)
- Title
- Order number (order-bound trips)
- Deliveryman name
- Party names
- Party phone numbers (with separator normalization)

**FR-7.3** The list shall support filtering by:
- Status (multi-select: New, In Progress, Failed, Completed, Cancelled)
- Deliveryman
- Party type (applies to from/to)
- Scheduled date range
- Created date range

**FR-7.4** The list shall default to 25 items per page with user-configurable page size options of 10/25/50 per view, and each view shall remember the user's selection independently.

**FR-7.5** Users filtering by "فشل" (Failed) shall see all trips that have at least one failed attempt, regardless of the trip's current header status (including trips later retried or cancelled).

**FR-7.6** Users filtering the list to "جديد" (New) status shall see only trips that are Pending with no StartedAtUtc and zero attempts, providing a quick view of trips ready to be started. Order-bound trips have no attempts and are treated as In Progress for status filtering.

**FR-7.7** Order-bound trips are displayed in this list for viewing only. The row never links to `/trips/{id}`; opening it routes to the owning order's workflow page (`/orders/{orderId}`) when the user also holds `Orders.Read`. Editing, lifecycle control, and history for order-bound trips remain exclusively on the order workflow.

**FR-7.8** Party-type and scheduled-date filters apply only to standalone trips; order-bound rows are excluded while those filters are active.

### FR-8: Trip Details View

**FR-8.1** Users shall view full trip details including:
- Header (number, status, title, courier, scheduled/created/status-changed times)
- Route cards (from/to with party type, name, phone, address, location link when present, external badge)
- Complete attempts list (number, status, started/ended times, reason)
- Paginated history timeline (event, actor, timestamp, attempt#, reason, old/new courier for reassignment)

**FR-8.2** History shall be paginated server-side with a default of 10 items per page and user-configurable options of 10/25/50, with the selection persisted per view.

**FR-8.3** The details page shall show appropriate actions based on trip state and user permissions.

### FR-9: Trip Editor

**FR-9.1** Users with `Trips.Write` shall access an editor at `/trips/create` and `/trips/{id}/edit`.

**FR-9.2** The editor shall support:
- Deliveryman selection (searchable picker)
- Title and description entry
- Party type selection and entity pickers for known types
- External party free-form entry when "External" is selected
- Optional scheduled date/time entry (Riyadh timezone)
- Optional edit note (max 200 characters) on edit operations

**FR-9.3** When the deliveryman is changed on an existing trip, the editor shall display a reassignment reason field (required, 1-200 characters) and a notice: "سيُسجَّل تغيير المندوب كعملية إعادة إسناد، ولن تُسجَّل التغييرات الأخرى حقلاً بحقل."

**FR-9.4** The editor shall validate all input before allowing save, and a rejected save shall not consume a sequence number.

### FR-10: Deliveryman Details Page

**FR-10.1** A new route `/deliverymen/{id}` shall be created with page permission `Deliverymen.Read`.

**FR-10.2** The page shall display deliveryman basic info with an edit action (reusing the existing `DeliverymanEditorDialog`).

**FR-10.3** When the user has `Trips.Read`, the page shall display:
- **Summary counters** for standalone trips: مفتوحة (Pending), فاشلة (any failed attempt), مكتملة (Completed), ملغاة (Cancelled), الإجمالي (total)
- **Standalone trips section**: paginated table with default 10 items and user-configurable page sizes per view, showing trip number, title, route summary, status, attempts, scheduled date, and actions
- **Order-bound trips section**: paginated read-only table with default 10 items and user-configurable page sizes, showing trip number, order number/link, order status, trip status, created date

**FR-10.4** Order links shall only be rendered when the user has `Orders.Read` permission.

**FR-10.5** When the user lacks `Trips.Read`, the page shall display only the deliveryman info without any trip sections or trip-related requests.

### FR-11: Navigation

**FR-11.1** A new navigation group "المشاوير" shall be added with:
- Title: "المشاوير"
- Subtitle: "مشاوير مستقلة"
- Icon: Route icon
- View all href: `/trips`
- Items: "كل المشاوير" pointing to `/trips`
- Create action: "إنشاء مشوار" pointing to `/trips/create` (visible only with `Trips.Write`)

**FR-11.2** The navigation system shall include `group.Create` in permission evaluation so the create footer button renders for users with `Trips.Write`.

### FR-12: Master Data Protection

**FR-12.1** The system shall block deletion of any deliveryman, client, supplier, or shipping carrier that is referenced by a standalone trip (including historical references in trip reassignment history).

**FR-12.2** Deletion attempts shall return a clear Arabic error message indicating the entity is linked to standalone trips.

### FR-13: Concurrency Control

**FR-13.1** All trip mutations shall use optimistic concurrency control through an integer version token.

**FR-13.2** When a version mismatch is detected, the system shall return the Arabic message: "تم تعديل المشوار من مستخدم آخر. أعد تحميل البيانات ثم حاول مرة أخرى."

### Non-Functional Requirements

### NFR-1: Data Integrity

**NFR-1.1** All trip numbering and trip creation shall execute in a database transaction that commits or rolls back atomically.

**NFR-1.2** Failed validation shall never consume a sequence number.

**NFR-1.3** Party snapshots of up to 253 characters (address), 100 characters (name), and 22 characters (phone) shall persist without truncation.

### NFR-2: Performance

**NFR-2.1** List queries shall support server-side paging and shall not load all trips into memory.

**NFR-2.2** Search predicates shall be compatible with both PostgreSQL (production) and SQLite (tests).

**NFR-2.3** Party deletion guards shall use indexed EXISTS checks, not full table scans.

### NFR-3: Compatibility

**NFR-3.1** The feature shall be additive-only: no existing table, column, constraint, or row shall be altered.

**NFR-3.2** Existing test suites (including work-record numbering, order lifecycle, history protection, and all setup delete flows) shall remain green.

**NFR-3.3** Existing pages (orders, deliverymen list, setup entities, roles editor, navigation) shall be visually and functionally unchanged except for explicit, documented additions.

**NFR-3.4** The `deliverymen` list shall gain only a name link to the new details page; all existing row actions, edit dialog, delete flow, and paging shall remain unchanged.

### NFR-4: Localization and Display

**NFR-4.1** All user-facing text shall be in Arabic with RTL layout.

**NFR-4.2** Numbers and dates shall be rendered in LTR direction with `CultureInfo.InvariantCulant` formatting inside the RTL page.

**NFR-4.3** Status chips shall use the existing semantic palette: brand (active/current), emerald (completed), amber (needs attention), rose (terminated), slate (neutral).

### NFR-5: Rollback Safety

**NFR-5.1** The feature shall support clean rollback by dropping the three new tables and deleting `Trips` permission grants.

**NFR-5.2** Voided sequence numbers shall remain consumed after rollback (gaps are acceptable and harmless).

## Acceptance Criteria

### AC-1: Creation and Numbering
- [ ] A user with `Trips.Write` can create a trip and receives a unique monthly number formatted as `#n · MM/yyyy`
- [ ] Numbers interleave naturally with order-bound trips (standalone trip gets #5, next order delivery gets #6, next standalone gets #7)
- [ ] Failed validation does not consume a sequence number

### AC-2: Lifecycle Actions
- [ ] A new trip can be started, which creates attempt #1
- [ ] An in-progress trip can be completed or failed (with required reason)
- [ ] A failed trip can be retried (creates pending attempt #2), cancelled, or left failed
- [ ] Completed and cancelled trips reject all further actions
- [ ] A retry attempt must be started explicitly (not auto-started)

### AC-3: Editing and Reassignment
- [ ] A new trip (Pending, no StartedAtUtc, zero attempts) can be edited
- [ ] Once started, all trip fields become permanently read-only
- [ ] Reassignment requires a reason, renumbers under the new courier in the creation month, and records old/new courier names and numbers in history

### AC-4: Display and Search
- [ ] The list shows all standalone trips with server-side paging and case-insensitive search
- [ ] Failed filter matches any trip with at least one failed attempt (even if later retried/cancelled)
- [ ] Trip details show complete history with actor, reason, and attempt context
- [ ] Party snapshots persist exactly as captured, including 22-character phone numbers

### AC-5: Deliveryman Page
- [ ] The new `/deliverymen/{id}` page shows courier info plus standalone and order-bound trips when `Trips.Read` is granted
- [ ] Users without `Trips.Read` see only courier info (no redirect to access-denied)
- [ ] Order-bound rows resolve links from both direct (`OrderId`) and step-bound (`DeliveryStep.OrderId`) trips
- [ ] Order links are hidden when user lacks `Orders.Read`

### AC-6: Permissions
- [ ] Users without `Trips.Read` see no trips data or navigation group
- [ ] Read-only users see all trip data but no create/edit/status actions
- [ ] The navigation group includes a create footer button visible only to `Trips.Write` users
- [ ] Filtering to "جديد" (New) status shows only trips ready to be started

### AC-7: Master Data Protection
- [ ] Deleting a deliveryman, client, supplier, or carrier with referencing trips is blocked with an Arabic message
- [ ] Historical courier references (reassignment history) also block deletion

### AC-8: Concurrency and Data Safety
- [ ] Concurrent updates return a conflict error with Arabic reload message
- [ ] The migration is additive-only (three new tables, no altered tables)
- [ ] All existing tests remain green
- [ ] Page size selection is configurable per view and persists independently

## Out of Scope

- Deliveryman login or mobile app
- Linking standalone trips to orders
- Changes to `DeliveryTrip`, `Shipment`, `Supply`, or `WorkRecordLifecycle`
- Soft delete, hard delete, or archive
- GPS tracking, route optimization, or proof-of-delivery attachments
- Notifications (SMS, push, WhatsApp)
- Financial settlement or cash collection
- Bulk operations
- Editing attempts or history rows
- Global page size preferences (each view maintains its own selection)

## Assumptions and Constraints

1. The `NumberSequenceService` already supports deliveryman monthly numbering
2. Party snapshots are built from entities loaded with `GetForUpdateAsync` (includes `Address.City` and `Phone`)
3. Supplier/carrier list items may return empty strings for missing phone/address; these are mapped to null in snapshots
4. Scheduled date is informational only and does not affect numbering or validation
5. Trip numbers may have gaps after reassignment; gaps are acceptable
6. A trip with any failed attempt appears in failed filters/counters even after retry or cancellation
7. Page sizes default to 25 for trips list, 10 for history and deliveryman sections, and each view remembers user's selection independently
8. No automatic role grants; permissions are assigned through the role editor after deployment
9. The migration is a manual deployment step applied before the new build

## Dependencies

- Existing `NumberSequenceKind.Deliveryman` numbering system
- Existing `IClientReader`, `IDeliverymanReader`, `IShippingCarrierReader` for entity lookups
- New `ISupplierReader.GetSuppliersByNameAsync` method (to be added, mirroring client reader)
- Existing `PermissionFeature` enum and `RoleFeatureCatalog`
- Existing `WorkRecordStatus` enum (Pending, Completed, Cancelled, Failed)
- Existing `PageScaffold` and `TableFooter` components
- Existing delete handlers for deliverymen, clients, suppliers, and shipping carriers
