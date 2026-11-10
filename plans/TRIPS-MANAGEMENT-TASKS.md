# Standalone Trips (مشاوير مستقلة) — Executable Task Registry

- **Version:** 1.0 · 2026-11-09
- **Sources of truth:** `plans/TRIPS-MANAGEMENT-SPEC.md` v1.4 (including §16.8) · `AGENTS.md`
- **Status:** implementation complete 2026-11-09 for T01–T16, plus T18 (unified list with order-bound trips). Release build and the full suite are green (277 tests), the Node time test passes, and T17 rollback is documented in `plans/TRIPS-MANAGEMENT-RELEASE.md`. Remaining owner work: apply/rehearse the migration on a populated copy and run the manual UI matrix (checks 51–63, 69). The UI was not exercised in a browser during implementation because no app instance/login was available.
- **Execution mode:** sequential vertical slices. Each task is self-contained: it names its goal, dependencies, locked decisions, exact files, steps, acceptance criteria, targeted tests and verification commands. A task is done only when its own acceptance criteria and the global definition of done pass.
- **Out of scope for this registry:** implementation approval. **Do not start T01 until the owner approves this file.** The specification is already approved as a document, not as permission to code.

---

## 0. How to execute a task

1. Read the task fully, plus the spec decisions it cites. Do not rely on memory of earlier tasks.
2. Implement Domain → Application → Infrastructure → Web in that order, exactly as the task lists files.
3. Run `dotnet restore`, then `dotnet build OrderHub.sln --configuration Release`, then the task's targeted tests, then the full suite.
4. Exercise the task's UI acceptance manually in the browser when the task adds or changes UI. Use a development database and a development copy — never a production database.
5. Mark the checkbox `[x]` and append a one-line deviation note only when the implementation differs from the task.
6. **Do not commit.** At the end, propose a Conventional Commit message (each task has one). The owner runs every commit, tag and push.
7. Never edit a migration that already ran, never edit `SealedRoleSeedSql.V1`, never add JavaScript, never fetch fonts at runtime, never change `DeliveryTrip`, orders, shipments, supplies, delivery steps, documents, pricing or existing numbering behaviour.
8. When the code contradicts a task, stop and record the discrepancy in this file before improvising. The spec's no-harm rule wins over convenience.

### Global definition of done (applies to every task)

- `dotnet build OrderHub.sln --configuration Release` is green with zero new warnings introduced by the task.
- All tests pass, including the pre-existing suite.
- New tables/columns are additive-only. Existing columns, rows, indexes, constraints and enum encodings are untouched.
- All expected failures return `Result`/`Result<T>` with one of `"Unauthorized"`, `"Forbidden"`, `"NotFound"`, `"Conflict"`, `"InvalidOperation"`, `"Persistence"`. No throwing for expected failures.
- Domain invariants throw only as a last line of defence; handlers pre-check and return a `Result` with an Arabic message.
- Permission gates exist in **both** layers: page/attribute for navigation and UX, and handler-level checks for direct invocation.
- Arabic, RTL UI. Numbers and dates rendered with `CultureInfo.InvariantCulture` and `dir="ltr"` where required. No `MarkupString`.
- UI uses `PageScaffold`, `TableFooter`, `.oh-btn` and the one-primary-CTA rule; no dead buttons; every async action has a busy flag.
- No new JavaScript files. No new fonts. No new clipboard interop.
- Migrations are created with `DesignTimeAppDbContextFactrory.cs` and timestamped after `20261108214724_SeedSealedRoles`.
- `StandaloneTrip` does **not** implement `IWorkRecord`; no new `NumberSequenceKind`; no change to `WorkRecordStatus`.
- The feature stays inert for every existing role until `Trips` is granted. Sealed presets are not modified.

### Build / test commands (from `OrderHub/`)

```powershell
dotnet restore OrderHub.sln
dotnet build OrderHub.sln --configuration Release
dotnet test OrderHub.sln --configuration Release --no-build
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~<TestClass>"
node OrderHub.Tests/Web/order-local-time.test.mjs
```

Migration creation:

```powershell
dotnet ef migrations add AddStandaloneTrips --project OrderHub.Infrastructure --startup-project OrderHub.Web --output-dir Migrations
```

---

## 1. Final review — verified facts that constrain the tasks

The spec and §16.8 were re-checked against the current code. Each fact below is evidence-backed and is addressed by the task named on the right.

| # | Verified fact | Evidence | Consequence / task |
|---|---|---|---|
| RF-01 | There is no trips code: no `Features/Trips` folder, no `StandaloneTrip*` type, no trips route. | glob/grep of Application, Infrastructure, Web | T02–T15 create everything from scratch; nothing partial is being extended. |
| RF-02 | `PermissionFeature` has 14 members and is stored as text (`role_permissions.feature varchar(50)`). | `PermissionFeature.cs`, `RolePermissionConfiguration.cs` | Add `Trips` with no migration, no data change (T01). |
| RF-03 | Sealed presets are explicit lists (4 roles, 31 cells) and tests assert that total. | `SealedRolePresets.cs`, `SealedRoleSeedTests.cs` | Do not add `Trips` to presets or seed SQL; tests must stay at 4/31 (T01). |
| RF-04 | `RoleFeatureCatalog` has four hard-coded switches with silent fallbacks; `Tone` returns a Tailwind class string. | `RoleFeatureCatalog.cs` | Four mandatory entries in T01; a partial implementation looks wrong without failing. |
| RF-05 | `NavMenu` builds `_allowedPermissions` from `Groups.SelectMany(group => group.Items)` only; `group.Create` is never evaluated. | `NavMenu.razor:278-289`; desktop/mobile render `group.Create` | Fix the projection in T01, before any trips group exists (T13). |
| RF-06 | `TableFooter` offers only 10/25/50 and clamps its own display; it cannot protect the data layer. | `TableFooter.razor` | Trips list defaults to 25, history to 10, deliveryman sections to 10; queries clamp inputs (T09, T10, T11, T13, T15). |
| RF-07 | Existing party readers project the full address/phone. Clients always have both; suppliers/carriers return `""` for missing values. `ISupplierReader` has no by-name search. | `ClientReader`, `SupplierReader`, `ShippingCarrierReader` | Add `GetSuppliersByNameAsync` mirroring `ClientReader`; map `""` to `null` in snapshots (T06, T10). |
| RF-08 | `IClientRepository`/`ISupplierRepository`/`IShippingCarrierRepository.GetForUpdateAsync` include `Address.City` and `Phone`; `FindAsync` does **not**. | `ClientRepository.cs:59-68`, `SupplierRepository.cs:19-28`, `ShippingCarrierRepository.cs:18-26` | Snapshot handlers must use `GetForUpdateAsync`, never `FindAsync`; deliverymen only need `FindAsync` (T06, T07). |
| RF-09 | `NextMonthlyAsync` accepts only Supplier/Deliveryman/ShippingCarrier, converts to Asia/Riyadh, and updates the sequence through raw SQL in the current transaction. | `NumberSequenceService.cs` | `Deliveryman` is valid; pass a UTC `DateTimeOffset`; save inside the transaction (T06, T07). |
| RF-10 | `ModelBase.CreatedAt` is a `DateTime` set from `DateTime.UtcNow` in the constructor; `UpdateCreationDate(DateTime?)` is public. | `ModelBase.cs` | Call `UpdateCreationDate(clock.GetUtcNow().UtcDateTime)` right after construction (T06). |
| RF-11 | `DeliveryTrip.NumberingPartyId` is NOT NULL, has a `> 0` check and an FK to deliverymen; step-bound trips have `OrderId == null`. | `DeliveryTripConfiguration.cs`, `DeliveryTrip.cs` | Order-bound queries filter `NumberingPartyId` and resolve `OrderId ?? DeliveryStep.OrderId`; no null fallback (T05, T11). |
| RF-12 | `ProtectHistory()` is a single `Any` over four history types and runs before work-record saves. | `AppDbContext.cs` | Add `StandaloneTripHistoryEntry` to the existing pattern; do not restructure (T04). |
| RF-13 | Setup delete handlers have named existing guards and messages. | `DeleteDeliverymanCommandHandler.cs`, client/supplier/carrier counterparts | Insert the trips check after the existing guards; keep every existing message (T12). |
| RF-14 | The test project references Application, Domain and Infrastructure only — not Web. | `OrderHub.Tests.csproj` | Catalog, nav, `TableFooter` and page behaviours are manual checks; do not add a Web reference just to unit-test Razor (T01, T13–T16). |
| RF-15 | `IUnitOfWork` exposes Begin/Commit/Rollback; `INumberSequenceService` lives in `Application.Interfaces.Services`. | `IUnitOfWork.cs`, `INumberSequenceService.cs` | Handlers can allocate and transact directly; no new abstraction (T06, T07). |
| RF-16 | `ICurrentUser` lives in `OrderHub.Application.Context`. Domain cannot reference Application. | `ICurrentUser.cs`, project references | `TripOperation` is a Domain record; the `ICurrentUser` factory lives in Application (T02). |
| RF-17 | Latest migration is `20261108214724_SeedSealedRoles`; the design-time factory is misspelled `DesignTimeAppDbContextFactrory.cs`. | `OrderHub.Infrastructure/Migrations`, `OrderHub.Infrastructure` | New migration after that timestamp; use the factory (T04). |
| RF-18 | `/deliverymen` today is a list with an edit dialog and a delete action; rows are not clickable and there is no details page. | `Deliverymen/Index.razor` | Add the details page and a name link; keep the list, dialog, delete and paging unchanged (T15). |
| RF-19 | `DeliverymanEditorDialog` accepts `DeliverymanId`, closes with `DialogResult.Ok(true)` on save. | `DeliverymanEditorDialog.razor` | Reuse it on the details page instead of a second form (T15). |
| RF-20 | `DeliveryTrip` has no route fields; `Order` has a `Client` navigation but no client-name snapshot property. | `DeliveryTrip.cs`, `Order.cs` | Order-bound rows show trip number, resolved order number/status and trip status — not a fabricated route (T11, T15). |

### Review conclusions carried into the plan

- **Two spec refinements are required and are implemented as stated here:**
  1. `TripOperation.Create(ICurrentUser, TimeProvider)` cannot live in Domain (RF-16). The Domain record validates its own fields; an Application `TripOperationFactory` performs the `ICurrentUser` mapping and returns `Result<TripOperation>`.
  2. `TripDetails` carries a **paged** history (`PagedResult<TripHistoryEntryData>`) instead of the full history list, because §9.5 requires server-side paging at 10. No separate history query is added.
- **Validation happens before numbering.** The aggregate is constructed and fully validated first with `TripNumber = 0`; a separate `AssignNumber` call runs inside the transaction. A rejected create therefore cannot consume a sequence value.
- **Snapshots are built from `GetForUpdateAsync`**, which already loads `Address.City` and `Phone`; concatenating the city again would produce `"City - City - Street"`.
- **The edit screen is the only reassignment path.** One save that changes the courier and other fields writes a single `Reassigned` row; the optional edit note is stored in that row's `Reason` column, which the schema already allows.
- **No UI action is rendered before its route or command exists.** The list ships in T13 without create/edit affordances; T14 adds them together with the editor route. This keeps every task free of dead buttons.
- **The trips feature is off by default.** Admins see it through the `IsAdmin` bypass; every other user needs an explicit grant. No seed change.

---

## 2. Dependency graph

```
T01 Permissions + catalog + nav cache
 ├──────────────────────────────────────────────────────────► T13 (nav group)
 └──────────────────────────────────────────────────────────► T15 (local Trips.Read gate)

T02 Primitives ─► T03 Aggregate ─► T04 Persistence ─► T05 Repository
                                                       ├─► T06 Create ─► T07 Update/Reassign
                                                       │                     └─► T08 Lifecycle
                                                       ├─► T09 List read ─────────► T13
                                                       ├─► T10 Details/Editor/Lookups ─► T13, T14
                                                       ├─► T11 Deliveryman reads ─────► T15
                                                       └─► T12 Usage reader + delete guards

T06 ─► T14 (create/edit page)
T07 ─► T14
T08 ─► T13 (action dialogs), T14 (none)
T10 ─► T14
T13 ─► T14 (adds create CTA, Edit action, nav Create)
T11, T14 ─► T15
T01–T15 ─► T16 regression ─► T17 deployment/rollback/acceptance
```

Rules:
- T03 may be written together with T02 in one sitting but must be committed and tested separately.
- T05 is the only place that touches `DeliveryTrip`; T11 only reads it.
- T12 must not be merged into the delete handlers before T05 exists.
- T13 must not add the nav `Create` or the page create CTA; T14 owns them.
- T16 starts only after every UI task is checked.

---

## 3. Tasks

### [ ] T01 — Trips permission, role catalog and navigation permission-cache fix

**Goal:** Make `Trips` a first-class permission feature with an Arabic catalog card, keep it ungranted by default, and fix the navigation permission cache so a create-only group can later render its footer CTA.

**Depends on:** nothing. **Delivers independently:** the roles editor shows «المشاوير»; existing roles keep exactly their current permissions; existing navigation renders identically.

**Locked decisions:** D13, D35, §9.9, §15.1, §16.8 items 1 and 8.

**Files**
- Modify `OrderHub.Domain/Enums/PermissionFeature.cs`
- Modify `OrderHub.Web/Components/Pages/Features/Administration/Roles/Editor/RoleFeatureCatalog.cs`
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor`
- Create `OrderHub.Tests/TripPermissionTests.cs`

**Steps**
1. Add `[Description("المشاوير")] Trips` inside the Logistics block, after `ShippingCarriers` and before the Reference Data block. Do not rename, reorder or renumber existing members; the enum is stored as text, so the declaration position is compile-time only.
2. Add all four `RoleFeatureCatalog` cases:
   - `Name` → `"المشاوير"`
   - `Description` → `"إدارة مشاوير المناديب المستقلة"`
   - `Icon` → `Icons.Material.Outlined.Route`
   - `Tone` → `"bg-amber-50 text-amber-600"` (a Tailwind class string, not a tone token)
   Keep the existing `_` fallback arms unchanged.
3. Fix the navigation projection in `NavMenu.OnParametersSetAsync`:
   - from `Groups.SelectMany(group => group.Items)`
   - to `Groups.SelectMany(group => group.Items.Append(group.Create).OfType<NavItemModel>())`
   Keep `.Select(item => (item.Feature, item.Level)).Distinct()`. `Append` accepts `NavItemModel?`; `OfType<NavItemModel>()` filters the null. This only adds permission evaluations; it cannot hide an existing item.
4. Add `TripPermissionTests`:
   - `Enum.IsDefined(PermissionFeature.Trips)` is true and its `Description` attribute is `"المشاوير"`.
   - `SealedRolePresets.All` contains no `PermissionFeature.Trips` entry (feature stays off by default).
   - The existing sealed-role totals are unchanged: run `SealedRoleSeedTests` without edits and expect 4 roles / 31 cells.
5. Manual checks (the test project has no Web reference):
   - `/admin/roles` as Higher Manager shows the «المشاوير» card with the amber badge and Arabic description.
   - Grant `Trips` at `Read` to a test custom role and confirm it persists after reload; revoke it.
   - Existing groups (الطلبات، المنتجات، جهات التعامل، المستندات، الإدارة) render the same items and the Orders/Products create footer buttons still appear.

**Acceptance criteria**
- `PermissionFeature` has 15 members and no migration is generated or needed.
- The roles editor shows the new card with the correct name, description, icon and amber tone.
- No sealed role gains `Trips`; `SealedRoleSeedTests` passes unchanged.
- The nav permission projection includes `group.Create`; existing groups and their create buttons are visually unchanged.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripPermissionTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~SealedRoleSeedTests"
```

**Traps**
- `RoleFeatureCatalog.Tone` returns a Tailwind class string; `NavItemModel.Tone` is a token such as `"amber"`. Do not mix them.
- Do not add a Web project reference to the test project to test Razor components.
- Do not add `Trips` to `SealedRolePresets` or `SealedRoleSeedSql`; §15.1 deliberately keeps it ungranted.
- Do not remove the duplicated Orders/Products create items from `Items` in this task; that is existing behaviour.

**Proposed commit:** `feat(trips): add trips permission and nav create permission cache fix`

---

### [ ] T02 — Domain primitives: enums, party snapshot, attempt, history, operation

**Goal:** Add the immutable building blocks of the aggregate with their validation, so the trip root and persistence can be built on stable types.

**Depends on:** nothing (T01 is independent). **Delivers independently:** every primitive can be constructed and tested without a database or a trip root.

**Locked decisions:** D3, D11, D17, D18, D25, D31, §4.3–§4.6.

**Files**
- Create `OrderHub.Domain/Enums/TripPartyType.cs`
- Create `OrderHub.Domain/Enums/StandaloneTripEventType.cs`
- Create `OrderHub.Domain/Enums/TripDisplayState.cs`
- Create `OrderHub.Domain/Models/TripOperation.cs`
- Create `OrderHub.Domain/Models/TripParty.cs`
- Create `OrderHub.Domain/Models/StandaloneTripAttempt.cs`
- Create `OrderHub.Domain/Models/StandaloneTripHistoryEntry.cs`
- Create `OrderHub.Application/Features/Trips/Contracts/TripOperationFactory.cs`
- Create `OrderHub.Tests/TripDomainPrimitivesTests.cs`

**Steps**
1. Enums exactly as §4.6:
   - `TripPartyType`: `Client`, `Supplier`, `ShippingCarrier`, `External`.
   - `StandaloneTripEventType`: `Created`, `AttemptStarted`, `Completed`, `Failed`, `Cancelled`, `Retried`, `Reassigned`, `Edited`.
   - `TripDisplayState`: `New`, `InProgress`, `AwaitingRetryStart`, `Failed`, `Completed`, `Cancelled` (derived only, never stored).
   Do not add members to `WorkRecordStatus`.
2. `TripOperation` (Domain record):
   - `int UserId`, `string UserName`, `DateTime AtUtc`, `string Source = "Web"`.
   - Validate: id > 0; name trimmed 1–150; `AtUtc.Kind == DateTimeKind.Utc`; source non-empty.
   - No reference to `ICurrentUser` (RF-16).
3. `TripOperationFactory` (Application):
   - `Result<TripOperation> Create(ICurrentUser user, TimeProvider clock, string source = "Web")`.
   - Return `Unauthorized` with an Arabic message when `!user.IsAuthenticated` or `user.Id` is null/non-positive.
   - Name = `DisplayName ?? UserName ?? $"#{id}"`, trimmed and capped at 150.
   - Time = `clock.GetUtcNow().UtcDateTime`.
4. `TripParty` (owned value object):
   - Fields: `TripPartyType PartyType`, `int? PartyId`, `string Name`, `string? Phone`, `string? Address`, `string? Location`.
   - Private parameterless constructor for EF; static factories:
     - `FromClient(Client client)`, `FromSupplier(Supplier supplier)`, `FromShippingCarrier(ShippingCarrier carrier)`, `External(string? name, string? phone, string? address, string? location)`, and a generic `Known(TripPartyType type, int id, string? name, string? phone, string? address, string? location)` used by the factories and tests.
   - Known-party factories read `Name.Value`, `Phone?.Number.FullNumber`, `Address?.FullAddress` (which already includes the city), and `Client.Location` only for clients.
   - Normalise every string: trim; empty → `null` for nullable fields. This is where supplier/carrier `""` becomes `null`.
   - Validation:
     - known type → `PartyId > 0`; name 1–100; phone ≤30; address ≤255; location ≤255.
     - `External` → `PartyId == null`; name 1–100; phone 6–22 characters, at least 6 digits, characters limited to digits, whitespace, `+`, `-`, `(`, `)`; address 1–255; location ≤255.
     - reject a known type with a null id and `External` with an id.
   - No public setters and no mutation methods; the snapshots freeze once stored.
5. `StandaloneTripAttempt`:
   - Fields per §4.4; private parameterless constructor.
   - Static `Pending(int attemptNumber, DateTime createdAtUtc, DateTime? startedAtUtc = null)`.
   - Behaviours: `Start(DateTime atUtc)`, `Complete(DateTime atUtc)`, `Fail(string reason, DateTime atUtc)`, `Cancel(string reason, DateTime atUtc)`.
   - Guards per §4.4 rules 9–12: terminal attempts reject further changes; `Completed`/`Failed` require a start; `Cancelled` may be unstarted but requires an end; `Pending` has no end; reason required for `Failed`/`Cancelled` (1–200); end ≥ start.
   - No public setters; no delete.
6. `StandaloneTripHistoryEntry`:
   - Fields per §4.5 including `PreviousDeliverymanName`/`NewDeliverymanName`.
   - Private constructor plus one factory per event type. Each factory enforces:
     - `ChangedByUserId > 0`, actor name 1–150, `ChangedAtUtc` UTC, `Source = "Web"`.
     - `Failed`/`Cancelled`/`Reassigned` require a trimmed reason of 1–200.
     - `Reassigned` requires both courier ids, both courier names and both trip numbers.
     - `Created`: previous status null, new status `Pending`.
     - Header status columns are populated only for `Created`, `Completed`, `Failed`, `Cancelled`, `Retried`; null for `AttemptStarted`, `Reassigned`, `Edited`.
   - No public setters, no update methods; append-only by construction.
7. Tests (`TripDomainPrimitivesTests`) cover:
   - all four party factory paths, including the city appearing exactly once in the address;
   - external party validation matrix (missing name/phone/address; short phone; letters in phone; an id with `External`);
   - `""` from supplier/carrier becomes `null`;
   - length limits for name/phone/address/location;
   - attempt transition matrix (started/complete/fail/cancel), reason requirements, end/start ordering, terminal immutability;
   - history factory required-field matrix, including both courier names on `Reassigned`;
   - `TripOperation` rejects id 0, blank name, non-UTC time;
   - `TripOperationFactory` rejects an unauthenticated caller and falls back to `#id`.

**Acceptance criteria**
- Every primitive is immutable after construction and has no public setters.
- External parties cannot be created without name, phone and address; known parties cannot exist without a positive id.
- Attempt and history invariants from §4.4/§4.5 are enforced in code and covered by tests.
- Domain has no reference to Application; `TripOperationFactory` is the only `ICurrentUser` consumer.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripDomainPrimitivesTests"
```

**Traps**
- The event enum is `StandaloneTripEventType`, not `TripEventType`.
- Do not add `New` or `InProgress` to `WorkRecordStatus`; both are display states.
- Do not put `Create(ICurrentUser, TimeProvider)` on the Domain record (RF-16).
- `Address.FullAddress` already contains the city. Do not concatenate it again.
- Do not make `TripParty` a mutable class with public setters; EF needs only the private parameterless constructor.

**Proposed commit:** `feat(trips): add standalone trip domain primitives`

---

### [ ] T03 — `StandaloneTrip` aggregate: creation, edit/reassign, lifecycle, derived state

**Goal:** Implement the trip root with the complete transition table, append-only history, version increments and the two-phase number assignment used by the create/reassign handlers.

**Depends on:** T02. **Delivers independently:** every lifecycle rule from §10.1 can be tested in memory; no database yet.

**Locked decisions:** D3–D8, D15, D16, D20, D21, D31–D33, §4.2, §5.1–§5.6.

**Files**
- Create `OrderHub.Domain/Models/StandaloneTrip.cs`
- Create `OrderHub.Domain/Models/StandaloneTrip.Lifecycle.cs` (match the existing `Order.Lifecycle.cs` split convention)
- Create `OrderHub.Tests/StandaloneTripDomainTests.cs`

**Steps**
1. Fields and collections per §4.2:
   - `DeliverymanId`, `Deliveryman` navigation, `Title`, `Description`, `From`, `To`, `ScheduledAtUtc`, `Status`, `StartedAtUtc`, `StatusChangedAtUtc`, `TripNumber`, `SequenceYear`, `SequenceMonth`, `Version`.
   - `IReadOnlyCollection<StandaloneTripAttempt> Attempts` ordered by `AttemptNumber`.
   - `IReadOnlyCollection<StandaloneTripHistoryEntry> History` ordered by `Id`/insertion.
   - Private parameterless constructor and a private mutable list for history/attempts.
2. `static StandaloneTrip Create(int deliverymanId, string? title, string? description, TripParty from, TripParty to, DateTimeOffset? scheduledAtUtc, TripOperation operation)`:
   - Validate: deliveryman id > 0; title trimmed 1–150; description trimmed ≤1000 with empty → null; from/to non-null; operation non-null.
   - Set `Status = Pending`, `StartedAtUtc = null`, `StatusChangedAtUtc = op.AtUtc`, `Version = 1`, `TripNumber = SequenceYear = SequenceMonth = 0`, `ScheduledAtUtc = scheduledAtUtc?.UtcDateTime`.
   - Append the `Created` history row. No attempts.
3. `void AssignNumber(long tripNumber, int sequenceYear, int sequenceMonth)`:
   - Allowed exactly once; `tripNumber > 0`, `sequenceYear > 0`, `sequenceMonth` 1–12.
   - Does not write history and does not increment `Version` (numbering is part of creation, not a subsequent mutation).
4. Derived helpers:
   - `bool IsNew => Status == Pending && StartedAtUtc == null && Attempts.Count == 0`.
   - `StandaloneTripAttempt? OpenAttempt => Attempts.SingleOrDefault(a => a.Status == Pending)`.
   - `int? LatestAttemptNumber => Attempts.Count == 0 ? null : Attempts.Max(a => a.AttemptNumber)`.
   - `TripDisplayState GetDisplayState()`:
     - header `Failed`/`Completed`/`Cancelled` map directly;
     - header `Pending` + no open attempt + `StartedAtUtc == null` → `New`;
     - header `Pending` + open attempt not started → `AwaitingRetryStart`;
     - header `Pending` otherwise → `InProgress`.
5. `Update(...)`:
   - Signature: `Update(string? title, string? description, TripParty from, TripParty to, DateTimeOffset? scheduledAtUtc, int deliverymanId, string? newDeliverymanName, long? newTripNumber, string? reassignReason, TripOperation operation)`.
   - Guard: `IsNew` must be true; otherwise throw `InvalidOperationException("لا يمكن تعديل المشوار بعد بدء المحاولة الأولى.")`.
   - Validate title/description/from/to as in `Create`.
   - If `deliverymanId == DeliverymanId`:
     - reject when `newDeliverymanName != null`, `newTripNumber != null` or `reassignReason != null` with `InvalidOperationException("المندوب الجديد مطابق للمندوب الحالي.")`;
     - apply the field changes and append **one** `Edited` row; `Reason = reassignReason` (therefore null for a pure edit).
   - Else:
     - require `newDeliverymanName` 1–100, `newTripNumber > 0`, `reassignReason` 1–200;
     - capture old id/name/number, set `DeliverymanId`, `Deliveryman = null` (or leave the navigation and let EF fix up), `TripNumber = newTripNumber`;
     - append **exactly one** `Reassigned` row with old/new ids, old/new names, old/new numbers and the reason. Do not append an `Edited` row.
   - Update title/description/parties/schedule in both branches.
   - Increment `Version` by exactly one; `ModifiedAt` is updated by `ModelBase.UpdateModificationDate`.
6. `Start(TripOperation op)`:
   - If `Status != Pending` → reject.
   - If no attempts: append attempt 1 as `Pending` and start it at `op.AtUtc`; set `StartedAtUtc = op.AtUtc`.
   - Else if an open attempt exists and is not started: start it at `op.AtUtc`; `StartedAtUtc` is not reset.
   - Else reject `InvalidOperationException("لا يمكن بدء المشوار في حالته الحالية.")`.
   - Append `AttemptStarted` with the attempt number; increment `Version`.
7. `Complete(TripOperation op)`:
   - Require `Status == Pending`, an open attempt, and `openAttempt.StartedAtUtc != null`; otherwise reject.
   - Complete the attempt at `op.AtUtc`; set header `Completed`, `StatusChangedAtUtc = op.AtUtc`.
   - Append `Completed` (`Pending → Completed`); increment `Version`.
8. `Fail(string? reason, TripOperation op)`:
   - Require `Status == Pending`, an open started attempt and a trimmed reason 1–200.
   - Fail the attempt; set header `Failed`, status-changed time.
   - Append `Failed` (`Pending → Failed`); increment `Version`.
9. `Cancel(string? reason, TripOperation op)`:
   - Allowed only from `Pending` or `Failed`; require a trimmed reason 1–200.
   - If an open attempt exists, cancel it with the same reason/time (an unstarted retry attempt is allowed).
   - Set header `Cancelled`, status-changed time; append `Cancelled` (`<old> → Cancelled`); increment `Version`.
   - Cancelling a `Failed` trip must not create or rewrite any attempt.
10. `Retry(TripOperation op)`:
    - Require `Status == Failed` and no open attempt.
    - Append the next attempt as `Pending`, not started; set header `Pending`, status-changed time.
    - Append `Retried` (`Failed → Pending`); increment `Version`.
11. Every mutation requires a non-null `TripOperation` and rejects terminal headers. Do not implement `IWorkRecord`.
12. Tests (`StandaloneTripDomainTests`) cover §10.1 items 1–20 plus §16.1: S-01, S-02, S-03, S-05, S-06, S-07, S-08, S-10A. Specifically:
    - create → Pending, no attempts, `Created` row, version 1;
    - display state for new/in-progress/retry-pending/failed/completed/cancelled;
    - start (first and retry), double start rejection;
    - complete/fail/cancel/retry matrix and terminal rejection;
    - cancel from Failed leaves the failed attempt untouched;
    - edit/reassign rejected after start or when attempts exist (including a retry-pending trip);
    - reassign writes one `Reassigned` row with both names and both numbers; reassign plus other edits still writes one row;
    - version increments by exactly one per mutation;
    - history is exposed read-only and attempts remain sequential/immutable.

**Acceptance criteria**
- The transition table §5.2 is implemented exactly, including "cancel closes only an open attempt".
- `StartedAtUtc` is set once and never reset; `IsNew` requires zero attempts.
- A reassignment writes a single `Reassigned` history row, never an additional `Edited` row.
- The aggregate never allocates a number; `AssignNumber` is the only way to set it.
- No implementation of `IWorkRecord` and no call to `WorkRecordLifecycle`.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StandaloneTripDomainTests"
```

**Traps**
- Do not use `Status == Pending` alone as the definition of "new"; a retried trip is `Pending` with attempts and must be read-only (D15/S-10A).
- Do not reset `StartedAtUtc` on retry; the derived state uses the open attempt, not the header timestamp.
- Do not create an attempt on cancel from `Failed`.
- Do not write an `Edited` row during a reassignment.
- Do not store the edit note in a new column; `Reason` on the `Edited` row is the supported place.
- Do not let `AssignNumber` run twice or increment the version.

**Proposed commit:** `feat(trips): add standalone trip aggregate and lifecycle`

---

### [ ] T04 — EF configurations, history protection and the additive migration

**Goal:** Persist the three tables with the exact columns, indexes and constraints from §8.1, protect trip history from updates/deletes, and add one additive migration.

**Depends on:** T03. **Delivers independently:** SQLite `EnsureCreated` and the PostgreSQL migration both produce the schema; domain tests continue to pass.

**Locked decisions:** D1, D11, D19, D21, §8.1–§8.4, §8.5.

**Files**
- Create `OrderHub.Infrastructure/Configurations/StandaloneTripConfiguration.cs`
- Create `OrderHub.Infrastructure/Configurations/StandaloneTripAttemptConfiguration.cs`
- Create `OrderHub.Infrastructure/Configurations/StandaloneTripHistoryConfiguration.cs`
- Modify `OrderHub.Infrastructure/AppDbContext.cs` (three DbSets + `ProtectHistory`)
- Create migration `OrderHub.Infrastructure/Migrations/<timestamp>_AddStandaloneTrips.cs`
- Create `OrderHub.Tests/StandaloneTripPersistenceTests.cs`

**Steps**
1. `StandaloneTripConfiguration`:
   - table `standalone_trips`;
   - `title varchar(150)` required, `description varchar(1000)` nullable;
   - `OwnsOne(From)` and `OwnsOne(To)` with fixed column names `from_*`/`to_*`, required;
   - `PartyType` as `varchar(30)` string conversion; `PartyId` nullable int;
   - `ScheduledAtUtc`/`StartedAtUtc`/`StatusChangedAtUtc` as `timestamp with time zone`; `Status` as `varchar(30)` string conversion;
   - `TripNumber bigint`, `SequenceYear/SequenceMonth int`, `Version int` `IsConcurrencyToken().HasDefaultValue(1)`;
   - FK `deliveryman_id` → deliverymen `Restrict`;
   - unique index `(deliveryman_id, sequence_year, sequence_month, trip_number)`;
   - indexes `(deliveryman_id, created_at)`, `(from_party_type, from_party_id)`, `(to_party_type, to_party_id)`, `(status, scheduled_at_utc)`, `(created_at)`;
   - checks per §8.1: positive number/year/month, status set, external rows have no party id and populated name/phone/address, known rows have a positive party id, title length.
   - Use only provider-neutral SQL in `HasCheckConstraint` (no `::` casts, no `ILIKE`).
2. `StandaloneTripAttemptConfiguration`:
   - table `standalone_trip_attempts`; FK `trip_id` → standalone trips `Restrict`;
   - unique `(trip_id, attempt_number)`; partial unique `(trip_id)` with `HasFilter("status = 'Pending'")`; index `(trip_id, status)`;
   - checks per §8.1, including the cancelled-unstarted rule and `ended_at_utc IS NULL OR started_at_utc IS NULL OR ended_at_utc >= started_at_utc`.
3. `StandaloneTripHistoryConfiguration`:
   - table `standalone_trip_histories`; FKs to trip and `users` both `Restrict`;
   - `EventType`/`Source` as `varchar(30)` string conversions;
   - indexes `(trip_id, id)`, `(previous_deliveryman_id)`, `(new_deliveryman_id)`;
   - checks per §8.1, including the reason rule and the reassignment-completeness rule.
4. `AppDbContext`:
   - add `DbSet<StandaloneTrip>`, `DbSet<StandaloneTripAttempt>`, `DbSet<StandaloneTripHistoryEntry>`;
   - add `StandaloneTripHistoryEntry` to the existing single `Any` expression in `ProtectHistory()`. Do not restructure the method or change its message.
5. Create the migration with the design-time factory. It must contain only the three `CreateTable` calls, their indexes and constraints. No existing table is altered, no seed SQL is added. Timestamp after `20261108214724`.
6. Tests (`StandaloneTripPersistenceTests`) cover §10.4 items 42–50:
   - unique trip number per courier/year/month;
   - unique `(trip_id, attempt_number)`;
   - a second `Pending` attempt for the same trip is rejected by the partial index;
   - FK `Restrict` on deliveryman, trip and user;
   - status/reason/attempt-number/timing checks, including an accepted cancelled-unstarted attempt;
   - `ProtectHistory` rejects `Modified` and `Deleted` trip history with the existing Arabic immutability message, and still protects the four existing history types;
   - a 22-character phone snapshot round-trips without truncation;
   - `EnsureCreated` produces snake_case tables; all existing tests remain green.
7. Do not run `dotnet ef database update` against a shared database. Empty-database verification is `EnsureCreated`; the populated-copy rehearsal is T17.

**Acceptance criteria**
- The migration is additive: three new tables, indexes and constraints only.
- Every §8.1 index and check exists and is enforced on SQLite in tests.
- Trip history rows cannot be updated or deleted through the context.
- `Version` is a concurrency token with default 1 and is not updated by the database on its own.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StandaloneTripPersistenceTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WorkRecordPersistenceTests"
```

**Traps**
- Partial indexes must use a filter that both PostgreSQL and SQLite accept. `status = 'Pending'` is safe; do not use PostgreSQL-only syntax.
- Owned types must have explicit column names; default shadow columns would break the snapshot contract.
- Do not rewrite `ProtectHistory` into a switch or a list; one added type to the existing pattern is the requirement.
- Do not add a `Deliveryman` FK to `standalone_trip_histories`; the history stores ids and names without live references.
- Do not edit the new migration after it has been applied anywhere, including a developer copy.

**Proposed commit:** `feat(trips): persist standalone trips with history protection`

---

### [ ] T05 — Standalone trip repository and trips feature composition

**Goal:** Provide the persistence surface used by every command, including the order-bound courier query and feature DI registration.

**Depends on:** T04. **Delivers independently:** trips can be added, loaded for edit/update and paged from the database; no commands or UI yet.

**Locked decisions:** D14, D21, §7.5.

**Files**
- Create `OrderHub.Application/Features/Trips/Contracts/IStandaloneTripRepository.cs`
- Create `OrderHub.Infrastructure/Features/Trips/StandaloneTripRepository.cs`
- Create `OrderHub.Infrastructure/Features/Trips/TripsServiceCollectionExtensions.cs`
- Modify `OrderHub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` (call `AddTripsFeature()` after `AddSetupPersistence()`)
- Create `OrderHub.Tests/StandaloneTripRepositoryTests.cs`

**Steps**
1. Interface:
   - `void Add(StandaloneTrip trip)`
   - `Task<StandaloneTrip?> GetByIdAsync(int id, CancellationToken)` (no tracking, with attempts for display)
   - `Task<StandaloneTrip?> GetForUpdateAsync(int id, CancellationToken)` (tracked, attempts + history)
   - `Task<StandaloneTrip?> GetForEditAsync(int id, CancellationToken)` (tracked, attempts)
   - `Task<IReadOnlyList<OrderBoundTripRow>> GetOrderBoundTripsPageAsync(int deliverymanId, int page, int pageSize, CancellationToken)`
   - `Task<int> CountOrderBoundTripsAsync(int deliverymanId, CancellationToken)`
   `OrderBoundTripRow` is an Infrastructure-facing record; the reader in T11 maps it to the Application contract. If a row record in Application is preferred, place it under `Features/Trips/Contracts` and reuse it in both layers.
2. Implementation (Infrastructure feature folder, `AppDbContext` injected directly, matching the Setup repositories):
   - `GetForUpdateAsync` includes `Attempts` and `History`;
   - `GetForEditAsync` includes `Attempts`;
   - order-bound query:
     - `context.DeliveryTrips.Where(t => t.NumberingPartyId == deliverymanId)`;
     - resolve `OrderId = t.OrderId ?? t.DeliveryStep.OrderId`;
     - project order number and status from the resolved order;
     - order newest first by `CreatedAt` then `Id`;
     - guard `page < 1 → 1` and `pageSize <= 0 → 25` even though the reader normalises (RF-06).
   - Never fall back to `Order.DeliverymanId` or `DeliveryStep.DeliverymanId` (RF-11).
3. `TripsServiceCollectionExtensions.AddTripsFeature()` registers the repository now; T09/T12 extend it with readers. Call it from `AddInfrastructure`.
4. Tests:
   - add a trip with an attempt/history and load it for edit/update with the expected includes;
   - order-bound paging returns direct (`OrderId != null`) and step-bound (`OrderId == null`) trips for the numbering party;
   - a trip reassigned at the order level but still owned by the original numbering party is returned for that party and not for the new one (seed two `DeliveryTrip` rows with different `NumberingPartyId`);
   - ordering and stable totals;
   - `page = 0`, negative and `pageSize = 0` do not throw;
   - a trip with neither `Order` nor `DeliveryStep` is impossible through the domain and is not fabricated by the query.

**Acceptance criteria**
- The repository returns tracked update graphs only from `GetForUpdateAsync`/`GetForEditAsync`.
- Order-bound rows always resolve an order id or are excluded; no broken link data is produced.
- Filtering uses `NumberingPartyId` exclusively.
- DI resolves `IStandaloneTripRepository` from `AddInfrastructure`.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StandaloneTripRepositoryTests"
```

**Traps**
- `DeliveryTrip.NumberingPartyId` is non-null; do not add a null/zero fallback.
- Do not modify `DeliveryTrip`, its configuration or its numbering.
- Do not use `AsNoTracking` on the update graph; changes would not persist.
- Do not add a generic repository abstraction; this interface is the only persistence surface the trips feature needs.

**Proposed commit:** `feat(trips): add standalone trip repository`

---

### [ ] T06 — CreateTrip command: validation, snapshots and monthly numbering

**Goal:** Create a standalone trip end to end: validate first, resolve parties with complete snapshots, allocate one deliveryman monthly number inside a transaction, and return a receipt.

**Depends on:** T05. **Delivers independently:** trips can be created and verified through handlers and tests; no UI.

**Locked decisions:** D9, D12, D17, D18, D25, D26, §5.3, §6.1, §7.3, §7.4, §8.5.

**Files**
- Create `OrderHub.Application/Features/Trips/Contracts/TripPartyInput.cs`
- Create `OrderHub.Application/Features/Trips/Contracts/TripInputValidation.cs`
- Create `OrderHub.Application/Features/Trips/Contracts/TripPartySnapshotFactory.cs`
- Create `OrderHub.Application/Features/Trips/CreateTrip/CreateTripCommand.cs`
- Create `OrderHub.Application/Features/Trips/CreateTrip/CreateTripCommandHandler.cs`
- Create `OrderHub.Application/Features/Trips/CreateTrip/CreateTripReceipt.cs`
- Create `OrderHub.Tests/CreateTripHandlerTests.cs`

**Steps**
1. Contracts:
   - `TripPartyInput(TripPartyType PartyType, int? PartyId, string? Name, string? Phone, string? Address, string? Location)`.
   - `TripInputValidation` returns an Arabic failure message for: blank/overlong title, overlong description, a known party with no positive id, an external party missing name/phone/address, an overlong field. Reuse the domain factories for the final guard.
   - `TripPartySnapshotFactory` resolves a known input through the matching repository's `GetForUpdateAsync` and calls the matching `TripParty` factory. Use `FindAsync` only for the deliveryman (RF-08).
2. `CreateTripCommand` record with `[RequirePermission(PermissionFeature.Trips, PermissionLevel.Write)]`:
   `int DeliverymanId, string? Title, string? Description, TripPartyInput From, TripPartyInput To, DateTimeOffset? ScheduledAtUtc`.
3. Handler dependencies: repository, deliveryman/client/supplier/carrier repositories, `INumberSequenceService`, `IUnitOfWork`, `ICurrentUser`, `TimeProvider`.
4. Handler order of operations:
   1. `TripOperationFactory.Create` → `Unauthorized` on failure.
   2. Validate all textual and party input and resolve `from`/`to` snapshots. Any failure returns `InvalidOperation` before any transaction opens.
   3. Resolve the deliveryman (`FindAsync`); missing → `InvalidOperation("المندوب غير موجود.")`.
   4. `StandaloneTrip.Create(...)`, then `trip.UpdateCreationDate(clock.GetUtcNow().UtcDateTime)` immediately (RF-10).
   5. `BeginTransactionAsync`.
   6. `NextMonthlyAsync(NumberSequenceKind.Deliveryman, deliverymanId, new DateTimeOffset(DateTime.SpecifyKind(trip.CreatedAt, DateTimeKind.Utc)))` (RF-09).
   7. `trip.AssignNumber(allocation.Number, allocation.Year, allocation.Month)`.
   8. `repository.Add(trip)`; `SaveChangesAsync` **inside** the transaction; `CommitTransactionAsync`.
   9. Return `CreateTripReceipt(Id, Version, Status, TripNumber, SequenceYear, SequenceMonth)`.
5. Failure handling:
   - `PersistenceConflictException` or `DbUpdateConcurrencyException` → rollback → `Conflict`.
   - `PersistenceException` → rollback → `Persistence` with an Arabic message.
   - Any other exception → rollback and rethrow, so a programming error is not disguised as a user error.
6. Tests:
   - success: Pending, version 1, zero attempts, one `Created` history, number/month/receipt correct;
   - two creates for one courier/month get 1 then 2; a second courier starts at 1 (test 21);
   - validation failure consumes no number: after a rejected create, the next valid create still gets 1 (test 26);
   - create at 23:30 UTC on the last day of the month is numbered in the next Riyadh month per test 66;
   - missing deliveryman, unknown client/supplier/carrier, and a client id labelled as a supplier all return `InvalidOperation`;
   - client/supplier/carrier snapshots contain `"<City> - <Street>"` exactly once; a supplier/carrier with no phone/address stores `null` (tests 65, 77);
   - a 22-character phone snapshot persists intact;
   - unauthenticated → `Unauthorized`; no `Trips.Write` → denied (test 28);
   - rollback test (25): pre-insert a trip occupying the number the sequence will return, attempt a create, expect a `Conflict`/`Persistence` failure, then remove the blocker and confirm the next create receives the same number — proving the sequence increment rolled back.

**Acceptance criteria**
- No sequence value is consumed by a rejected request.
- The trip and its sequence increment commit or roll back together.
- Snapshots are complete (city included) and never truncate.
- The receipt is sufficient to navigate to a details page without a second lookup.
- The handler returns only the documented error codes.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~CreateTripHandlerTests"
```

**Traps**
- Do not allocate before validation and entity resolution (§8.5).
- Do not use `FindAsync` for clients/suppliers/carriers; it omits `Address.City` and `Phone` (RF-08).
- Do not pass a local or unspecified `DateTime` to `NextMonthlyAsync` (RF-09).
- Do not call `SaveChangesAsync` after the commit; the sequence SQL uses `Database.CurrentTransaction`.
- Do not concatenate the city manually; `Address.FullAddress` already contains it.
- Do not map supplier/carrier `""` into the snapshot; the party factory normalises it to `null`.

**Proposed commit:** `feat(trips): create standalone trips with monthly numbering`

---

### [ ] T07 — UpdateTrip command: edit and reassignment

**Goal:** Edit a new trip, refresh only the changed party snapshot, and reassign the courier with a required reason and renumbering in the original creation month.

**Depends on:** T06. **Delivers independently:** edit and reassignment are callable and testable; no UI.

**Locked decisions:** D15, D16, D32, D33, §4.3 rule 2, §5.3, §5.4, §5.6.

**Files**
- Create `OrderHub.Application/Features/Trips/UpdateTrip/UpdateTripCommand.cs`
- Create `OrderHub.Application/Features/Trips/UpdateTrip/UpdateTripCommandHandler.cs`
- Create `OrderHub.Tests/UpdateTripHandlerTests.cs`

**Steps**
1. Command `(int Id, int ExpectedVersion, int DeliverymanId, string? Title, string? Description, TripPartyInput From, TripPartyInput To, DateTimeOffset? ScheduledAtUtc, string? EditNote, string? ReassignReason)` with `[RequirePermission(Trips, Write)]`.
2. Handler:
   1. Build `TripOperation`; load `GetForEditAsync` → `NotFound`; compare `ExpectedVersion` → `Conflict` with the Arabic reload message.
   2. Reject when `!trip.IsNew` → `InvalidOperation("لا يمكن تعديل المشوار بعد بدء المحاولة الأولى.")`.
   3. Validate title/description/schedule and the party inputs (same rules as create).
   4. If `DeliverymanId != trip.DeliverymanId`:
      - resolve the new deliveryman (`FindAsync`) → `InvalidOperation` when missing;
      - require a trimmed `ReassignReason` 1–200 → `InvalidOperation`;
      - allocate **inside a transaction** using the trip's original creation month: `new DateTimeOffset(DateTime.SpecifyKind(trip.CreatedAt, DateTimeKind.Utc))` (D32);
      - pass the new number and the new courier's name to `trip.Update`.
   5. If `DeliverymanId == trip.DeliverymanId`:
      - reject when `ReassignReason` is non-null → `InvalidOperation("المندوب الجديد مطابق للمندوب الحالي.")`;
      - call `trip.Update` with null courier name/number.
   6. Snapshot refresh: for each end, compare `(PartyType, PartyId)` with the stored party. If equal, pass the stored `TripParty` unchanged (ignore the request text). If different, resolve and build a new snapshot. External parties always rebuild from input.
   7. `SaveChangesAsync` (and commit when a transaction was opened). Return the new `Version`.
3. Store `EditNote` in the `Edited` row's `Reason` (1–200, optional). This is the only place an edit note fits without a schema change (§4.5 rule 5).
4. Tests:
   - edit while new updates all editable fields, writes one `Edited` row and increments the version once;
   - edit after start → `InvalidOperation`; edit on a retry-pending trip with attempts → `InvalidOperation` (S-10A);
   - stale version → `Conflict`, no write;
   - reassign while new allocates a new number under the new courier in the original month, writes one `Reassigned` row with old/new ids, names, numbers and reason, and keeps `SequenceYear/Month` unchanged (tests 22, 23, 27);
   - reassignment performed in a later calendar month still uses the original month's sequence (S-21);
   - reassign to the same courier with a reason → `InvalidOperation`, and no sequence value is consumed (S-26A);
   - missing/whitespace reason → `InvalidOperation`, no number consumed (S-26B);
   - reassign after start → `InvalidOperation`;
   - reassign plus title/party changes writes exactly one `Reassigned` row and no `Edited` row (§5.4 rule 9);
   - an untouched party keeps its stored snapshot when a master record changed (S-29); replacing From with another client rebuilds only From (S-30);
   - known → external clears `PartyId`; external → known discards free text (S-31, S-32);
   - unauthenticated → `Unauthorized`; no Write → denied.

**Acceptance criteria**
- Editing is allowed only while the trip is new (`Pending`, `StartedAtUtc == null`, zero attempts).
- Reassignment writes one history row and renumbers atomically under the new courier in the creation month.
- Untouched snapshots never change during an unrelated edit.
- A rejected save consumes no number and leaves the trip/sequence untouched.
- The UI receives the new version.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~UpdateTripHandlerTests"
```

**Traps**
- Do not compare `PartyId` alone; a client and a supplier can share an id. Compare `(PartyType, PartyId)`.
- Do not allocate the new number before the reason and the new courier are validated.
- Do not construct a "first day of month" date for the sequence; the creation instant already determines the Riyadh month.
- Do not write a separate `Edited` row for a reassignment.
- Do not let a retried trip pass the new-only check because its header is `Pending`.
- Do not clear `StartedAtUtc` or rewrite the sequence period.

**Proposed commit:** `feat(trips): edit and reassign standalone trips`

---

### [ ] T08 — Lifecycle commands: start, complete, fail, cancel, retry

**Goal:** Expose the five status commands with version checks, Arabic reasons and exact history rows.

**Depends on:** T07 (same repository/graph and version conventions). **Delivers independently:** every transition is callable through MediatR and testable; no UI.

**Locked decisions:** D5–D8, D10, D20, D21, §5.2, §5.5, §5.6.

**Files**
- Create `OrderHub.Application/Features/Trips/StartTrip/StartTripCommand.cs` + handler
- Create `OrderHub.Application/Features/Trips/CompleteTrip/CompleteTripCommand.cs` + handler
- Create `OrderHub.Application/Features/Trips/FailTrip/FailTripCommand.cs` + handler
- Create `OrderHub.Application/Features/Trips/CancelTrip/CancelTripCommand.cs` + handler
- Create `OrderHub.Application/Features/Trips/RetryTrip/RetryTripCommand.cs` + handler
- Create `OrderHub.Tests/TripLifecycleHandlerTests.cs`

**Steps**
1. Common shape for all five:
   - `(int Id, int ExpectedVersion, ...)`; `[RequirePermission(Trips, Write)]`;
   - build `TripOperation`; load `GetForUpdateAsync` (attempts + history) → `NotFound`;
   - compare version → `Conflict` with the Arabic reload message;
   - call the domain method; catch `InvalidOperationException` and return `InvalidOperation` with the Arabic message;
   - `SaveChangesAsync`; map `DbUpdateConcurrencyException`/`PersistenceConflictException` → `Conflict`;
   - return `Result<int>` with the new version.
2. `StartTripCommand(Id, ExpectedVersion)`.
3. `CompleteTripCommand(Id, ExpectedVersion, string? Note)` — the optional note is accepted by the command but the current schema has no place for it; do not invent a column. Either omit the parameter entirely or keep it out of the domain call and document that it is not persisted. **Preferred:** omit `Note` in phase 1 so the API does not promise data it discards.
4. `FailTripCommand(Id, ExpectedVersion, string? Reason)` — trim and require 1–200 before the domain call.
5. `CancelTripCommand(Id, ExpectedVersion, string? Reason)` — trim and require 1–200.
6. `RetryTripCommand(Id, ExpectedVersion)`.
7. Tests:
   - start on a new trip creates attempt 1 started and writes `AttemptStarted`;
   - start on a retry-pending trip starts the existing open attempt; a second start is rejected (S-02 variant);
   - complete closes the attempt and the header;
   - fail requires a reason, closes the attempt and sets the header;
   - complete/fail on an unstarted retry attempt → `InvalidOperation` (S-06);
   - cancel from new, in-progress and failed: closes the open attempt only when one exists; a failed attempt is not rewritten (S-01, S-05, S-08);
   - retry creates an unstarted attempt 2 and returns the header to Pending (S-03);
   - completed/cancelled reject all five (S-07);
   - stale version returns `Conflict` and writes nothing;
   - unauthenticated → `Unauthorized`; no Write → denied;
   - each successful transition increments the version exactly once and writes exactly one history row;
   - no sequence number is consumed by any lifecycle command.

**Acceptance criteria**
- The five commands reproduce §5.2 exactly and never bypass the version check.
- Reasons are validated in the handler and returned as `InvalidOperation`, not as a domain exception in the UI.
- A rejected transition writes nothing: no attempt, no history, no version change.
- No delete or archive command exists.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripLifecycleHandlerTests"
```

**Traps**
- `Cancel` from `Failed` must not create an attempt.
- `Retry` must not start the new attempt.
- `Complete` and `Fail` require an open **started** attempt, not merely a `Pending` header.
- Do not reset `StartedAtUtc`.
- Do not return a stale version; the UI uses the returned value for the next action.

**Proposed commit:** `feat(trips): operate standalone trip lifecycle`

---

### [ ] T09 — Trips list read model: search, filters, derived state and paging

**Goal:** Provide the server-side paged list used by `/trips`, with provider-compatible case-insensitive search, guaranteed-safe numeric handling, exact status predicates and clamped paging.

**Depends on:** T05. **Delivers independently:** the list query is testable on SQLite; no UI.

**Locked decisions:** D23, D27, D31, §7.2, §9.4, §16.8 items 2, 6, 7, 15.

**Files**
- Create `OrderHub.Application/Features/Trips/Contracts/ITripReader.cs` (list method first; extended in T10/T11)
- Create `OrderHub.Application/Features/Trips/Contracts/TripListItem.cs`
- Create `OrderHub.Application/Features/Trips/Contracts/TripSearchCriteria.cs`
- Create `OrderHub.Application/Features/Trips/Contracts/TripDateRange.cs`
- Create `OrderHub.Application/Features/Trips/GetTripsPage/GetTripsPageQuery.cs` + handler
- Create `OrderHub.Infrastructure/Features/Trips/TripReader.cs`
- Modify `OrderHub.Infrastructure/Features/Trips/TripsServiceCollectionExtensions.cs` (register `ITripReader`)
- Create `OrderHub.Tests/TripListQueryTests.cs`

**Steps**
1. `TripSearchCriteria`:
   - `string? Search`
   - `IReadOnlyCollection<TripDisplayState> DisplayStates`
   - `int? DeliverymanId`
   - `TripPartyType? PartyType` (matches either end)
   - `DateOnly? ScheduledFrom`, `DateOnly? ScheduledTo`
   - `DateOnly? CreatedFrom`, `DateOnly? CreatedTo`
2. `GetTripsPageQuery(TripSearchCriteria Criteria, int Page, int PageSize)` with `[RequirePermission(Trips, Read)]` → `PagedResult<TripListItem>`.
3. Page normalisation in the handler/reader (RF-06):
   - `page = Math.Max(1, page)`;
   - `pageSize` → 25 when not one of 10/25/50;
   - if `TotalCount > 0` and the requested page exceeds `TotalPages`, clamp to `TotalPages`;
   - never construct `PagedResult` with `PageSize <= 0`.
4. Search predicate (provider-compatible, no `ILike`):
   - `term = criteria.Search?.Trim().ToLowerInvariant()`; empty → ignore.
   - textual branch: `t.Title.ToLower().Contains(term)`, deliveryman name lower, `From.Name`/`To.Name` lower, null-safe `From.Phone`/`To.Phone` `Contains`.
   - numeric branch: only when `long.TryParse(term, NumberStyles.None, CultureInfo.InvariantCulture, out var number)` succeeds; OR `t.TripNumber == number`.
   - digit-only phone search: compare `FromPhone`/`ToPhone` with separators removed through chained `Replace(" ", "")`, `Replace("-", "")`, `Replace("+", "")` and `Contains(term)`.
   - Combine textual and numeric branches with `OR`. A term that overflows `long` simply skips the numeric branch.
5. Filters:
   - New: `Status == Pending && StartedAtUtc == null`.
   - InProgress: `Status == Pending && StartedAtUtc != null` (this includes retry-pending, per §9.4).
   - Failed: `t.Attempts.Any(a => a.Status == Failed)`.
   - Completed: `Status == Completed`.
   - Cancelled: `Status == Cancelled`.
   - Selected states OR together; empty selection means all.
   - Party type matches `(From.PartyType == type || To.PartyType == type)`.
   - Date ranges: `TripDateRange.ToUtc(DateOnly from, DateOnly to)` converts Riyadh day boundaries to a half-open UTC range (`>= start`, `< endExclusive`). Apply to `ScheduledAtUtc` (set range → exclude null schedules) and `CreatedAt`.
6. Projection (`TripListItem`): id, trip number/year/month, title, deliveryman id/name, from/to type and name, scheduled, status, `DisplayState`, `CurrentAttemptNumber`, `AttemptCount`, `HasFailedAttempt`, created, version, `IsEditable`.
   - Compute `DisplayState` in the reader from projected attempt facts (open attempt started flag, has attempts), never in the UI.
   - Use projection subqueries or a second query for the page's attempts; no per-row query.
7. Sort: `CreatedAt desc, Id desc`.
8. `TripDateRange`:
   - `TimeZoneInfo.FindSystemTimeZoneById("Asia/Riyadh")` (same id as `NumberSequenceService`);
   - `from.ToDateTime(TimeOnly.MinValue)` and `to.AddDays(1).ToDateTime(TimeOnly.MinValue)` converted with `TimeZoneInfo.ConvertTimeToUtc`;
   - reject `from > to` by returning an empty range (no exception).
9. Tests:
   - search by title, courier name, party name, phone with and without separators;
   - Arabic and English case-insensitivity on SQLite (test 64);
   - a digit-only term matches the exact trip number and a phone fragment, does not throw on a 30-digit term, and does not match an unrelated number (S-53);
   - each status predicate, multi-select OR, and the failed-filter behaviour after retry/cancel/complete (tests 37–38, S-04);
   - date boundary: 23:30 Riyadh on the last selected day included, 00:30 the next day excluded (test 76);
   - paging: page 0, negative, beyond-last, `pageSize` 0 and 999; total count intact (test 75);
   - default sort and `IsEditable` for new vs retried;
   - no `Trips.Read` → denied.

**Acceptance criteria**
- The search predicate runs identically on SQLite and PostgreSQL; `ILike` appears nowhere in the feature.
- A digit-only term can never throw or match an unrelated trip number alone.
- Display state is produced by the reader and matches §5.1.
- Page and page-size inputs are clamped before `PagedResult`/`Skip`.
- Read-only users can execute the query; no write data is exposed.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripListQueryTests"
```

**Traps**
- `Contains` on a lowered column is the proven pattern; `EF.Functions.Like` with a user-built `%term%` needs escaping and is not required.
- Do not use `int.TryParse`; `TripNumber` is `bigint`.
- Do not treat an overflow or an out-of-range term as an error.
- Do not compare a UTC column with a local date; use the half-open UTC range.
- Do not compute the display state in Razor or from the header alone.
- Do not pass 20 as a page size; `TableFooter` does not offer it.

**Proposed commit:** `feat(trips): add trips list query with search and filters`

---

### [ ] T10 — Details, editor and lookup read models

**Goal:** Provide the details page data (with paged history), the editor data, and capped searchable lookups for deliverymen, clients, suppliers and carriers.

**Depends on:** T09 (shares `ITripReader` and `TripReader`). **Delivers independently:** every read model is queryable and testable; no UI.

**Locked decisions:** D17, D27, §4.3, §7.1, §7.2, §9.5, §9.6, §16.8 items 3 and 9.

**Files**
- Create `OrderHub.Application/Features/Trips/Contracts/TripDetails.cs`, `TripPartyData.cs`, `TripAttemptData.cs`, `TripHistoryEntryData.cs`
- Create `OrderHub.Application/Features/Trips/Contracts/TripEditorData.cs`
- Create `OrderHub.Application/Features/Trips/Contracts/TripLookupKind.cs`, `TripLookupItem.cs`
- Create `OrderHub.Application/Features/Trips/GetTripDetails/GetTripDetailsQuery.cs` + handler
- Create `OrderHub.Application/Features/Trips/GetTripEditor/GetTripEditorQuery.cs` + handler
- Create `OrderHub.Application/Features/Trips/GetTripLookups/GetTripLookupsQuery.cs` + handler
- Modify `OrderHub.Application/Features/Trips/Contracts/ITripReader.cs`
- Modify `OrderHub.Infrastructure/Features/Trips/TripReader.cs`
- Modify `OrderHub.Application/Features/Setup/Suppliers/Contracts/ISupplierReader.cs`
- Modify `OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierReader.cs`
- Create `OrderHub.Tests/TripReadQueryTests.cs`

**Steps**
1. `GetTripDetailsQuery(int Id, int HistoryPage = 1, int HistoryPageSize = 10)` with `[RequirePermission(Trips, Read)]` → `TripDetails?` or a `Result`-consistent failure for a missing id. Follow the existing read-handler convention used by `GetOrderDetails`.
   - `TripDetails`: id, number/year/month, title, description, deliveryman id/name, route parties, scheduled/created/status-changed timestamps, status, `DisplayState`, `CurrentAttemptNumber`, `AttemptCount`, `HasFailedAttempt`, `Version`, `IsEditable`, attempts, paged history.
   - History paging: normalise `HistoryPage >= 1`, `HistoryPageSize` to 10/25/50 (default 10), clamp beyond the last page, return `PagedResult<TripHistoryEntryData>` (RF §1 refinement 2).
2. `GetTripEditorQuery(int Id)` with `[RequirePermission(Trips, Write)]` → `TripEditorData`: header fields, both party snapshots, `Version`, `IsEditable`, deliveryman id. No history page.
3. `GetTripLookupsQuery(TripLookupKind Kind, string? Search, int Take = 20)` with `[RequirePermission(Trips, Write)]` → `IReadOnlyList<TripLookupItem>`:
   - `TripLookupKind`: `Deliveryman`, `Client`, `Supplier`, `ShippingCarrier`.
   - Clamp `Take` to 1–20.
   - Deliveryman → `IDeliverymanReader.GetDeliverymenByNameAsync`.
   - Client → `IClientReader.GetClientsByNameAsync`.
   - Supplier → the new `ISupplierReader.GetSuppliersByNameAsync`.
   - Carrier → `IShippingCarrierReader.GetShippingCarriersByNameAsync`.
   - `TripLookupItem(Id, Name, Detail)`; `Detail` may contain city/phone for display. Never return entities, usernames or unrelated sensitive data.
4. `ISupplierReader.GetSuppliersByNameAsync(string searchTerm, int take, CancellationToken)`:
   - mirror `ClientReader.GetClientsByNameAsync` exactly: projection includes `City.Name + " - " + Street`, `Phone.Number.FullNumber`, ordered by name, capped by `take`;
   - same case behaviour as the client reader (plain `.Contains`); do not silently make one picker case-insensitive and another not (RF-07/§7.2);
   - do not change any existing supplier method.
5. Extend `TripReader` with `GetTripDetailsAsync` and `GetTripEditorAsync`; project directly to contracts. Attempts ordered by number ascending; history ordered by id ascending (timeline order) inside the paged window.
6. Tests:
   - details for each known party type includes the city exactly once; supplier/carrier null phone/address surfaces as `null`, not `""`; a 22-character phone is intact (tests 49, 65, 77);
   - details displays derived state matching the header + attempts;
   - history pages: default 10, page 2, page beyond last, invalid size normalised, total count stable;
   - editor `IsEditable` true only for a new trip; false after start and after retry;
   - editor returns both snapshots and the version;
   - lookups: empty search returns up to 20; a search filters; `take` clamped; each kind maps to the right reader; supplier lookup returns the same city/phone projection as clients;
   - no `Trips.Read` on details, no `Trips.Write` on editor/lookups.

**Acceptance criteria**
- No contract exposes an EF entity or a navigation.
- History is paged server-side and never loaded in full.
- Party snapshots display exactly what was stored, with the city included and no empty strings.
- Lookups are capped and use existing readers; only the supplier method is added.
- Missing/deleted master records do not break details; the snapshot is the source of display.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripReadQueryTests"
```

**Traps**
- Do not write a new client/carrier reader; reuse the existing ones.
- Do not change shared reader case behaviour; that alters existing screens (§7.2, no-harm rule).
- Do not load all suppliers into memory for the picker.
- Do not concatenate the city again over `Address.FullAddress`.
- Do not return the full history "because the page is small"; §9.5 requires paging.
- Do not include order links in `TripDetails`; standalone trips have no order.

**Proposed commit:** `feat(trips): add trip details, editor and lookup queries`

---

### [ ] T11 — Deliveryman trip queries: summary, standalone and order-bound

**Goal:** Provide the three independent read models the deliveryman details page needs, with attempt-based counters and order links resolved from the numbering party.

**Depends on:** T05. **Delivers independently:** every courier section is queryable and testable; no UI.

**Locked decisions:** D14, D29, D31, §7.2, §7.5, §9.7.

**Files**
- Create `OrderHub.Application/Features/Trips/GetDeliverymanTripsSummary/GetDeliverymanTripsSummaryQuery.cs` + handler + `DeliverymanTripsSummary.cs`
- Create `OrderHub.Application/Features/Trips/GetDeliverymanStandaloneTrips/GetDeliverymanStandaloneTripsQuery.cs` + handler + `DeliverymanTripListItem.cs`
- Create `OrderHub.Application/Features/Trips/GetDeliverymanOrderBoundTrips/GetDeliverymanOrderBoundTripsQuery.cs` + handler + `OrderBoundTripListItem.cs`
- Modify `OrderHub.Application/Features/Trips/Contracts/ITripReader.cs`
- Modify `OrderHub.Infrastructure/Features/Trips/TripReader.cs`
- Create `OrderHub.Tests/DeliverymanTripsQueryTests.cs`

**Steps**
1. Summary `(int DeliverymanId)` gated `Trips.Read` → `DeliverymanTripsSummary`:
   - `OpenCount`: header `Pending` (new or in progress).
   - `FailedCount`: trips with at least one failed attempt (attempt-based, independent of the header).
   - `CompletedCount`: header `Completed`.
   - `CancelledCount`: header `Cancelled`.
   - `TotalCount`: all standalone trips for the courier.
   - Counters are not mutually exclusive (D31); use one query with conditional aggregates/EXISTS, not five round trips.
2. Standalone page `(int DeliverymanId, int Page, int PageSize)` gated `Trips.Read` → `PagedResult<DeliverymanTripListItem>`:
   - filter by courier, newest first, default 10, normalise to 10/25/50;
   - fields: id, number/year/month, title, route summary (from name → to name), status, display state, current attempt/total, scheduled, version, `IsEditable`.
3. Order-bound page `(int DeliverymanId, int Page, int PageSize)` gated `Trips.Read` → `PagedResult<OrderBoundTripListItem>`:
   - repository query filtered by `NumberingPartyId`;
   - resolve `OrderId = trip.OrderId ?? trip.DeliveryStep.OrderId`;
   - fields: trip id, trip number/year/month, status, resolved order id, order number, order status, created date;
   - rows with no resolvable order are excluded from the query (the domain guarantees one exists);
   - newest first, default 10, normalised.
4. Tests (tests 39–41, 67):
   - counters: open includes new and in-progress but not retry-pending twice; failed counts a trip with a failed attempt even when the header is Pending/Completed/Cancelled; completed/cancelled by header; total matches;
   - standalone paging counts only that courier;
   - order-bound paging returns direct and step-bound trips, stable totals;
   - a trip whose order-step deliveryman changed but whose `NumberingPartyId` stayed with the original courier appears only under the original courier (test 67);
   - rows expose the resolved order id for both direct and step-bound trips;
   - no `Trips.Read` → denied.

**Acceptance criteria**
- The three queries are independent; one empty section cannot blank another.
- Counters are attempt-based where required and use the documented labels in the UI.
- Order-bound filtering uses `NumberingPartyId` only.
- Both order-bound shapes (direct and step-bound) resolve a usable order id.
- Paging inputs are clamped.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~DeliverymanTripsQueryTests"
```

**Traps**
- Do not filter by `Order.DeliverymanId` or `DeliveryStep.DeliverymanId`.
- Do not use the header status for the failed counter.
- Do not fabricate a route for order-bound trips; `DeliveryTrip` has no route fields (RF-20).
- Do not merge the three queries into one page load; the page must be able to skip all of them without `Trips.Read`.
- Do not count order-bound trips in the standalone counters.

**Proposed commit:** `feat(trips): add deliveryman trip queries`

---

### [ ] T12 — Trip usage reader, DI registration and master-data delete guards

**Goal:** Block deleting a deliveryman, client, supplier or shipping carrier that a standalone trip references, including a courier referenced only by reassignment history.

**Depends on:** T05. **Delivers independently:** all four delete flows are guarded and tested; no UI change.

**Locked decisions:** D24, §8.6, §16.8 items 11 and 12.

**Files**
- Create `OrderHub.Application/Interfaces/ITripUsageReader.cs`
- Create `OrderHub.Infrastructure/Features/Trips/TripUsageReader.cs`
- Modify `OrderHub.Infrastructure/Features/Trips/TripsServiceCollectionExtensions.cs`
- Modify `OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommandHandler.cs`
- Modify `OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommandHandler.cs`
- Modify `OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommandHandler.cs`
- Modify `OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommandHandler.cs`
- Create `OrderHub.Tests/TripUsageReaderTests.cs`
- Create `OrderHub.Tests/TripDeleteGuardTests.cs`
- Create `OrderHub.Tests/TripServiceRegistrationTests.cs`

**Steps**
1. `ITripUsageReader` in `OrderHub.Application/Interfaces` (not under `Features/Trips`, so Setup handlers do not depend on trip feature types):
   - `Task<bool> HasDeliverymanTripsAsync(int deliverymanId, CancellationToken)`
   - `Task<bool> HasPartyTripsAsync(TripPartyType partyType, int partyId, CancellationToken)`
2. `TripUsageReader`:
   - deliveryman: `StandaloneTrips.AnyAsync(t => t.DeliverymanId == id)` **or** `StandaloneTripHistories.AnyAsync(h => h.PreviousDeliverymanId == id || h.NewDeliverymanId == id)`;
   - party: one `AnyAsync` over the trip table matching either `From` or `To` `(PartyType, PartyId)`.
   - Both are `EXISTS` checks; no list materialisation, no tracking.
3. Register `ITripUsageReader` in `AddTripsFeature()`.
4. Delete handlers: add `ITripUsageReader` to the primary constructor and insert the check **after** every existing guard and **before** `Remove`:
   - deliveryman → `"لا يمكن حذف المندوب لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة."`
   - client → `"لا يمكن حذف العميل لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة."`
   - supplier → `"لا يمكن حذف المورد لارتباطه بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة."`
   - carrier → `"لا يمكن حذف شركة الشحن لارتباطها بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة."`
   Keep every existing message, order and `try/catch` unchanged.
5. Tests:
   - each guard blocks when a trip references the party as From and as To;
   - the deliveryman guard blocks a courier referenced only by `previous_deliveryman_id`/`new_deliveryman_id` after reassignment (test 73);
   - the guard allows deletion when no trip references the party (existing behaviour unchanged);
   - an order-referenced deliveryman still returns the existing order message first (existing guard precedence);
   - `TripServiceRegistrationTests`: `AddInfrastructure` registers `ITripUsageReader` (descriptor assertion) and the four delete handlers resolve from the container with test stubs where a Web-only dependency is required (test 68);
   - the reader works on SQLite (owned-type translation).

**Acceptance criteria**
- Deleting a referenced party is blocked with the Arabic message from §8.6.
- A historical courier reference blocks deletion even after reassignment.
- Existing delete behaviour for unreferenced records is unchanged.
- A missing DI registration cannot reach production; it is caught by a resolving test.
- No setup handler references a `Features/Trips` type.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripUsageReaderTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripDeleteGuardTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~TripServiceRegistrationTests"
```

**Traps**
- Do not check only `StandaloneTrip.DeliverymanId`; the history reference is required (test 73).
- Do not reorder existing guards; the trips check goes after them.
- Do not move `ITripUsageReader` under `Features/Trips`.
- Do not use a polymorphic FK; none is possible.
- Do not block deletion of a party with no trips.

**Proposed commit:** `feat(trips): guard master-data deletion with trip usage`

---

### [ ] T13 — Trips list and details screens (read + status actions)

**Goal:** Ship the first usable trips screens: a paged, filterable list and a details page with attempts, paged history and the status actions that already have commands.

**Depends on:** T01, T08, T09, T10. **Delivers independently:** `/trips` and `/trips/{id}` work with real data and every rendered action is wired. No create/edit affordance yet.

**Locked decisions:** D27, D31, §9.1, §9.4, §9.5, §9.8, §16.8 items 2 and 1 (nav item only).

**Files**
- Create `OrderHub.Web/Components/Pages/Features/Trips/_Imports.razor`
- Create `OrderHub.Web/Components/Pages/Features/Trips/TripPresentation.cs` (status/attempt chips and the action matrix)
- Create `OrderHub.Web/Components/Pages/Features/Trips/Index.razor` (+ `Index.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Trips/Details.razor` (+ `Details.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Trips/TripActionDialog.razor` (+ code-behind)
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor` (group with the item only)
- Modify `OrderHub.Web/wwwroot/css/mud-blazor-overrides.css` only if an existing Mud widget needs the design-system correction

**Steps**
1. `TripPresentation`:
   - display-state chip map per §9.8 (slate/brand/rose/emerald/slate, Arabic labels);
   - attempt chip map;
   - action matrix returning the allowed actions for `(DisplayState, hasWritePermission)`;
   - no hard-coded colours outside the semantic five hues.
2. `/trips` with `[PermissionAuthorize(PermissionFeature.Trips, PermissionLevel.Read)]`:
   - `PageScaffold` title «المشاوير المستقلة», description «إدارة مشاوير المناديب المستقلة»;
   - toolbar: search, status multi-select, deliveryman picker, party type select, scheduled range, created range, «مسح الفلاتر», «تحديث»;
   - any filter change resets the page to 1 (S-48);
   - `MudTable`/`MudDataGrid` with `ServerData`, columns per §9.4; numbers/dates `dir="ltr"` + invariant;
   - status chip from `DisplayState`; attempts column `current / total` or `0`;
   - row click opens `/trips/{id}`; action buttons stop propagation;
   - action column only when `Trips.Write`; actions: Start/Complete/Fail/Cancel/Retry per state via `TripActionDialog`; **Edit is not rendered in this task** because `/trips/{id}/edit` does not exist yet;
   - `TableFooter` 1-based, page size 10/25/50, default 25;
   - loading, empty and error states; a conflict from an action shows the Arabic reload message and refreshes the row;
   - **no create CTA in this task**; T14 adds it with the editor route.
3. `/trips/{Id:int}` with `[PermissionAuthorize(Trips, Read)]`:
   - header card: number, status chip, title, courier, scheduled/created/status-changed (RTL labels, LTR values);
   - route cards From/To: type chip, name, phone, address, location link only when present, external badge;
   - actions per state (Start/Complete/Fail/Cancel/Retry) via the same dialog; **no Edit action yet**; no delete;
   - attempts table: number, status chip, started, ended, reason;
   - history timeline with actor, timestamp, attempt number, reason, old/new courier names and old/new number for reassignment; `TableFooter` 10/25/50 default 10, server-side via `GetTripDetailsQuery(Id, HistoryPage, HistoryPageSize)`.
4. `TripActionDialog`:
   - one component with an action parameter (`Start`, `Complete`, `Fail`, `Cancel`, `Retry`);
   - confirmation text per action; reason field only for Fail/Cancel with 1–200 counter;
   - calls the matching command with the page's current `ExpectedVersion`; busy flag disables all buttons; success snackbar and reload; failure banner with the handler message;
   - a version conflict offers «إعادة تحميل».
5. Navigation: add the dedicated group per §9.3 with `Items` containing «كل المشاوير» only:
   - title «المشاوير», subtitle «مشاوير مستقلة», icon `Icons.Material.Outlined.Route`, `ViewAllHref="/trips"`;
   - item: «كل المشاوير», `/trips`, tolerance `Trips.Read`, tone `"amber"`, description «إدارة مشاوير المناديب المستقلة»;
   - leave `Create = null`; T14 adds it. Do not place any trip item in «جهات التعامل».
6. Manual acceptance:
   - admin and a `Trips.Read`-only custom role;
   - a user without `Trips` sees no group and is redirected from `/trips`;
   - a read-only user sees no action column and no status actions on details;
   - every displayed action executes once, shows a busy state and updates the page;
   - RTL/mobile and 12px floor.

**Acceptance criteria**
- Both pages render real data and every visible action is wired; there is no create/edit control yet.
- Read-only users never see write actions; direct command calls are still rejected by T08.
- Filters and paging are server-side and reset correctly.
- Status chips come from `DisplayState`; tones use only the semantic five hues.
- Exactly one primary CTA per page: the details page has no primary CTA in this task; the list has none until T14.
- No JavaScript, no dead buttons, no fabricated counts.

**Verify**
```powershell
dotnet build OrderHub.sln --configuration Release
dotnet run --project OrderHub.Web
```

Manual: create trips through the API/tests or a seeded dev copy; open `/trips`; filter/search; open a new trip, start it, complete/fail/cancel/retry another; verify the failed filter after retry/cancel; open `/deliverymen` and confirm the existing list is unchanged.

**Traps**
- Do not render Edit or the create CTA before their routes exist.
- Do not use `MudTable` row click as the only navigation; the row itself must be clickable and action buttons must not bubble.
- Do not recompute the display state in Razor.
- Do not pass a page size outside 10/25/50.
- Do not add a delete control.
- Do not add a Trips item to «جهات التعامل»; its view-all points to `/clients`.

**Proposed commit:** `feat(trips): add trips list and details screens`

---

### [ ] T14 — Create/Edit page, reassignment UI and the remaining entry points

**Goal:** Add `/trips/create` and `/trips/{id}/edit` with party pickers and external fields, wire reassignment with its reason and audit hint, then expose create/edit wherever the spec places them.

**Depends on:** T06, T07, T10, T13. **Delivers independently:** a write user can create, edit and reassign a new trip entirely in the browser; the list and details entry points become complete.

**Locked decisions:** D15, D16, D22, D25, D26, §5.4 rule 9, §9.2, §9.3, §9.6, §16.8 item 9.

**Files**
- Create `OrderHub.Web/Components/Pages/Features/Trips/Editor/TripEditor.razor` (+ `.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Trips/Editor/TripPartyEditor.razor`
- Create `OrderHub.Web/Components/Pages/Features/Trips/Editor/TripEditorState.cs`
- Create `OrderHub.Web/Components/Pages/Features/Trips/Editor/TripReassignDialog.razor`
- Modify `OrderHub.Web/Components/Pages/Features/Trips/Index.razor` (create CTA + Edit action)
- Modify `OrderHub.Web/Components/Pages/Features/Trips/Details.razor` (Edit action)
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor` (add `Create`)
- Modify `OrderHub.Web/Components/Pages/Features/Trips/_Imports.razor`

**Steps**
1. `TripEditor.razor` with two routes:
   - `@page "/trips/create"` and `@page "/trips/{TripId:int}/edit"`, both `[PermissionAuthorize(Trips, Write)]`;
   - `?deliverymanId=` preselects the courier on create;
   - load editor data only for edit; for create, load lookups on demand.
2. State and validation:
   - deliveryman required;
   - title required 1–150; description ≤1000;
   - From/To required; type selector (`Client`, `Supplier`, `ShippingCarrier`, `External`);
   - known types: searchable picker via `GetTripLookupsQuery` (20 max); show the selected name and city/phone detail;
   - external: name, phone (6–22, ≥6 digits, optional `+`), address required; location optional;
   - scheduled date/time entered in Riyadh and converted to UTC;
   - edit note ≤200 (existing trips only);
   - inline Arabic errors; save disabled while busy; no local save if `IsEditable == false`.
3. On save:
   - create → `CreateTripCommand`; navigate to `/trips/{receipt.Id}`; snackbar;
   - edit → `UpdateTripCommand` with the loaded `Version`; snackbar; reload or navigate to details;
   - map `Conflict` to the Arabic reload message; `InvalidOperation` messages next to the form or in a banner; `Persistence` in a banner.
4. Reassignment:
   - when the selected courier differs, show the reason field (required 1–200) and the hint «سيُسجَّل تغيير المندوب كعملية إعادة إسناد، ولن تُسجَّل التغييرات الأخرى حقلاً بحقل.» (§16.8 item 9);
   - opening `TripReassignDialog` confirms the old/new courier and the new number rule before submit;
   - reassign and other edits are one save and one history row.
5. Entry points, added only now that the routes exist:
   - list: primary CTA «إنشاء مشوار» (Write only) and Edit action for new trips;
   - details: Edit action when `IsEditable` and Write;
   - nav: add the group `Create` («إنشاء مشوار», `/trips/create`, `Trips.Write`, icon `Icons.Material.Filled.Add`). The T01 cache fix makes it render (manual check).
6. Guard for a retried trip: details/list must not offer Edit when `IsEditable` is false, even though the header is `Pending` (S-10A).
7. Manual acceptance:
   - create with a client and an external party; verify the details snapshot and number;
   - edit the title only while new; change From to another client; verify only From changed;
   - reassign to another courier with a reason; verify the new number, the old/new names in history and the single `Reassigned` row;
   - attempt to edit after start (no action) and via a direct URL (server rejection + Arabic notice);
   - write user sees the nav create CTA; read-only user does not; no duplicate create item appears in the panel.

**Acceptance criteria**
- Create and edit pages work for every party type, including external validation.
- A rejected save shows Arabic feedback and consumes no number.
- Reassignment is part of the edit screen, requires a reason and records one `Reassigned` row.
- The audit hint is visible whenever the courier changes.
- The list/nav/details entry points exist only after the routes exist; read-only users see none.
- One primary CTA per page («حفظ المشوار» on the editor).

**Verify**
```powershell
dotnet build OrderHub.sln --configuration Release
dotnet run --project OrderHub.Web
```

Manual: as a write user, create → details → edit → reassign → start; as a read-only user, confirm no CTA/actions; as a user without `Trips`, confirm `/trips/create` redirects to `/access-denied`.

**Traps**
- Do not call the editor query for a user without Write; the page gate already blocks it.
- Do not send `ReassignReason` when the courier did not change; the handler rejects it.
- Do not clear the fields of an untouched party client-side; the server keeps its snapshot.
- Do not show the create CTA in the nav from the `Items` list as a duplicate; the cache fix covers `group.Create`.
- Do not use `MudTextField` for the reason without a 1–200 counter; enforce the server rule in the UI too.

**Proposed commit:** `feat(trips): add standalone trip editor`

---

### [ ] T15 — Deliveryman details page with local trips gating

**Goal:** Add `/deliverymen/{id}` with the courier header, counters, standalone trips and read-only order-bound trips, without changing the existing list, dialog, delete or paging.

**Depends on:** T01, T11, T14. **Delivers independently:** every courier section works and the `/deliverymen` list gains only a name link.

**Locked decisions:** D14, D29, D34, §9.2, §9.7, §16.7.

**Files**
- Create `OrderHub.Web/Components/Pages/Features/Deliverymen/Details.razor` (+ `Details.razor.cs`)
- Modify `OrderHub.Web/Components/Pages/Features/Deliverymen/Index.razor` (name becomes a link)
- Modify `OrderHub.Web/Components/Pages/Features/Deliverymen/_Imports.razor` if trip namespaces are needed
- Reuse `OrderHub.Web/Components/Pages/Features/Deliverymen/Editor/DeliverymanEditorDialog.razor`

**Steps**
1. Route `/deliverymen/{DeliverymanId:int}` with `[PermissionAuthorize(PermissionFeature.Deliverymen, PermissionLevel.Read)]`.
2. Load the deliveryman basics using the existing read query. Header: name, city, phone, edit action when `Deliverymen.Write`; edit reuses `DeliverymanEditorDialog` with `DialogParameters`; on `Ok(true)`, reload the header. Do not build a second form.
3. Local trips gate:
   - after the page permission passes, call `PermissionAccess.IsAllowedAsync(PermissionFeature.Trips, PermissionLevel.Read)`;
   - only when true dispatch the summary and both paged queries;
   - when false, render the courier data only; no trips section, no trip request, no `/access-denied` redirect (D34, test 57).
4. Counters (standalone only, not mutually exclusive): «مفتوحة» (`Pending`), «فاشلة» (any failed attempt), «مكتملة» (`Completed`), «ملغاة» (`Cancelled`), «الإجمالي». Use the summary contract; do not compute in the page.
5. Standalone section:
   - `TableFooter` default 10, server-side paging;
   - columns: number, title, route summary, status chip, attempts, scheduled;
   - actions follow the state matrix when `Trips.Write`; reuse `TripActionDialog` from T13;
   - create button prefilled with `deliverymanId` only when `Trips.Write`; the route exists from T14.
6. Order-bound section:
   - `TableFooter` default 10, server-side paging;
   - columns: trip number, resolved order number, order status, trip status, created;
   - read-only: no start/complete/fail/retry/cancel actions (S-69);
   - order link gated once per page load by `PermissionAccess.IsAllowedAsync(Orders, Read)` (S-67A);
   - when the order id is null (should not happen), render «—» with no link.
7. List change: make the deliveryman name a link to the details page. Do not change row behaviour, edit/delete actions, paging or permissions. A user without `Deliverymen.Read` gets the existing access-denied behaviour for the new route.
8. Manual acceptance:
   - a user with `Deliverymen.Read` but no `Trips.Read` sees the page normally and no trips section (network: no trip request);
   - a `Trips.Read` user without `Orders.Read` sees order-bound rows without links;
   - a write user starts/cancels a trip from the section and the counters update;
   - the create button prefills the courier;
   - `/deliverymen` behaves exactly as before apart from the name link (S-54A, test 70).

**Acceptance criteria**
- The trips section is absent, not disabled, when `Trips.Read` is missing; no trip request is dispatched.
- Counters match T11 exactly and are labelled «مفتوحة» rather than «جارية».
- Order-bound rows are read-only and link only when the user can open the order.
- The existing `/deliverymen` list, edit dialog, delete action and paging are unchanged.
- Both sections have independent loading/empty/error states.

**Verify**
```powershell
dotnet build OrderHub.sln --configuration Release
dotnet run --project OrderHub.Web
```

Manual: seed a courier with new/in-progress/failed/completed/cancelled trips plus order-bound direct and step-bound trips; verify counters, both tables, links, and the no-`Trips`/no-`Orders` variants.

**Traps**
- Do not rely on the `[RequirePermission]` on trip queries as the section gate; it redirects the whole page.
- Do not evaluate `Orders.Read` per row; evaluate once per load.
- Do not count order-bound trips in the counters.
- Do not change the existing list's paging, row actions or delete behaviour.
- Do not add a second courier edit form.
- Do not show a link for an unresolved order.

**Proposed commit:** `feat(trips): add deliveryman details page`

---

### [ ] T16 — Full regression and no-harm verification

**Goal:** Prove the feature is additive and does not change any existing workflow, then update the spec and this registry to reflect reality.

**Depends on:** T01–T15. **Delivers independently:** a green, documented baseline; no new feature work.

**Files**
- Modify `plans/TRIPS-MANAGEMENT-SPEC.md` (status line only; keep locked decisions intact)
- Modify this file (checkboxes, deviations, manual results)
- Add tests only where a gap is found; no production behaviour changes

**Steps**
1. Run the full verification chain:
   ```powershell
   dotnet restore OrderHub.sln
   dotnet build OrderHub.sln --configuration Release
   dotnet test OrderHub.sln --configuration Release --no-build
   node OrderHub.Tests/Web/order-local-time.test.mjs
   ```
2. Manual regression matrix (record results under T16):
   - **No `Trips`:** no nav group, `/trips*` redirects to `/access-denied`, `/deliverymen` unchanged, delete flows unchanged.
   - **`Trips.Read` only:** list/details/courier sections visible; no create/edit/status actions; no nav create CTA.
   - **`Trips.Write`:** create/edit/reassign/status actions; nav create CTA visible once (no duplicate item).
   - **`Deliverymen.Read` without `Trips.Read`:** courier page loads, no trips section, no trip request.
   - **`Trips.Read` without `Orders.Read`:** courier page order-bound rows without links.
   - **Admin:** sees everything without a grant; the role editor can grant `Trips`.
3. Existing flows to exercise in the browser:
   - `/orders` list/details/workflow, `/orders/create` supplier-only save;
   - `/deliverymen` list search, edit dialog, delete guard, paging;
   - `/clients`, `/suppliers`, `/shipping-carriers` delete flows (with and without trips);
   - `/admin/roles` (15 cards, existing grants unchanged);
   - any page using `TableFooter` (inventory pages if present, orders, products) still renders its page-size selector.
4. Search verification: the SQLite tests prove the predicate; run the same searches manually on the development PostgreSQL database (read-only) to confirm Arabic/English case behaviour and phone fragments.
5. Data-safety checks:
   - the migration added three tables only (inspect the generated `Up`/`Down`);
   - no existing table, column, index or row was changed;
   - `number_sequences` was not reset; gaps are expected and documented;
   - no `Trips` grant exists in any seeded role.
6. Update the spec status line and this registry. Fix any failure in the owning task, not here.

**Acceptance criteria**
- Full build/test/Node chain is green.
- Every manual matrix row behaves as specified.
- The existing suite passes unchanged (test 72).
- `/deliverymen` list/dialog/delete/paging are unchanged apart from the name link (test 70).
- The spec and this file are accurate; no unchecked task remains in T01–T15.

**Verify:** the commands and the completed manual matrix above.

**Proposed commit:** `test(trips): complete regression and no-harm verification`

---

### [ ] T17 — Deployment rehearsal, rollback, release note and sign-off

**Goal:** Prove the migration and rollback on a populated copy, document the operational steps, and close the feature.

**Depends on:** T16. **Delivers independently:** a deployment package and a signed-off registry.

**Files**
- Modify `plans/TRIPS-MANAGEMENT-SPEC.md` (deployment/rollback notes if they changed)
- Modify this file (checkboxes, sign-off, deviations)
- No production code changes unless a rehearsal exposes a defect (fix it in the owning task)

**Steps**
1. Migration rehearsal on a **populated copy** (owner-run or owner-approved):
   - back up the copy;
   - apply the new migration after all existing migrations;
   - confirm the three tables, indexes and constraints exist;
   - confirm no existing table/row changed and no sequence row was reset;
   - confirm the app starts and existing pages work with the new tables empty.
2. Rollback rehearsal on the same copy:
   - export trip data if any exists;
   - drop `standalone_trip_histories`, `standalone_trip_attempts`, `standalone_trips` in that order;
   - delete `role_permissions` rows with `feature = 'Trips'`;
   - do **not** decrement `number_sequences`; gaps stay and are harmless;
   - confirm existing pages still work.
3. Release note (Arabic) covering:
   - the migration must be applied **before** the new build;
   - `Trips` is ungranted by default; admins see it immediately; other users need a grant through the role editor;
   - deleting a referenced courier/client/supplier/carrier is now blocked with an Arabic message;
   - trip numbering shares the courier monthly sequence, so gaps are expected;
   - rollback instructions and the data-export caveat.
4. Sign-off:
   - every task T01–T17 checked;
   - full suite + Node test green;
   - manual matrix recorded;
   - proposed commit messages listed below.
5. **Do not commit, tag or push.** Present the final commit proposal to the owner.

**Acceptance criteria**
- The migration applies to a populated copy and changes only the three new tables.
- The rollback rehearsal restores the previous state without touching existing data or sequences.
- The release note is accurate and includes the grant and deployment-order warnings.
- All registry tasks are checked and the spec status matches reality.

**Verify:** the rehearsal log and the full verification chain from T16.

**Proposed commit:** `docs(trips): record deployment and rollback for standalone trips`

---

### [x] T18 — Unified trips list (standalone + order-bound) — owner follow-up 2026-11-09

**Goal:** After first run, the owner required `/trips` to be a real table that lists every trip in the program regardless of source, with order-bound rows read-only and routed to their order workflow page.

**What changed**
- `TripSource` enum (`Standalone`, `OrderBound`); `TripListItem` gains `Source`, `OrderId`, `OrderNumber`, `OrderStatus`; party/title/schedule/attempt fields are nullable for order-bound rows.
- `TripReader.GetPageAsync` unions standalone rows with `DeliveryTrips` linked via `OrderId` or `DeliveryStepId`, projecting both sides to the same anonymous shape before `Concat` (EF Core cannot translate a set operation after a client projection). Order-bound rows carry the deliveryman from `NumberingPartyId`, resolve the order number/status through either the direct order or the delivery step's order, and are ordered/paged together with standalone rows.
- Filters: party-type and scheduled-date filters exclude order-bound rows (they have no route or schedule); display-state filter maps order-bound `Pending` to In Progress; search covers order number and order-bound trip number; deliveryman and created-date filters apply to both sources.
- UI: table `Breakpoint` changed from `Lg` to `Sm` (the `Lg` breakpoint collapsed the table into stacked cards below 1280px, which the owner saw); the title cell shows a «طلب» badge plus the order number for order-bound rows; actions menu offers «فتح الطلب» instead of edit/lifecycle; row click opens `/orders/{orderId}` only with `Orders.Read`; standalone rows keep `/trips/{id}`.
- Copy: page description, search placeholder, nav item, role catalog and editor labels now say the list includes order-bound trips («كل مشاوير المناديب، المستقلة والمرتبطة بالطلبات»).

**Impact**
- Read-only behavior change confined to `/trips`; no schema, data, migration, numbering or lifecycle change.
- Order-bound row ids are `DeliveryTrip` ids and can collide numerically with standalone ids — routing always uses `Source`/`OrderId`, never `/trips/{id}` for order-bound rows.
- Deep paging stays server-side: ordering and `Skip/Take` are applied to the union in SQL, not in memory (verified in tests).

**Rollback**
- Code-only: revert `TripReader`, `TripListItem`, `TripSource` and the Trips page changes; the list returns to standalone-only with no data loss.

**Tests**
- 6 new `TripListQueryTests`: order-bound metadata, delivery-step resolution, interleaved ordering across sources with stable paging, order-number search, party/schedule exclusion, display-state mapping.

**Proposed commit:** `feat(trips): show all trips in a unified list`

---

## 4. Traceability matrices

### 4.1 §16.8 pre-implementation fixes → tasks

| §16.8 item | Task(s) | Proof |
|---|---|---|
| 1. Navigation permission cache | T01 (fix), T13 (group), T14 (create CTA) | Manual: write user sees the create footer; read-only does not. |
| 2. Page sizes 10/25/50 | T09 (25), T10 (10 history), T11 (10), T13, T15 | Queries normalise; `TableFooter` renders an offered value. |
| 3. City + nullable contacts | T06, T07, T10 | `GetForUpdateAsync`; `""` → null; tests 65/77. |
| 4. UTC conversion and creation clock | T06, T07 | `SpecifyKind` + `UpdateCreationDate`; test 66. |
| 5. Order-bound filter | T05, T11 | `NumberingPartyId`; test 67. |
| 6. Provider-compatible search | T09 | `ToLower().Contains`; no `ILike`; test 64. |
| 7. Numeric search parsing | T09 | `long.TryParse` guard; S-53. |
| 8. Role catalog tone/entries | T01 | Four switch cases; manual card check; test 61 scenario. |
| 9. Audit limitation | T03, T07, T14 | One `Reassigned` row; editor hint; §5.4 rule 9. |
| 10. Snapshot refresh policy | T07 | Diff by `(PartyType, PartyId)`; S-29/S-30. |
| 11. Historical courier references | T02/T03 (names), T12 (guard) | Tests 73/74. |
| 12. DI registration | T05, T12 | `AddTripsFeature`; resolving test 68. |
| 13. History protection | T04 | `ProtectHistory` addition; test 48. |
| 14. Numbering preconditions | T06, T07 | Deliveryman kind allowed; UTC range; save in transaction. |
| 15. Paging inputs | T09, T10, T11 | Clamp before `PagedResult`; test 75. |

### 4.2 Spec sections → tasks

| Spec | Task(s) |
|---|---|
| §3 decisions D1–D36 | Enforced across T01–T17; D33 noted in T07 code/commit. |
| §4.1–§4.6 domain | T02, T03 |
| §5.1–§5.6 lifecycle/numbering/concurrency | T03, T06, T07, T08 |
| §6.1–§6.2 validation | T02, T03, T06, T07 |
| §7.1–§7.5 application/repository | T05–T12 |
| §8.1–§8.6 infrastructure | T04, T05, T12 |
| §9.1–§9.9 web UI | T01, T13, T14, T15 |
| §10.1 domain tests | T02, T03 |
| §10.2 numbering tests | T06, T07 |
| §10.3 application tests | T06–T12 |
| §10.4 infrastructure tests | T04 |
| §10.5 manual UI checks | T13, T14, T15, T16 |
| §10.6 regression tests | T01, T09, T11, T12, T16 |
| §12 phases | This registry's phases |
| §13 assumptions | T03 (lengths), T06 (numbering), T09 (paging), T13–T15 (UI) |
| §14 acceptance | T16, T17 |
| §15 compatibility/rollback | T16, T17 |
| §16 scenarios | T03 (16.1), T04 (16.2 partial), T06/T07 (16.3), T06/T07 (16.4), T01/T13–T15 (16.5), T17 (16.6), T05/T11/T15 (16.7), all (16.8) |

### 4.3 Test numbers → tasks

| Test # | Task |
|---|---|
| 1–20 | T02, T03 |
| 21–27 | T06, T07 |
| 28–31 | T06–T08 |
| 32–36 | T06, T07, T12 |
| 37–41 | T09, T11 |
| 42–50 | T04, T10 |
| 51–61 | T13, T14, T15 (manual) |
| 62–63 | T01, T13 (manual) |
| 64 | T09 |
| 65–66 | T06 |
| 67 | T11 |
| 68 | T12 |
| 69 | T01 (manual) |
| 70 | T16 |
| 71 | T17 |
| 72 | T16 |
| 73–74 | T12, T03 |
| 75 | T09, T11 |
| 76 | T09 |
| 77 | T06, T10 |

---

## 5. Task status board

| Task | Phase | Status | Blocked by |
|---|---|---|---|
| T01 Permissions + catalog + nav cache | Foundation | [x] | — |
| T02 Domain primitives | Foundation | [x] | — |
| T03 Trip aggregate + lifecycle | Foundation | [x] | T02 |
| T04 EF + history protection + migration | Foundation | [x] | T03 |
| T05 Repository + composition | Application | [x] | T04 |
| T06 CreateTrip | Application | [x] | T05 |
| T07 UpdateTrip + reassignment | Application | [x] | T06 |
| T08 Lifecycle commands | Application | [x] | T07 |
| T09 Trips list read model | Application | [x] | T05 |
| T10 Details/editor/lookups | Application | [x] | T09 |
| T11 Deliveryman trip queries | Application | [x] | T05 |
| T12 Usage reader + delete guards | Application | [x] | T05 |
| T13 Trips list + details screens | Web | [x] | T01, T08, T09, T10 |
| T14 Create/Edit + entry points | Web | [x] | T06, T07, T10, T13 |
| T15 Deliveryman details page | Web | [x] | T01, T11, T14 |
| T16 Regression + no-harm | Close | [x] | T01–T15 |
| T17 Deployment + rollback + sign-off | Close | [~] | T16 |

---

## 6. Acceptance and sign-off

- [x] T01–T15 checked; full suite + Node test green. Manual UI matrix pending owner run (no app instance/login available during implementation).
- [x] T16 regression green (271 tests, Release). No existing order, delivery-step, setup-delete, navigation, roles-editor or paging behaviour was changed; the only shared touch point is the `nav group.Create` permission-cache fix and the four delete guards.
- [~] T17 rollback and deployment order documented in `plans/TRIPS-MANAGEMENT-RELEASE.md`; applying the migration to a populated copy and rehearsing rollback remain owner-run.
- [x] Spec status updated; no locked decision changed without an owner-approved amendment.
- [ ] `Trips` is ungranted by default; the owner decides which roles receive it after deployment.
- [ ] No commit, tag or push was performed by an agent.

**Proposed commit sequence (owner runs them):**

1. `feat(trips): add trips permission and nav create permission cache fix`
2. `feat(trips): add standalone trip domain primitives`
3. `feat(trips): add standalone trip aggregate and lifecycle`
4. `feat(trips): persist standalone trips with history protection`
5. `feat(trips): add standalone trip repository`
6. `feat(trips): create standalone trips with monthly numbering`
7. `feat(trips): edit and reassign standalone trips`
8. `feat(trips): operate standalone trip lifecycle`
9. `feat(trips): add trips list query with search and filters`
10. `feat(trips): add trip details, editor and lookup queries`
11. `feat(trips): add deliveryman trip queries`
12. `feat(trips): guard master-data deletion with trip usage`
13. `feat(trips): add trips list and details screens`
14. `feat(trips): add standalone trip editor`
15. `feat(trips): add deliveryman details page`
16. `test(trips): complete regression and no-harm verification`
17. `docs(trips): record deployment and rollback for standalone trips`

**Final proposal after the owner's review:** `feat(trips): add standalone trips management`


---

## 7. Implementation record (2026-11-09)

### Test suites added

| Suite | Covers |
|---|---|
| `TripDomainPrimitivesTests` | Party normalization, snapshot nulls, attempt transitions, history factories, operation/validation rules, display/event enums |
| `StandaloneTripDomainTests` | Creation, numbering, start/complete/fail/retry/cancel, terminal states, edit and reassignment, version increments |
| `StandaloneTripPersistenceTests` | Graph round-trip, unique trip number, partial unique open-attempt index, history immutability, version concurrency token, 22-char phone, check constraints |
| `StandaloneTripRepositoryTests` | Aggregate loading, order-bound direct/step resolution, paging clamp |
| `CreateTripHandlerTests` | Snapshots, receipt, Riyadh period input, unauthenticated/validation/missing-entity paths, rollback, UTC schedule |
| `UpdateTripHandlerTests` | Pure edit, reassignment from the creation month, stale version, post-start rejection, snapshot preservation, failed renumber save |
| `TripLifecycleHandlerTests` | All five transitions, reason validation, terminal states, no sequence consumption |
| `TripListQueryTests` | Projection, newest-first, case-insensitive/numeric/phone search, display-state filters, Riyadh ranges, paging clamp, permissions |
| `TripReadQueryTests` | Details/history paging, snapshot fidelity, editor flags, capped lookups, permission levels |
| `DeliverymanTripsQueryTests` | Summary counters (attempt-based failure), independent sections, `NumberingPartyId` filtering |
| `TripUsageReaderTests` | Current and historical deliveryman references, both party ends |
| `TripDeleteGuardTests` | Four delete guards, existing-guard precedence, unreferenced deletion, Setup isolation |
| `TripServiceRegistrationTests` | DI registration and scoped lifetimes |

Shared helpers: `TripTestDb` (seeded SQLite in-memory database and order-bound fixtures) and `TripHandlerTestContext` (real repositories/unit of work, fake numbering that mirrors the Riyadh period calculation, fixed clock/user).

### Deviations

- **Order-bound query translation fix.** `StandaloneTripRepository.GetOrderBoundTripsPageAsync` originally applied `Where`/`OrderBy` to the projected `OrderBoundTripListItem`, which EF cannot translate (runtime `InvalidOperationException`, not caught by compilation). The filter and ordering now run before the projection; result shape and semantics are unchanged. Found by the new repository/query tests.
- **No SQLite shortcut for numbering or delete guards.** Handler tests use the real repositories and `UnitOfWork`; only `INumberSequenceService`, `ICurrentUser` and `TimeProvider` are faked, because the production sequence service runs PostgreSQL-only SQL.
