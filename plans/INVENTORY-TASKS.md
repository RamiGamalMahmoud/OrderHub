# Inventory (Multi-Warehouse Stock) — Executable Task Registry

- **Version:** 2.1 · 2026-11-12
- **Sources of truth:** `plans/INVENTORY-MANAGEMENT-SPEC.md` v2.1 · `design_draft.md` (§ قرارات المالك المحسومة + § قرارات الجولة الثانية, D1–D21) · `concepts_brief.md` · `AGENTS.md`
- **Status:** planning complete. **No inventory production code exists** (verified by repo-wide search). Every task below is unchecked. This registry fully replaces v1.0 — wherever v1.0 conflicted with the newer documents, v2.1 encodes the newer decision.
- **Execution mode:** strictly sequential tasks T01 → T08, one migration each (plus the standalone seed migration in T01). A task is done only when its own acceptance criteria and the global definition of done pass.

---

## 0. How to execute a task

1. Read the task fully, plus the spec sections it cites (`SPEC §n`, `D#` = decisions in `design_draft.md`). Do not rely on memory of earlier tasks.
2. Implement Domain → Application → Infrastructure → Web in that order.
3. Run `dotnet restore`, then `dotnet build OrderHub.sln --configuration Release`, then the task's targeted tests, then the full suite.
4. Exercise the task's UI acceptance manually in the browser when the task adds or changes UI.
5. Mark the checkbox `[x]` and append a one-line deviation note only when the implementation differs from the task.
6. **Do not commit.** At the end, propose the task's Conventional Commit message (listed per task). The owner runs every commit.
7. Never edit a migration that already ran, never edit `SealedRoleSeedSql.V1`, never add JavaScript, never fetch fonts at runtime, never change `Supply`, pricing, documents, or existing numbering behavior.

### Global definition of done (applies to every task)

- `dotnet build OrderHub.sln --configuration Release` is green with zero new warnings introduced by the task.
- All tests pass, including the pre-existing suite.
- Migrations are additive-only: new tables, nullable columns, defaults, indexes. Existing columns, rows, constraints and enum encodings are untouched.
- Expected failures return `Result`/`Result<T>` with `"Unauthorized"`, `"Forbidden"`, `"NotFound"`, `"Conflict"`, `"InvalidOperation"`, `"Persistence"`. Never throw for expected failures.
- Permission gates exist in **both** layers: page attribute for navigation/UX, handler-level check for direct invocation; keeper-binding (SPEC §7.3) inside every mutating inventory handler.
- Arabic, RTL UI; numbers via `CultureInfo.InvariantCulture`; no `MarkupString` for previews; `PageScaffold` + `TableFooter` (exceptions per AGENTS.md); `.oh-btn`; exactly one primary CTA per page; busy flags on all async buttons.
- No new JavaScript files. No runtime font fetching.
- Migration created via the design-time factory (`DesignTimeAppDbContextFactrory.cs`), timestamped **after `20261109124656_AddStandaloneTrips`**, named exactly as its task states.
- Seed/permission test counts are updated in the same task that changes the matrix; drift is never left for later.
- Legacy data safety: legacy orders (no allocations) keep today's behavior exactly; no retroactive movements, reservations or approvals.

### Build / test commands (from repo root)

```powershell
dotnet restore OrderHub.sln
dotnet build OrderHub.sln --configuration Release
dotnet test OrderHub.sln --configuration Release --no-build
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~<TestClass>"
```

Migration creation:

```powershell
dotnet ef migrations add <Name> --project OrderHub.Infrastructure --startup-project OrderHub.Web --output-dir Migrations
```

---

## 1. Verified facts that constrain the tasks

Re-checked against the current code on 2026-11-12:

| # | Fact | Evidence | Consequence |
|---|---|---|---|
| FR-01 | `PaymentMethods` is the last member before the `// Administration` block in `PermissionFeature`; 14 members total; stored as text | `PermissionFeature.cs` | T01 inserts `Warehouses`/`Inventory` between them; no data migration needed |
| FR-02 | `NumberSequenceKind` has 4 members (Order, Supplier, Deliveryman, ShippingCarrier); `NumberSequenceService` rejects unknown kinds in the monthly path | `NumberSequenceKind.cs`, `NumberSequenceService.cs` | T05 adds `Warehouse = 5` to the monthly allow-list |
| FR-03 | `WorkRecordType` has 3 members (SupplierSupply, Shipment, DeliveryTrip); `IWorkRecord` is internal, auto-numbered by `UnitOfWork` | `WorkRecordType.cs`, `AppDbContext.cs`, `UnitOfWork.cs` | T05 adds `StockIssueOrder = 4`; implement `IWorkRecord` explicitly like `DeliveryTrip`; never assign numbers manually |
| FR-04 | `SealedRoleSeedSql.V1` is frozen; current seed = **4 roles / 31 cells** (sales 9, purchasing 6, manager 11, data-entry 5); tests assert these counts | `SealedRoleSeedSql.cs`, `SealedRoleSeedTests.cs` | T01 adds `V2` in its own migration; totals become **5 roles / 38 cells** (SPEC §7.1) |
| FR-05 | `UpdateOrderHandler` rebuilds a replacement `Order` and `ReplaceOrderItems` deletes + re-inserts all items; item ids unstable | `UpdateOrderHandler.cs`, `Order.cs` | T03 adds explicit client-sent item ids, validated same-order (pattern: `Order.SynchronizeDeliverySteps`, line 209) |
| FR-06 | `Order.SynchronizeDeliverySteps` already proves the explicit-id sync pattern | `Order.cs:209` | T03 mirrors it for items (`SynchronizeOrderItems`) |
| FR-07 | Create/update order handlers hard-require a supplier for every item | `CreateOrderCommandHandler`, `UpdateOrderHandler` | T03 changes the rule: supplier required only for items with a supplier-sourced remainder; stock-fully-sourced items save without one |
| FR-08 | `Order.Lifecycle` exposes `DeliveryReadinessError()` / `CanArchive`; readiness knows nothing about stock | `Order.Lifecycle.cs` | T05 keeps readiness unchanged (no order-level stock gate, D19) and adds reservation release on close; the delivery cap lands in T08 |
| FR-09 | `AppDbContext.ProtectHistory` rejects Modified/Deleted on history entities | `AppDbContext.cs` | T02 adds `InventoryMovement` to it |
| FR-10 | Repositories live in the misspelled `OrderHub.Infrastructure/Reopsitories/` (some namespaces `...Repositories`) | `AGENTS.md` | Match the folder/namespace of each file touched; never "fix" the typo |
| FR-11 | Tests run SQLite in-memory (`EnsureCreated`, snake_case, `RbacTestDb` runs seed SQL explicitly) — **no PostgreSQL test harness exists and CI disables PG deliberately** | `AGENTS.md`, `RbacTestDb.cs` | T02/T03 prove CAS logic on SQLite; the real lock proof is the documented manual PG script (T08/D13) |
| FR-12 | `AttachmentsController` + `LocalFileAccessService` have no ownership/size/type checks and store by raw uploaded name | `OrderHub.Web/Controllers/Files/AttachmentsController.cs` | T04 leaves them untouched; inventory attachments get a separate endpoint/folder/metadata entity (D14) |
| FR-13 | Latest migration is `20261109124656_AddStandaloneTrips` | `Migrations/` | All inventory migrations timestamp after it |
| FR-14 | `RoleFeatureCatalog` uses a hard-coded switch for name/description/icon/tone | `RoleFeatureCatalog.cs` | T01 adds two cases to all four methods or the roles editor renders English generic cards |
| FR-15 | `NavMenu.razor` is the single nav source, permission-filtered groups | `NavMenu.razor` | T01 adds the «المخزون» group + first item; later tasks append items, never dead links |
| FR-16 | Sealed-role guards (rename/delete/permission-edit rejected) already exist | `SealedRolePresets`, roles editor | T01 storekeeper preset inherits them automatically through `All` |

---

## 2. Dependency graph

```
T01 Permissions + V2 seed + candidates + catalog + nav group
 └► T02 Ledger + StockBalance + CAS operations
      ├► T03 Warehouses (entity → handlers → screens)   [independent of T02 except guards]
      │      └► T04 Stock-in + attachments
      │           └► T05 Order identity + allocations + atomic reservation + source rule
      │                └► T06 Issue orders + batches + numbering
      │                     └► T07 Approvals (edit payload / cancel / reversal)
      │                          └► T08 Delivery records + returns/damaged/counts + rebuild + acceptance
```

Rules: strictly sequential; T03 may start only after T01 but its deactivation guard completes in T04; no task may create a migration other than its own.

---

## 3. Tasks

### [x] T01 — Permission surface, sealed presets V2, keeper candidates, catalog, nav group

**Goal:** Make `Warehouses` and `Inventory` first-class permission features, seed the locked 5-role / 38-cell matrix in its own migration, and expose the warehouse-manager-safe candidate query.

**Depends on:** nothing. **Delivers independently:** roles editor shows 16 Arabic cards; seeded roles match D1/D3; Manager can list keeper candidates.

**Locked decisions:** D1, D2, D3, SPEC §7.1.

**Files**
- Modify `OrderHub.Domain/Enums/PermissionFeature.cs`
- Modify `OrderHub.Domain/Authorization/SealedRolePresets.cs`
- Modify `OrderHub.Infrastructure/Migrations/SealedRoleSeedSql.cs` (add `V2`; **never touch `V1`**)
- Create migration `..._SeedInventoryPermissions` (single `migrationBuilder.Sql(SealedRoleSeedSql.V2)` in `Up`; non-destructive `Down`)
- Modify `OrderHub.Web/Components/Pages/Features/Administration/Roles/Editor/RoleFeatureCatalog.cs`
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor`
- Create `OrderHub.Application/Features/Warehouses/GetKeeperCandidates/GetWarehouseKeeperCandidatesQuery.cs` (+ handler) and `WarehouseKeeperCandidate.cs`
- Modify `OrderHub.Application/Interfaces/Repositories/IUserRepository.cs` + `OrderHub.Infrastructure/Reopsitories/UserRepository.cs`
- Modify `OrderHub.Tests/RbacTestDb.cs` (run V1 then V2) and `OrderHub.Tests/SealedRoleSeedTests.cs`
- Create `OrderHub.Tests/WarehouseKeeperCandidateTests.cs`

**Steps**
1. Enum members after `PaymentMethods`, before `// Administration`: `[Description("المخازن")] Warehouses` then `[Description("المخزون")] Inventory`.
2. `SealedRolePresets`: sales +`Inventory=Read` (10 cells) · purchasing +`Inventory=Read` (7 cells) · manager +`Warehouses=Write`, `Inventory=Approve` (13 cells) · new `StorekeeperKey = "storekeeper"` «أمين مخزن» with exactly `Warehouses=Read`, `Products=Read`, `Inventory=Write` (3 cells) · data-entry unchanged (5). Total **38**.
3. `SealedRoleSeedSql.V2`: insert the `storekeeper` role (guarded by `role_key` OR same display name) + the 7 new cells, each `WHERE NOT EXISTS (role_id + feature)`; `CURRENT_TIMESTAMP`/`TRUE`/text names exactly like V1; provider-neutral (PostgreSQL + SQLite).
4. Catalog entries (both features, all four switch methods): `Warehouses` «المخازن» — icon `Icons.Material.Filled.Warehouse`, tone `bg-amber-50 text-amber-600`, description «إدارة المخازن وأمناء المخزون وحالة التشغيل»; `Inventory` «المخزون» — icon `Icons.Material.Filled.Inventory`, tone `bg-teal-50 text-teal-600`, description «أرصدة المنتجات وحركات المخزون وأوامر الصرف».
5. NavMenu: add group «المخزون» (icon Warehouse, subtitle «المخازن والأرصدة وأوامر الصرف») with no items yet — or add it in T02/T03 alongside its first item; **never render a dead link**. If the group renders zero visible items it must be hidden (existing `GroupVisible` logic).
6. Candidate query: `[RequirePermission(PermissionFeature.Warehouses, PermissionLevel.Write)]`; returns `Id`, `DisplayName`, `IsActive`, `HasInventoryWrite` (true for `IsAdmin` or role level for `Inventory` ∈ {Write, Approve}); ordered by `DisplayName`; never exposes username/password hash/role name/phone.
7. Tests: seed totals 5 / 38 (manager 13, sales 10, purchasing 7, storekeeper 3, data-entry 5); idempotency after a second run; storekeeper name reserved + sealed; name-collision case re-derived: with a pre-existing custom «مبيعات» role (1 cell) the sales preset is skipped → expected **5 roles / 29 cells**; candidate handler minimal fields + gating; admin/storekeeper `HasInventoryWrite=true`, sales user `false`.
8. Update any other test asserting 31 cells or 4 roles in the same task.

**Acceptance**
- 16 feature cards, Arabic, four segments, no matrix-component code change.
- V2 idempotent; V1 byte-for-byte unchanged; totals exactly 5 / 38.
- Manager gets candidates; a sales user is denied at the pipeline; payload has no sensitive fields.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~SealedRoleSeedTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WarehouseKeeperCandidateTests"
```
Manual: `/admin/roles` as Higher Manager — 16 cards, «أمين مخزن» read-only «دور جاهز».

**Traps**
- Do not insert the enum members inside the Administration block.
- Do not give the candidate query `Users.Read`; do not reuse `GetUsersQuery`.
- Counts: 38 everywhere; the collision case is 29 (re-derive, don't copy v1.0's numbers).

**Proposed commit:** `feat(inventory): add warehouse and inventory permissions with sealed storekeeper preset`

**Deviations:** migration created with `--startup-project OrderHub.Infrastructure` because the Web host lacks `Microsoft.EntityFrameworkCore.Design`; the nav group is added empty (hidden by `GroupVisible`) until T02/T03 add its first item; `IUserRepository` gained `GetAllWithRolePermissionsAsync` (stubbed in `UserRolePreservationTests`'s fake) and `RoleCommandGuardTests`'s sales cell count moved 9 → 10.

---

### [x] T02 — Ledger, StockBalance, CAS operations

**Goal:** The immutable movement ledger and the per-(warehouse, product) cached balance with `DamagedQty`, plus the atomic repository operations every later task consumes.

**Depends on:** T01 (feature surface). **Delivers independently:** ledger + balances testable end to end; no UI.

**Locked decisions:** D5, D6, D7, D20, D13, SPEC §2 (Q1–Q12), §5.2, §5.3.

**Files**
- Create `OrderHub.Domain/Enums/InventoryDirection.cs` (`In=1, Out=2`), `InventoryMovementType.cs` (`OpeningStock=1, StockIn=2, IssueOut=3, ReturnIn=4, ReturnDamagedIn=5, DamagedDisposal=6, Adjustment=7, Reversal=8, DamagedAdjustment=9`), `InventoryMovementStatus.cs` (`Active=1, Reversed=2`)
- Create `OrderHub.Domain/Models/InventoryMovement.cs`, `StockBalance.cs`
- Create `OrderHub.Infrastructure/Configurations/InventoryMovementConfiguration.cs`, `StockBalanceConfiguration.cs`
- Modify `OrderHub.Infrastructure/AppDbContext.cs` (DbSets + `ProtectHistory` for `InventoryMovement` — FR-09)
- Create `OrderHub.Application/Interfaces/Repositories/IInventoryRepository.cs` + `OrderHub.Infrastructure/Reopsitories/InventoryRepository.cs` (match FR-10 namespace style)
- Modify `OrderHub.Infrastructure/Reopsitories/ServiceCollectionExtensions.cs`
- Create migration `..._AddInventoryLedger`
- Create `OrderHub.Tests/InventoryLedgerTests.cs`

**Steps**
1. `StockBalance`: `WarehouseId`, `ProductId`, `StockOnHand`, `ReservedOnHand`, `DamagedQty` (all `>= 0`), `Version`. Behaviors: `ApplyIn`, `ApplyOut`, `DamagedIn`, `DamagedDisposal`, `Reserve`, `Release`, `Consume` (stock + reserved decrease together, never negative); each asserts invariants Q1/Q3/Q8/Q11 and bumps `Version`.
2. `InventoryMovement`: fields per SPEC §5.3 — direction, type, quantity `> 0`, the five nullable source refs (plain nullable ints in this migration; real FKs added by later tasks when the target table lands), `ReversalOfMovementId` (required iff type = Reversal), `Status`, `EffectiveAtUtc` (rejects future beyond a small clock margin), `CreatedAtUtc`, `CreatedByUserId`, `Note?` (mandatory on Reversal/Adjustment). Factory validation; internal-only `MarkReversed()`; no public setters; no delete.
3. EF: enums as strings; checks `quantity > 0`, `stock_on_hand >= 0`, `reserved_on_hand >= 0`, `damaged_qty >= 0`; unique `(warehouse_id, product_id)`; unique filtered index on `reversal_of_movement_id WHERE NOT NULL`; indexes `(warehouse_id, product_id, effective_at_utc)` and `(product_id, effective_at_utc)`.
4. Repository: `GetStockItemAsync` (tracked), `GetStockAsync`, `Add(InventoryMovement)`, `GetOrCreateStockItemAsync`, `TryReserveAsync(warehouseId, productId, qty)` (atomic guarded update `stock_on_hand - reserved_on_hand >= qty`, bumps Version, returns false on insufficient), `ReleaseAsync`, `ConsumeAsync` (atomic, never negative), `GetLedgerAsync`/`CountLedgerAsync` ordered by `EffectiveAtUtc` desc. Provider-neutral (no PG-only SQL in this task).
5. Migration: both tables, indexes, checks; nothing on existing tables.
6. Tests: factory validation; `MarkReversed` internal only; reserve/release/consume boundaries + Version increments; second over-reserve returns false (`"Conflict"` path at handler level later); EF round-trip; unique pair; `ProtectHistory` rejects modify/delete; ledger ordering by `EffectiveAtUtc`; damaged in/disposal never touching `StockOnHand` (Q8).

**Acceptance**
- No API can produce a negative column or a zero/negative movement; movements are immutable after insert.
- Reserve is atomic and race-safe at the SQLite level (CAS proven — FR-11); real lock proof is T08's PG script.
- Existing tests remain green.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~InventoryLedgerTests"
```

**Traps**
- Do not add a mutable `Version` to `InventoryMovement` (ledger is insert-only; only `Status` transitions internally).
- Do not build a generic "inventory service" abstraction; the repository is the shared surface.
- Do not write PG-specific `FOR UPDATE` here — the ordered-lock protocol lands with the write-flows that need it (T03+); SQLite must keep passing.

**Proposed commit:** `feat(inventory): add stock ledger and cached balances`

**Deviations:** `warehouse_id` carries no FK in this migration (the `warehouses` table lands in T03), same as the five deferred source refs; `InventoryMovement` is a standalone entity with an explicit `created_at_utc` column (no `ModelBase`/`modified_at`, since the ledger never updates); the mandatory-note rule also covers `DamagedAdjustment` (Q11 sensitive adjustment); migration created with `--startup-project OrderHub.Infrastructure`.

---

### [x] T03 — Warehouses: entity, handlers, screens, nav

**Goal:** Warehouse registry with exactly one keeper, transfer semantics, guards, and the first usable inventory screen.

**Depends on:** T01, T02 (repository surface for guards). **Delivers independently:** warehouses manageable in the browser; keeper binding queryable.

**Locked decisions:** D1, D2, SPEC §5.1, §7.2, §7.3.

**Files**
- Create `OrderHub.Domain/Models/Warehouse.cs`, `OrderHub.Infrastructure/Configurations/WarehouseConfiguration.cs`
- Create `OrderHub.Application/Interfaces/Repositories/IWarehouseRepository.cs` + `OrderHub.Infrastructure/Reopsitories/WarehouseRepository.cs`
- Create migration `..._AddWarehouses`
- Create `OrderHub.Application/Features/Warehouses/`: `GetWarehouses`, `GetWarehouse`, `CreateWarehouse`, `UpdateWarehouse` (commands + handlers + contracts)
- Create `OrderHub.Web/Components/Pages/Features/Warehouses/Index.razor` (+ `.razor.cs`), `Editor/WarehouseEditorDialog.razor`, `Editor/WarehouseEditorState.cs`
- Modify `NavMenu.razor` (add «المخازن» item to the «المخزون» group)
- Create `OrderHub.Tests/WarehouseDomainTests.cs`, `WarehouseCommandHandlerTests.cs`

**Steps**
1. Entity: owned `EntityName` (unique, ≤ 100), `KeeperUserId` required, `IsActive`. Behaviors `Rename`, `AssignKeeper`, `Deactivate`, `Activate`; no delete. EF: FK to users `Restrict`, unique index on `keeper_user_id` (one user keeps at most one warehouse), unique name.
2. Handlers: `GetWarehousesQuery` (`Warehouses.Read`) → id, name, keeper display name, `IsActive`, pending-issue-order count (0 in this task; wired in T06); `GetWarehouseQuery`; `CreateWarehouseCommand`/`UpdateWarehouseCommand` (`Warehouses.Write`) — name uniqueness (case-insensitive), keeper exists + active, deactivation guarded (`InvalidOperation` when positive stock/damaged or pending work exists — guard reads `StockBalance` + (later) issue orders; in this task implement the stock part and leave a documented extension point).
3. Keeper transfer happens in one save: binding removed from the old warehouse context and added to the new; open counts/issue orders follow the current binding (consumers read by warehouse, so transfer needs no row updates — assert this in tests).
4. UI `/warehouses`: `PageScaffold` «المخازن»; MudTable/grid of name/keeper/activity badge (emerald نشط / slate متوقف); primary CTA «إضافة مخزن» (`Warehouses.Write` only); editor dialog per `CityEditorDialog` conventions with candidates from T01's query, amber inline warning when `HasInventoryWrite=false` (saving still allowed), activity toggle on edit; no delete control.
5. Tests: construction/validation; rename; assign/deactivate; unique name + unique keeper; deactivation guard with positive stock; handler authz; transfer keeps pending work readable via the new keeper binding.

**Acceptance**
- Exactly one keeper per warehouse, enforced at DB and handler level.
- Deactivation blocked with positive stock; no delete path anywhere.
- Page usable: create/edit/transfer/deactivate with Arabic inline errors; one primary CTA; RTL correct.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~Warehouse"
dotnet build OrderHub.sln --configuration Release
```
Manual: `/warehouses` as Manager (create → transfer keeper → deactivate) and as Data Entry (nav group hidden).

**Traps**
- `EntityName` is owned — mirror `CityConfiguration`.
- Do not add `Version` to `Warehouse` (registry data, last-write-wins acceptable — same call as v1.0).
- Candidates query requires `Warehouses.Write`; hide the dialog for read-only users.

**Proposed commit:** `feat(inventory): add warehouse registry with keeper assignment`

**Deviations:** the «المخزون» nav group now links «عرض الكل» to `/warehouses` until the `/inventory` page lands in T06 (avoids a dead link); the `AddWarehouses` migration also adds the deferred warehouse FKs on `stock_balances` and `inventory_movements`; keeper transfer means replacing one warehouse's keeper (a user keeps at most one warehouse — DB unique index + handler guard), and pending-work consumers resolve the keeper from the warehouse row so no dependent rows change; `PendingIssueOrderCount` is returned as 0 until T06 wires issue orders; warehouse-name uniqueness is case-insensitive at the handler level over the DB's case-sensitive unique index (provider-neutral, no citext).

### [x] T03a — Warehouse address/phone + eligible keepers (owner follow-up)

**Goal:** Every warehouse has a required address and an optional phone; only users who can write/approve inventory may keep a warehouse.

**Decisions:** address reuses `Address` + `cities` (street ≤ 150) and is required in the create/update handlers and editor but **nullable in the database** so T03 rows stay valid; phone reuses `Phone` (country code + national number) and is optional, blank number = no phone, clearing deletes the warehouse-owned `phones` row; keeper eligibility = active user AND (`IsAdmin` OR role has `Inventory.Write`/`Inventory.Approve`) — same predicate as T01's `HasInventoryWrite`, enforced in the candidates query and both handlers.

**Files:** `Warehouse` entity + `WarehouseConfiguration`; migration `..._AddWarehouseContact` (nullable `address_id`/`phone_id`, FKs `Restrict`, indexes); `IWarehouseRepository` + `WarehouseRepository` (contact projections, `RemovePhone`, `GetByIdAsync` includes address/phone); warehouse contracts, `WarehouseInputValidation`, create/update handlers; `IUserRepository.GetWithRolePermissionsAsync` + implementation; new `GetWarehouseEditorCitiesQuery` (`Warehouses.Write`); keeper-candidate query + record; editor dialog/state, list page, tests.

**Deviations:** `WarehouseKeeperCandidate` is now `(Id, DisplayName)` because the query returns only active eligible users (flags became redundant); editing a warehouse whose stored keeper is no longer eligible shows a red block, clears the selection and requires an eligible replacement on save; legacy warehouses without an address stay readable (`CityId = null`, empty street/phone) but must receive an address on their next save; `PendingIssueOrderCount` behaviour and all T03 guards are unchanged.

**Proposed commit:** `feat(inventory): require warehouse address, add optional phone, restrict keeper to eligible users`

---

### [x] T04 — Stock-in / opening balance + attachments

**Goal:** The keeper records stock-in and the one-time opening balance with optional PDF/JPG/PNG attachments, through a separate endpoint/folder/metadata entity that leaves the legacy attachments untouched.

**Depends on:** T03. **Delivers independently:** stock can enter through the UI with documents attached.

**Locked decisions:** D14, SPEC §5.4, §6.1, §7.2.

**Files**
- Create `OrderHub.Domain/Models/StockReceipt.cs`, `StockReceiptLine.cs`, `StockReceiptAttachment.cs` + configurations
- Create migration `..._AddStockReceipts`
- Create `OrderHub.Application/Features/Inventory/RecordStockIn/RecordStockInCommand.cs` (+ handler + `StockInLineInput.cs`, `StockReceiptDetails.cs`)
- Create `OrderHub.Application/Features/Inventory/UploadStockAttachment/` (command + handler) and `DeleteDraftStockAttachment` (or fold into the upload endpoint contract per the implementation's cleanest shape — choose one, document it)
- Create `OrderHub.Web/Controllers/Files/StockAttachmentsController.cs` (separate endpoint)
- Modify `OrderHub.Infrastructure/Services/FilesAccess/LocalFileAccessService.cs` **additively only** (new folder + type/size validation used by the new endpoint; existing behavior untouched)
- Create `OrderHub.Web/Components/Pages/Features/Inventory/StockIn/StockIn.razor` (+ `.razor.cs`, `StockInLineState.cs`, `StockInState.cs`)
- Modify `NavMenu.razor` («تسجيل توريد» CTA in the group where create CTAs live)
- Create `OrderHub.Tests/StockReceiptTests.cs`, `StockInHandlerTests.cs`, `StockAttachmentEndpointTests.cs`

**Steps**
1. Entities: header (`WarehouseId`, `ReceiptType` Opening/StockIn, `SupplierId?` must be product-linked when set, `ReferenceNote?`, actor/timestamps) + lines (`ProductId`, `Quantity > 0`, duplicates aggregate) + attachments (relative path, original name, optional description ≤ 200, size, mime, uploader, timestamp). Immutable after save — mistakes via approved reversal (T07).
2. Attachment rules (D14): PDF/JPG/PNG, ≤ 5 MB, enforced by magic-byte + extension at the new endpoint; files stored under a dedicated folder; draft uploads not referenced by a saved receipt are cleaned (on save failure or orphan sweep in the upload command).
3. Endpoint `StockAttachmentsController`: upload requires `Inventory.Write`; download/delete verify the attachment's owning receipt and the caller's keeper binding (or `Inventory.Approve` for download by Manager); returns via the existing file-stream pattern — **no changes to `AttachmentsController`** (FR-12).
4. Command `RecordStockInCommand` (`Inventory.Write` + `Products.Read` gate + **keeper binding** per SPEC §7.3): validates active warehouse, products exist, supplier link, then one transaction: receipt + per-line movement (`OpeningStock`/`StockIn`, `SourceStockReceiptId`) + `ApplyIn`; rollback → `"Persistence"`.
5. Deactivation guard completion: `UpdateWarehouseCommand` also rejects deactivation while pending issue orders exist (extension point from T03 — issue orders don't exist yet; implement the query defensively).
6. UI `/inventory/stock-in`: keeper-scoped warehouse fixed to his binding (Manager/HM have no stock-in per the locked rules — the page is hidden for them by permission+binding logic, not just attribute); receipt type select («رصيد افتتاحي» allowed only while the warehouse has no movements yet — handler enforces); product autocomplete (`Products.Read`), quantities, supplier select only if the user has `Suppliers.Read`, reference note, attachment upload list with per-file description, removal before save; one primary CTA «حفظ التوريد».
7. Tests: movements + balance in one transaction; rollback leaves nothing; `Products.Read` gate; keeper binding rejects a non-keeper even with `Inventory.Write`; opening type blocked after first movement; attachment size/type/ownership; legacy endpoint untouched (existing attachment tests still green).

**Acceptance**
- Stock enters only through this documented operation; no manual balance editing.
- Attachment rules enforced server-side; the legacy attachments flow behaves exactly as before.
- Opening balance is a distinct type on the same entity and cannot be repeated for a warehouse that already has stock.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockIn"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockAttachment"
```
Manual: record stock-in with 2 lines + 1 PDF as storekeeper; verify `/inventory` balances (T06 page may not exist yet — verify via ledger tests data or the balances query); repeat as sales (denied).

**Traps**
- The receipt id is needed by movements — two saves inside one transaction is acceptable.
- Do not reuse the order-bound `Supply` entity.
- Manager/HM never execute keeper operations (locked rule) — stock-in is keeper-bound; no `IsAdmin` bypass.

**Proposed commit:** `feat(inventory): record stock-in with attachments`

**Decisions / files**
- Domain: `StockReceipt` (Opening/StockIn, optional supplier, note ≤ 300), `StockReceiptLine` (unique per product, duplicates aggregate with checked sum), `StockReceiptAttachment` (draft until `StockReceiptId` is set). `AppDbContext.ProtectHistory` blocks receipt/line edits and any attachment edit/delete once bound, while allowing draft delete and the single draft→receipt binding.
- Migration `20261112212143_AddStockReceipts` (additive): `stock_receipts`, `stock_receipt_lines`, `stock_receipt_attachments`, unique stored file name, unique `(StockReceiptId, ProductId)`, `SourceStockReceiptId` FK (`Restrict`) on `inventory_movements`.
- Application: `RecordStockIn` (keeper binding, `Products.Read` gate, active warehouse, opening-once via `HasMovementsAsync`, supplier must be linked to every stocked product, two saves inside one transaction to obtain the receipt id); `UploadStockAttachment` (magic-byte + extension, ≤ 5 MB, ≤ 200-char description, stored `Guid` name, 24 h stale-draft sweep); `DeleteStockAttachment` (draft + keeper + original uploader); `GetStockAttachmentFile` (keeper or `Inventory.Approve`); `GetStockInContext` (warehouse fixed to the keeper binding, opening flag, product/supplier capability flags).
- Attachments split across two commands (`UploadStockAttachment` / `DeleteStockAttachment`) rather than the optional `DeleteDraftStockAttachment` name — both are exercised through one controller + the Blazor circuit.
- Web: `/inventory/stock-in` keeper-scoped page (warehouse card, receipt type, supplier only with `Suppliers.Read`, product autocomplete, quantities, note, attachment list with view/delete, single primary «حفظ التوريد», saved-receipt summary); `NavMenu` «تسجيل توريد» create CTA behind a `KeeperOnly` flag evaluated once through `GetStockInContextQuery` so Manager/HM never see it; `StockAttachmentsController` at `files/stock-attachments` with dedicated folder `StockAttachments` — `AttachmentsController` and the legacy `Attachments` folder are untouched.
- Tests: `StockReceiptTests` (aggregation/validation/immutability), `StockInHandlerTests` (receipt+ledger+balance, rollback, `Products.Read`, keeper, opening-once, supplier link, draft ownership), `StockAttachmentEndpointTests` (rules/size/magic bytes, upload/delete/download ownership, stale sweep).

**Deviations**
- T04 step 5 (reject deactivation while pending issue orders exist) cannot be implemented yet: there is no issue-order table before T06. The existing positive-stock guard stays and the extension comment remains in `UpdateWarehouseCommandHandler`.
- Orphan cleanup is best-effort: explicit delete, draft delete on user action, and a 24 h sweep in upload. A transient failure after the draft file is written is cleaned by the upload handler; files are never deleted during a save failure (the draft stays reusable).
- The receipt is inserted first inside the transaction to obtain its id for `SourceStockReceiptId` and attachment binding (allowed by the traps).
- `NavMenu` performs one extra `GetStockInContextQuery` for users holding `Inventory.Write`; it is cached with the existing `_permissionsEvaluated` flag.

---

### [x] T05 — Order item identity, allocations, atomic reservation, source rule

**Goal:** Order items allocate across warehouses and/or a linked supplier (D17); final save reserves atomically; item identity is stable; release-on-close lands (no order-level delivery gate, D19).

**Depends on:** T02, T04. **Delivers independently:** an order can be saved with allocations and reservations; editor shows live availability.

**Locked decisions:** D10, D11, D13, D17, D19, SPEC §5.5, §5.11, §6.2, §6.3; owner-approved behavior changes.

**Files**
- Create `OrderHub.Domain/Models/OrderInventoryAllocation.cs` + configuration
- Modify `OrderHub.Domain/Models/Order.cs` (add `SynchronizeOrderItems` mirroring `SynchronizeDeliverySteps`), `Order.Lifecycle.cs` (release hook only)
- Modify `OrderHub.Application/Features/Orders/Contracts/OrderInput.cs` (trailing optional `int? ItemId`, allocation inputs)
- Modify `CreateOrderCommandHandler.cs`, `UpdateOrderHandler.cs` (identity validation, source rule FR-07, atomic reservation)
- Modify `GetForEdit/OrderEditorItem.cs` + its reader; `GetItemEditor/*` availability
- Create `OrderHub.Application/Features/Inventory/GetStockAvailability/GetStockAvailabilityQuery.cs` (+ handler + rows)
- Modify `OrderHub.Web/.../Editor/Products/ProductEditorDrawer.razor`, `ProductsStep.razor`, `State/OrderItemEditorState.cs`, `OrderEditor.razor.cs`
- Create migration `..._AddOrderAllocations`
- Create `OrderHub.Tests/OrderAllocationTests.cs`, `OrderAllocationPersistenceTests.cs`, `StockSourceLifecycleTests.cs`

**Steps**
1. `OrderInventoryAllocation` per SPEC §5.5 (`SourceKind` Warehouse/Supplier, `Qty`, `IssuedQty`, `Status`); FK `Restrict` to `order_items` (D11) and to warehouse/supplier as applicable; unique index `(order_item_id, source_kind, warehouse_id)` to prevent duplicate warehouse rows per item.
2. Item identity: `OrderInput.Item` gains trailing optional `int? ItemId` (never reorder existing positional parameters); `Order.SynchronizeOrderItems` matches by id (same-order validation), keeps matched items, removes missing ones (blocked by FK when allocated — handler translates to a clear Arabic error), adds new; duplicates of the same product remain allowed.
3. Source rule change (FR-07, D17, owner-approved): the editor lets a product join the item list only after a source is chosen — warehouse(s), supplier, or both; supplier is required only when a supplier-sourced remainder exists; a fully stock-sourced item saves without a supplier; a legacy re-save (D10) requires **every** item to gain a source — code-level check with the Arabic message from the spec.
4. Availability query: `Inventory.Read`-gated; per warehouse actual/reserved/available, descending by available; company-wide (D2). In the editor, a user without `Inventory.Read` gets supplier-only sourcing with the Arabic hint (D25-era behavior preserved).
5. Final-save reservation (D13): in the create/update transaction, lock affected balance rows in ascending (product, warehouse) order, validate every allocation against live availability (Version check), then persist order + allocations; conflict → `"Conflict"` with «الكمية لم تعد متاحة في {المخزن}؛ أعد التوزيع.» Drafts allocate nothing; legacy history untouched.
6. Lifecycle: **no order-level stock gate** (D19) — `DeliveryReadinessError` keeps its current non-stock semantics; the stock cap belongs to the delivery record and lands in T08. On `Delivered/Cancelled/Archived` the un-issued remainder releases (system actor + reason history row). Orders without allocations are untouched (tests assert legacy parity).
7. Tests: identity sync (keep/move/add/remove incl. allocated-blocked case); reservation atomicity + race conflict; draft isolation; D10 enforcement; stock-only / supplier-only / mixed sources (D17); readiness unaffected by partial stock; release on the three close paths; allocation FK `Restrict` behavior; availability math + ordering.

**Acceptance**
- Two concurrent savers of the last units: exactly one wins, the other gets `"Conflict"` with no partial save.
- Item ids survive update; allocated items cannot be silently deleted.
- Readiness is never blocked by stock; legacy orders behave identically to today.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~OrderAllocation"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockSourceLifecycle"
```
Manual: editor — allocate an item across 2 warehouses + supplier remainder; save draft (no reservation); final save; reopen and confirm ids kept.

**Traps**
- Trailing optional parameters only — positional call sites in the editor and drafts must not break.
- The reservation transaction is the **only** place that writes `ReservedOnHand` from the order side; issue-order consumption later uses `ConsumeAsync`.
- Do not let the availability query leak entity types to the UI (contracts only).

**Proposed commit:** `feat(orders): add stock allocations with atomic reservation`

**Decisions / files**
- Domain: `OrderInventoryAllocation` (`SourceKind` Warehouse/Supplier, `Qty`, `IssuedQty`, `Status` Active/Released, `Remaining`, `ChangeQuantity`, `Release`); unique `(OrderItemId, SourceKind, WarehouseId)`; FK `Restrict` to order, order item, warehouse and supplier; quantity CHECK. `Order.SynchronizeOrderItems` mirrors `SynchronizeDeliverySteps` (keeps matched instances by id, rejects unknown/duplicate ids, blocks removal while an active allocation exists) and `Order.RemoveAllocations` drops sources from the aggregate. `Order.ReleaseOpenAllocations` sets Active rows to Released and writes one `StockRelease` history row (`PreviousStatus = null`).
- Migration `20261112222657_AddOrderAllocations` (additive; created with `--startup-project OrderHub.Infrastructure`).
- Application: `OrderAllocationRules.Plan` centralises D17 validation (0..n warehouses, ≤ 1 supplier, Σ = quantity, supplier item↔allocation match, `MissingSourceMessage` for D10); `OrderAllocationReservation` loads/locks warehouses and performs CAS reserve/release; `GetStockAvailabilityQuery` returns `(WarehouseId, WarehouseName, StockOnHand, ReservedOnHand, Available)` contracts only, active warehouses, descending by available, `Inventory.Read`-gated. Create and update handlers reserve inside the transaction after locking balances; a CAS miss returns `"Conflict"` with «الكمية لم تعد متاحة في {المخزن}؛ أعد التوزيع.». Create keeps drafts allocation-free (drafts persist no allocation choices).
- Lifecycle: Delivered/Cancelled/Archived release the un-issued remainder before save; legacy orders without allocations take the untouched path. `UpdateOrderHandler` deletes dropped sources (repository `RemoveAllocations` + aggregate removal) so the unique index does not block re-adding the same source later, and blocks removing an item once `IssuedQty > 0`.
- Web: `ProductEditorDrawer` «مصدر الكمية» section (editable warehouse rows with `Max = persisted + available`, derived supplier remainder, `ValidateSources`/`ApplyAllocations`); `ProductsStep` caches availability per product and falls back to supplier-only with an Arabic hint when `Inventory.Read` is denied; `OrderEditor` sends `ItemId` + allocations and maps returned item ids. New state: `OrderItemAllocationEditorState`, `StockAvailabilityEditorState`; `OrderItemEditorState` gained `ItemId`/`Allocations`.
- Tests: `OrderAllocationTests` (plan rules, allocation factory/quantity/release, readiness unaffected by stock, legacy no-op), `OrderAllocationPersistenceTests` (round-trip, FK `Restrict`, unique index, quantity CHECK, availability math/order/exclusions), `StockSourceLifecycleTests` (race conflict without partial save, id preservation + reservation resize, removal releases, issued-quantity block, D10 legacy re-save, supplier-only/mixed sources, release on three close paths, legacy close parity). Shared SQLite harness `OrderAllocationTestDb`.

**Deviations**
- Removing an allocated item is allowed while nothing has been issued: the reservation is released and the allocation rows are deleted inside the same transaction. Only `IssuedQty > 0` blocks removal («لا يمكن حذف بند صُرفت منه كميات. راجع أوامر الصرف أولاً.»), which is stricter and clearer than letting the FK surface a raw failure.
- Dropped sources are physically deleted rather than kept as `Released` rows, because the unique `(OrderItemId, SourceKind, WarehouseId)` index ignores status and would otherwise reject re-adding the same source on a later save. Released rows stay only for allocations released on order close (where the order is immutable afterwards).
- Drafts store no allocation choices (`OrderDraftData` unchanged); restoring a draft requires re-selecting a source for every item, matching D10.
- The editor availability cache is fetched once per product per editing session; a stale cache can only over-estimate `Max`, the final save still validates against live availability and returns `"Conflict"`.

**Follow-up (2026-11-13):** the editor now lists only warehouses that actually hold stock (`StockOnHand > 0`), disables and clears the supplier selection once warehouse sources cover the full quantity (ignored on save, with an inline note), and makes every drawer input update immediately (`Immediate="true"` + live remainder/coverage/price summary); warehouse quantities clamp to `MaxQuantity` as they are typed.

---

### [x] T06 — Issue orders, batches, numbering

**Goal:** Manual per-warehouse issue orders with monthly numbering, batch execution by the current keeper, shortfall reasons, and cancel requests pending Manager approval.

**Depends on:** T05. **Delivers independently:** full issue → batch → deduct flow testable and usable.

**Locked decisions:** D8, D13, D18, SPEC §5.6, §6.4; numbering decision (design_draft § قرار ترقيم أوامر الصرف).

**Files**
- Modify `WorkRecordType.cs` (`StockIssueOrder = 4`), `NumberSequenceKind.cs` (`Warehouse = 5`), `NumberSequenceService.cs` (monthly allow-list)
- Modify `OrderHub.Infrastructure/Configurations/NumberSequenceConfiguration.cs` (recreate `ck_number_sequences_kind` / `ck_number_sequences_scope` — D18)
- Create `OrderHub.Domain/Models/StockIssueOrder.cs`, `StockIssueLine.cs`, `StockIssueBatch.cs` + configurations
- Create migration `..._AddStockIssueOrders`
- Create `IStockIssueOrderRepository` + repository (Reopsitories folder)
- Create `OrderHub.Application/Features/Inventory/IssueOrders/`: `CreateStockIssueOrder`, `GetIssueOrdersForSales`, `GetPendingForKeeper`, `GetIssueOrderDetails`, `RecordIssueBatch`, `RequestIssueOrderCancel`
- Create `OrderHub.Web/Components/Pages/Features/Inventory/IssueOrders/`: `Index.razor`, `Details.razor`, `CreateIssueOrderDialog.razor` (+ states)
- Modify `OrderWorkflow` order page to surface issue status per stocked item + the shortfall reasons
- Create `OrderHub.Tests/StockIssueOrderTests.cs`, `IssueBatchHandlerTests.cs`

**Steps**
1. `StockIssueOrder : ModelBase, IWorkRecord` — `NumberSequenceKind.Warehouse`, `NumberingPartyId = WarehouseId`, status history via the work-record pipeline; explicit `IWorkRecord` implementation like `DeliveryTrip`. Status machine: `Pending → (PartiallyExecuted ⇄) → Completed`, `Pending/PartiallyExecuted → CancelRequested → Cancelled`; no exit from terminal states.
2. Lines: `AllocationId` (unique `(issue_order_id, allocation_id)`), `RequestedQty` ≤ allocation's un-issued reservation — validated **inside the reservation-lock transaction at creation** (Q4; not by index alone). Batches: qty, actor, `EffectiveAtUtc`, `ShortfallReason?` — mandatory when the batch is final and Σ batches < requested.
3. Numbering (D18): `Warehouse` in the monthly allow-list with `party_id = WarehouseId`; the same migration recreates `ck_number_sequences_kind` and `ck_number_sequences_scope` (drop/add, no rows touched, `Down` restores the originals — the single declared additive-rule exception); tests cover per-warehouse independence, month restart, and a regression insert for all four legacy kinds.
4. Handlers: create (sales, `Orders.Write`, any number of orders per allocation over time); batch recording (**current keeper binding**, D13 locks + Version; one `IssueOut` movement + `ConsumeAsync` per batch in one transaction); cancel request (keeper, mandatory reason → `CancelRequested`; blocks new batches and new orders against the same allocations until decided — decided in T07); order cancel/archive auto-release hook completes here (system actor/reason) if not fully landed in T05.
5. Keeper screens: pending queue scoped to his warehouse (D1/D2 — no `Orders.Read`; order number text-only); batch dialog (quantities per line, reason on shortfall); details with lines/batches/actor trail. Sales screen: his created orders + statuses + reasons.
6. Tests: numbering + legacy-kind CHECK regression; line cap vs reservation (Q4) incl. concurrent create race; batch accumulation (Q5) + shortfall reason; `CancelRequested` blocks; terminal immutability; keeper binding rejection; `ProtectHistory` interplay for work-record history.

**Acceptance**
- Issue orders are work records with monthly per-warehouse numbers; the same reserved unit cannot be drawn into two active orders.
- Only the current keeper records batches; deduction and reservation-exit happen in one transaction.
- Sales sees reasons; keeper sees only his warehouse; no `Orders.Read` needed anywhere.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockIssueOrder"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~IssueBatch"
```
Manual: sales creates an order per warehouse → storekeeper records a partial batch with reason → sales sees it on the order page.

**Traps**
- `IWorkRecord` is internal — implement explicitly; `UnitOfWork` numbers added work records, never assign manually.
- The `CancelRequested` state must block batch recording **in the handler**, not only in the UI.
- Do not create a second numbering path beside `UnitOfWork`.

**Proposed commit:** `feat(inventory): add manual issue orders with batch execution`

**Decisions / files (2026-11-13)**
- Domain: `StockIssueOrder` (`IWorkRecord` implemented explicitly: `Warehouse` kind, `NumberingPartyId = WarehouseId`, explicit initial `Pending` history), `StockIssueLine` (unique allocation per order, `IssuedQty`/`RemainingQty` computed from batches), `StockIssueBatch` (actor, effective UTC, optional ≤ 300-char shortfall reason). Status machine inside the aggregate (`Pending ⇄ PartiallyExecuted → Completed`, `Pending/PartiallyExecuted → CancelRequested → Cancelled`, terminal states immutable; a final batch closes the order even when short). `OrderInventoryAllocation.Consume` registers Σ batches (Q5 cap).
- Enums: `NumberSequenceKind.Warehouse = 5`, `WorkRecordType.StockIssueOrder = 4`, `WorkRecordStatus.PartiallyExecuted = 5`, `CancelRequested = 6`. `NumberSequenceService` monthly allow-list and both CHECK constraints include `'Warehouse'`.
- Migration `20261113001132_AddStockIssueOrders`: D18 exception — drops/re-adds `ck_number_sequences_kind` and `ck_number_sequences_scope` (no rows touched), creates `stock_issue_orders` / `stock_issue_lines` / `stock_issue_batches` with CHECKs, FKs (`Restrict`) and indexes; `Down` removes the three tables and restores the original CHECK definitions verbatim. Scaffolded with `--startup-project OrderHub.Infrastructure`; `has-pending-model-changes` reports none.
- Application: `CreateStockIssueOrder` (`Orders.Write`; transaction → `LockBalancesAsync` → Q4 active-requested sum + pending-cancel block → persist), `RecordIssueBatch` (`Inventory.Write`; keeper binding from `Warehouse.KeeperUserId`, D13 locks, `Version` concurrency, one `IssueOut` movement + `ConsumeAsync` per new batch in the same transaction), `RequestIssueOrderCancel` (keeper, mandatory reason). Queries: `GetIssueOrders` (one scoped list: keeper's warehouse → all for `Inventory.Approve` → own requests; actionable statuses first), `GetIssueOrderDetails` (manager/requester/keeper scope + UI capability flags), `GetOrderIssueOrders` (`Orders.Read`, all statuses with lines/batches for the order page), `GetIssueOrderContext` (open orders with active warehouse allocations + per-warehouse available-to-request).
- Lifecycle: `OrderLifecycleHandlers` now also auto-cancels non-terminal issue orders when an order is delivered/cancelled/archived (system reason «إلغاء تلقائي: …», inside the existing release transaction).
- Web: `/inventory/issue-orders` list (scope label, search, status/quantities/reasons), details (lines, batches, shortfall/cancel audit, keeper actions), `CreateIssueOrderDialog` (order → warehouse → quantities clamped to available), `RecordIssueBatchDialog` (per-line quantity, final toggle, per-line shortfall reason, effective local time), `RequestIssueCancelDialog`; nav item «أوامر الصرف» under المخزون; `OrderWorkflow` issue-order section with per-item issued/requested, shortfall and pending-cancel reasons plus a create link.
- Tests: `StockIssueOrderTests` (numbering party/date, Q4 cap, batch accumulation + mandatory shortfall, keeper-binding rejection, pending-cancel blocks, lifecycle auto-cancel, terminal immutability) and `WorkRecordNumberingTests.Warehouse_kind_allocates_monthly_numbers_and_legacy_kinds_keep_working` (Warehouse + four legacy kinds, month restart/sequence independence). Full suite: 417 passed.

**Deviations**
- The plan's `GetIssueOrdersForSales` / `GetPendingForKeeper` are one `GetIssueOrders` query whose scope is resolved from the caller (keeper warehouse, `Inventory.Approve`, else requester), so keepers need no `Orders.Read`; `GetIssueOrderContext` was added for the creation dialog.
- `PartiallyExecuted`/`CancelRequested` were added to the shared `WorkRecordStatus` enum instead of a private enum; `WorkRecordLifecycle` (trips/shipments) is untouched because the richer machine lives in the aggregate.
- A final batch with a shortfall closes the order as `Completed` (remaining allocation stays active on the order); the shortfall reason is mandatory for every short line.
- `StockIssueOrder` carries an explicit `Version` concurrency token (in addition to balance locks) because batches mutate an aggregate whose lines/batches are separate rows.
- `IssueBatchHandlerTests.cs` was folded into `StockIssueOrderTests` (same harness, shorter feedback loop).

---

### [x] T06b — Warehouse details: balances, filters and movement ledger

**Goal:** The read side of a single warehouse: `/inventory` lists the warehouses in the caller's scope, `/inventory/warehouses/{id}` shows the header, per-product balances with server-side search/filter/sort/paging, and a per-product movement ledger dialog.

**Depends on:** T06 (movements, balances, issue orders). **Delivers independently:** balances and ledger usable; no write path and no migration.

**Owner decisions (2026-11-13):**
- Split from T06c; routes `/inventory` and `/inventory/warehouses/{id}` — never merged with `/inventory/balances`.
- Scope: Admin / `Inventory.Approve` / `Inventory.Read` → all warehouses; keeper-only → his assigned warehouse; a user who is both keeper and manager follows the manager rule. A keeper requesting another `WarehouseId` gets `"Forbidden"` — never silently ignored or replaced.
- Statuses: «متاح» `Available > 0 && Reserved == 0` · «محجوز جزئياً» `Available > 0 && Reserved > 0` · «محجوز بالكامل» `StockOnHand > 0 && Available == 0` · «نفد» `StockOnHand == 0`. Damaged is a separate figure, never part of available.
- `Available` is EF-ignored — every filter/sort/sum must use the SQL arithmetic `StockOnHand - ReservedOnHand`.
- Server-side search/filter/sort/paging only, page size ≤ 100, deterministic ordering, previous request cancelled in the UI, no JavaScript.
- Cards: scope totals ignore the text search; category/status filters produce filtered totals with a visible hint. «إخفاء الأصفار» is display-only. Show total `StockBalance` rows and rows with actual stock separately; zero rows are visible by default.
- Ledger: all nine movement types, latest first; receipt shows `ReferenceNote` (receipts have no number); issue reference via `SourceIssueBatchId → StockIssueLine → StockIssueOrder`; no invented references.
- Inactive warehouse: «تسجيل توريد» disabled; «أوامر الصرف» stays read-only; details/ledger available.
- Contracts: `GetAccessibleWarehousesQuery`, `GetWarehouseStockQuery`, `GetWarehouseLedgerQuery`; `InventoryPresentation.cs`; handlers check identity themselves (tests bypass the pipeline).
- Boundaries: no domain/business-rule changes, no permission-preset changes, no migrations, no changes to T06 issue-order behavior.

**Files**
- Modify `plans/INVENTORY-TASKS.md` (this entry)
- Create `OrderHub.Application/Features/Inventory/Presentation/InventoryPresentation.cs` (status label/kind helper, issue-number label reuse)
- Create `OrderHub.Application/Features/Inventory/WarehouseStock/` — `WarehouseStockContracts.cs` (status/sort enums, `GetAccessibleWarehousesQuery`, `GetWarehouseStockQuery`, result records), `InventoryScope.cs` (scope resolution helper), handlers
- Create `OrderHub.Application/Features/Inventory/WarehouseLedger/` — `GetWarehouseLedgerQuery` + result records + handler
- Modify `OrderHub.Application/Interfaces/Repositories/IInventoryRepository.cs` + `OrderHub.Infrastructure/Reopsitories/InventoryRepository.cs` (projection methods + counts + category options)
- Modify `OrderHub.Application/Interfaces/Repositories/IWarehouseRepository.cs` + `OrderHub.Infrastructure/Reopsitories/WarehouseRepository.cs` (scoped summaries, inactive included)
- Create `OrderHub.Web/Components/Pages/Features/Inventory/Balances/Index.razor` (+ `.razor.cs`), `Balances/WarehouseDetails.razor` (+ `.razor.cs`), `Balances/LedgerDialog.razor`
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor` («عرض الأرصدة»/«المخزون» → `/inventory`)
- Modify `OrderHub.Web/Components/Pages/Features/Warehouses/Index.razor` (row action «عرض الأرصدة» with `Inventory.Read`)
- Create `OrderHub.Tests/WarehouseBalancesTests.cs`, `WarehouseLedgerTests.cs`

**Steps**
1. Contracts: `StockStatusFilter { All, Available, PartiallyReserved, FullyReserved, OutOfStock }`, `StockSortColumn { Available (default), Physical, Reserved, Damaged, ProductName }`, `bool Descending`, page clamp `1..100`, tiebreak by `ProductId`.
2. Scope helper: `IsAdmin` or `Inventory.Approve` → all; keeper binding → own; other authenticated `Inventory.Read` → all; unauthenticated → `"Unauthorized"`; unknown warehouse → `"NotFound"`; keeper of another warehouse → `"Forbidden"`.
3. `GetAccessibleWarehousesQuery` → summaries (id, name, active, keeper, full totals, product count) for the scope; category options returned by the stock query (keepers lack `Categories.Read`).
4. `GetWarehouseStockQuery` → header, full totals, filtered totals, rows (product, unit, category, on-hand, reserved, available, damaged, latest movement time), `TotalCount` + `WithStockCount`, category options, paging metadata.
5. `GetWarehouseLedgerQuery` → product header, performed-by name, movement type/direction/status/quantity/effective+created time/note, receipt `ReferenceNote`, issue number parts; reuses `GetLedgerAsync`/`CountLedgerAsync`.
6. UI `/inventory`: `PageScaffold` «المخزون», scope label, cards, warehouse cards/table linking to details; `/inventory/warehouses/{id}`: header + status badges + actions («تسجيل توريد» disabled when inactive, «أوامر الصرف»), totals cards, search with 400 ms debounce + cancellation, category/status selects, «إخفاء الأصفار», sortable columns, `MudTable ServerData`, `TableFooter`, ledger dialog with paging.
7. Tests: scope matrix (admin/approve/read/keeper/keeper+manager/foreign keeper/unknown/unauth); status boundaries (0/partial/full/out); `HideZero` never changes totals; filtered vs full totals; search/sort/paging determinism and page clamp; category options without `Categories.Read`; ledger all nine types, receipt note, issue chain, ordering, paging; inactive warehouse readable but not writable.

**Acceptance**
- A keeper sees only his warehouse and cannot reach another by URL (`"Forbidden"`); a manager sees all.
- Balances match the ledger/`StockBalance` math; `Available` never derived client-side.
- Filters/paging are server-side; a stale response never overwrites a newer one.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WarehouseBalances"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WarehouseLedger"
dotnet build OrderHub.sln --configuration Release
```
Manual (browser): `/inventory` as Manager and keeper; details search/filter/sort/paging; ledger dialog; inactive warehouse actions.

**Traps**
- Never sort/filter on `StockBalance.Available` in EF (ignored property) — write the subtraction.
- Do not gate the balances query with `Warehouses.Read` (keepers only have it, sales/purchasing have `Inventory.Read`).
- Do not leak entity types; projection records only.

**Proposed commit:** `feat(inventory): add warehouse stock details and ledger`

**Decisions / files (2026-11-13)**
- Application: `WarehouseStock/` — `StockReading.cs` (`StockStatusFilter`, `StockSortColumn`, `StockPaging.Normalize` clamp `1..100`), `InventoryPresentation.cs` (status/movement/direction/status labels, `IssueOrderNumberLabel` `{n:D5}/{m:D2}/{y}`, `BuildLocation`), `InventoryScope.cs` (approve → all; keeper binding → own; other `Inventory.Read` → all; `NotFound` checked before `Forbidden`), `GetAccessibleWarehousesQuery`, `GetWarehouseStockQuery` (header, full totals, filtered totals, `HasFilteredView`, category options, `TotalCount`/`WithStockCount`, `IsKeeperScoped`, `CanRecordStockIn`).
- `WarehouseLedger/` — `GetWarehouseLedgerQuery` + `GetWarehouseLedgerPageAsync`/`CountLedgerAsync` reuse: newest first (`EffectiveAtUtc desc, Id desc`), receipt `ReferenceNote`, issue order via `SourceIssueBatchId → StockIssueBatch → StockIssueLine → StockIssueOrder`, reversal id; product stock null → `NotFound`.
- Repositories: `IWarehouseRepository.GetInventoryHeaderAsync/GetInventorySummariesAsync` (inactive included, null id list = all) and eight `IInventoryRepository` projection/count methods (search/category/status/hide-zero on `StockOnHand - ReservedOnHand`, sort with `ProductId` tiebreak, correlated latest-movement subquery).
- Web: `/inventory` (`Inventory.Read`) cards + scope label + keeper CTA; `/inventory/warehouses/{id}` `PageScaffold` header, totals, filtered-totals hint, 400 ms debounced search with cancellation, category/status selects, «إخفاء الأصفار», sortable server-side table, `TableFooter`; `LedgerDialog` paged server-side with stock summary and all nine movement types; nav «أرصدة المخزون» under المخزون; `Warehouses/Index` row action «عرض الأرصدة» gated by `Inventory.Read`.
- Tests: `WarehouseBalancesTests.cs` (scope matrix incl. keeper+manager and foreign keeper, status boundaries, arithmetic, search/full totals, filtered totals, paging clamp/determinism/sort, category options without `Categories.Read`, inactive readable) and `WarehouseLedgerTests.cs` (receipt note + performer, issue-order reference through the real batch pipeline, paging/scope/`NotFound`/`Unauthorized`, nine labels). Full suite: 429 passed (Debug + Release); `has-pending-model-changes` reports none.

**Deviations**
- `Balances/Index.razor` keeps its `@code` inline (no `.razor.cs`); `LedgerDialog` also inline — same convention as `Products/Index` and the Warehouses screens.
- Added `Balances/_Imports.razor` (folder-level usings) instead of repeating them per file.
- `Inventory/IssueOrders/_Imports.razor` gained the missing `Orders.Workflow` using: the T06 pages rendered `UserLocalTime` as an unknown element (RZ10012) so movement timestamps were invisible; no logic changed.
- Browser pass deferred: no dev server was running and the app needs an authenticated PostgreSQL session, so UI verification is still manual (see Verify).

---

### [x] T06c — Consolidated balances and product distribution

**Goal:** `/inventory/balances` consolidates every product across the caller's warehouse scope, with per-warehouse breakdown.

**Depends on:** T06b (scope helper, status semantics, presentation helpers). **Delivers independently:** cross-warehouse read screen; no write path, no migration.

**Owner decisions (2026-11-13):** same scope precedence and status table as T06b; Admin/approve/read see all warehouses, keeper sees his own; totals per the same rule (text search does not change scope totals); server-side filtering/paging; contracts `GetConsolidatedInventoryBalancesQuery` and `GetProductWarehouseDistributionQuery`.

**Files**
- Create `OrderHub.Application/Features/Inventory/ConsolidatedBalances/` (two queries + contracts + handlers, reusing `InventoryScope`)
- Modify `IInventoryRepository` + `InventoryRepository` (consolidated projection + per-product distribution)
- Create `OrderHub.Web/Components/Pages/Features/Inventory/Consolidated/Index.razor` (+ `.razor.cs`), `DistributionDialog.razor`
- Modify `NavMenu.razor` (item «أرصدة المخزون» → `/inventory/balances`)
- Create `OrderHub.Tests/ConsolidatedInventoryTests.cs`

**Steps**
1. Consolidated rows: product, unit, category, physical, reserved, available, damaged aggregated over the scope, warehouse count, latest movement; paged and sorted server-side with the T06b status semantics.
2. Distribution query: per product → per-warehouse on-hand/reserved/available/damaged (including zero rows), deterministic ordering.
3. UI: `PageScaffold` «أرصدة المخزون», cards, filters, «توزيع المخازن» dialog, warehouse links to T06b details when the caller has access.
4. Tests: scope aggregation correctness across two warehouses, keeper sees only his warehouse, distribution includes zero rows, totals/filters/paging parity with T06b.

**Acceptance**
- Aggregate equals the sum of per-warehouse balances for the same scope.
- Keeper never sees another warehouse's contribution; manager sees all.
- No JavaScript, no client-side filtering.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~ConsolidatedInventory"
dotnet build OrderHub.sln --configuration Release
```
Manual: `/inventory/balances` as manager vs keeper; drill into a product distribution.

**Proposed commit:** `feat(inventory): add consolidated stock balances`

**Decisions / files (2026-11-13)**
- Application: `ConsolidatedBalances/` — `ConsolidatedBalancesContracts.cs` (`ConsolidatedStockRow`, `ConsolidatedBalancesResult`, `ConsolidatedStockQueryFilter`, distribution records/projections), `GetConsolidatedInventoryBalancesQuery`, `GetProductWarehouseDistributionQuery` + handlers; both reuse `InventoryScope` (approve → all, keeper binding → own, other `Inventory.Read` → all) and `StockPaging.Normalize`.
- Aggregation is per product over the scope: SQL `GROUP BY product_id` with sums and row count; status and «إخفاء الأصفار» filter the **aggregated** values (`HAVING`), never a single warehouse row; text search/category filter before aggregation. Cards: full totals ignore search/hide-zero; category/status produce filtered totals + `HasFilteredView`. Zero rows stay visible by default.
- `Available` is always `StockOnHand - ReservedOnHand` in SQL; `LatestMovementAtUtc` is a second grouped query over the page's product ids inside the same scope (avoids correlated subqueries in grouped projections).
- Repositories: seven `IInventoryRepository` methods (totals, filtered totals, count, with-stock count, page, category options, per-product distribution); null warehouse list = every warehouse; page sort by aggregated column with `ProductId` tiebreak; distribution inner-joins scoped warehouses with balance rows (zero quantities included) and orders by warehouse name (ordinal) then id.
- Web: `/inventory/balances` — `PageScaffold` «أرصدة المخزون», scope label, 4 cards + product counters, filtered hint, 400 ms debounced search with cancellation, category/status selects, «إخفاء الأصفار», sortable server-side `MudTable` + `TableFooter` («عرض حسب المخزن» secondary action); «التوزيع» opens `DistributionDialog` (stock chips, per-warehouse rows, inactive badge, link to `/inventory/warehouses/{id}`).
- Nav: «أرصدة المنتجات» → `/inventory/balances` and «أرصدة المخازن» → `/inventory` under the المخزون group, so each screen (and `/inventory/warehouses/{id}`) highlights exactly one link.
- Tests: `ConsolidatedInventoryTests.cs` (7): cross-warehouse sum + per-warehouse parity, statuses and latest movement, keeper isolation, distribution zero rows/order/totals, keeper `NotFound`, search/category/status/hide-zero vs scope totals, paging clamp and name sort, anonymous `Unauthorized`. Full suite: **436 passed** (Debug + Release); `has-pending-model-changes` reports none.

**Deviations**
- NavMenu got two items instead of repointing the single «أرصدة المخزون» item: a shared `/inventory` item would prefix-match `/inventory/balances` and mis-highlight.
- No unit column: the domain has no unit field (same as T06b); rows show product + code, category and the four quantities.
- Distribution lists only warehouses that hold a balance row for the product (zero quantities included); a warehouse with no row is omitted so a keeper asking about a product absent from his warehouse gets `NotFound` rather than a fabricated zero row.
- The consolidated totals cards materialize one small row per product (3 ints) because EF cannot translate a second `GROUP BY` over a grouped query; grid filtering, ordering and paging remain fully SQL.
- Tests reuse the internal `StubPermissions` helper from `WarehouseBalancesTests`; the `FailingInventoryRepository` fake in `StockInHandlerTests` delegates the seven new members.
- Browser pass deferred (same reason as T06b): no dev server running and the app needs an authenticated PostgreSQL session.

---

### [x] T07 — Approvals: edit payload, issue-order cancel, reversal

**Goal:** The Manager approval queue: pending order edits applied from a stored payload, issue-order cancellation approval, and movement reversal — with the approver≠requester rule and the reversal exception.

**Depends on:** T06. **Delivers independently:** the sensitive-operation loop closes.

**Locked decisions:** D4, D6, D12, D21, SPEC §5.7, §6.6, §6.7.

**Files**
- Create `OrderHub.Domain/Models/InventoryApprovalRequest.cs` + configuration (+ migration `..._AddInventoryApprovals`; add real FKs for `SourceIssueOrderId` etc. where targets now exist)
- Create `OrderHub.Application/Features/Inventory/Approvals/`: `RequestOrderEditApproval` (integrated into `UpdateOrderHandler` branching), `RequestIssueOrderCancel` decision handler, `RequestMovementReversal` + decision, `GetApprovals`, `DecideApproval`
- Modify `OrderHub.Web/.../Inventory/Approvals/`: `Index.razor`, `EditPayloadReview.razor` (diff view), reversal request dialog (from the ledger screen)
- Modify the order editor to route edits-with-pending-issue-orders into the approval flow
- Create `OrderHub.Tests/ApprovalFlowTests.cs`, `MovementReversalTests.cs`

**Steps**
1. Entity per SPEC §5.7: `RequestType` (`AllocationEdit`, `IssueOrderCancel`, `MovementReversal`, `DamagedDisposal` reserved for T08), typed references, `Payload` JSON (AllocationEdit only), `Reason`, `Status` (`Pending/Approved/Rejected/Stale`), `Version`, `TargetVersion`, requester/decider/times, `DecisionNote` mandatory on reject.
2. Decision protocol (D21): one CAS `WHERE Status='Pending' AND Version=@v` — zero rows → `"Conflict"` with no side effects; re-validate the target in the same transaction (order state/version + no other pending request; `CancelRequested` still at `TargetVersion`; movement still `Active`; damaged balance version) and move the request to `Stale` on drift. Share the helper across all four request types.
3. Edit flow (D12): when an update touches items with allocations/pending issue orders, the handler stores the full proposed payload instead of applying; order + old allocations stay live; further edits blocked until decision/resubmit; on approval, one transaction — preserve issued (shortfall beyond new demand → «مرتجع مستحق» queue entry for T08), release un-issued, reserve new allocations under D13; failed reservation → edit stays pending, old state intact; new issue orders are **not** auto-created.
4. Issue-order cancel: decides `CancelRequested` → `Cancelled` + release un-issued only; rejection returns to its prior state with the recorded reason visible to the keeper.
5. Reversal (D6/D4): keeper requests with mandatory reason on an `Active` movement; approval requires `Inventory.Approve`; **`Approver ≠ Requester` except `MovementReversal`**; the reversal movement takes `EffectiveAtUtc = decision time` (never the original's date) and the original is marked `Reversed`; on approval one transaction — paired `Reversal` movement + `MarkReversed` + balance re-derivation (`ApplyIn`/`ApplyOut` inverse per direction, or recompute from ledger for the affected pair); already-reversed movement rejected; reservation-side effects of reversing an `IssueOut` batch must restore the reservation and reopen the line's issued accounting (define and test explicitly).
6. Approvals screen: queue by type with payload diff for edits; decide with reason; Arabic reasons everywhere; `Stale` rows show the system note and a resubmit action.
7. Tests: matrix (type × requester × approver incl. the reversal exception and the Manager-execution rejection); payload atomicity; failed new reservation keeps old state; reversal restores reservation correctly; double reversal rejected; second approval decision → `"Conflict"` with no side effects; drifted target → `Stale` and resubmission.

**Acceptance**
- No sensitive operation applies without approval; requester can never approve himself except reversals and a self-recorded count (T08).
- A rejected/failed edit leaves the original order and reservation exactly as they were.
- Reversal never deletes; the ledger stays append-only.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~Approval"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~MovementReversal"
```
Manual: edit an allocated order as sales → approve as Manager → confirm issued preserved + new issue order created manually; self-approval attempt rejected.

**Traps**
- Do not apply the edit payload partially outside one transaction.
- The reversal's reservation restore is the subtlest path in the feature — write the test first.
- `DecisionNote`/`Reason` lengths: match the ≤200 work-record reason convention.

**Proposed commit:** `feat(inventory): add manager approvals for edits, cancels and reversals`

**Decisions / files (2026-11-13)**
- Entity (created earlier in T05 phase): `InventoryApprovalRequest` with `RequestType` (`AllocationEdit`, `IssueOrderCancel`, `MovementReversal`, `DamagedDisposal` reserved), typed target refs, `Payload` JSON (AllocationEdit only), `Reason`, `Status`, `Version`, `TargetVersion`, requester/decider/times, `DecisionNote`; additive migration `AddInventoryApprovals` — no model changes since.
- Application `Features/Inventory/Approvals/`: `RequestOrderEditApproval` reached from `UpdateOrderHandler` when the order has issued/partially executed allocations — stores the full proposed payload and returns `"ApprovalRequired"` with the queued request; `RequestIssueOrderCancel` (keeper, reason → `CancelRequested` + pending request); `RequestMovementReversal` (keeper, reason, `Active` non-`Reversal` movement, one pending per movement); `GetApprovals` (`Inventory.Approve`), `GetApprovalDetails` (payload diff source), `DecideApproval` (`Inventory.Approve`, `TargetVersion` + `Version` CAS).
- Decision protocol (D21): one CAS `Status='Pending' AND Version=@v`; zero rows → `"Conflict"` with no side effects; target re-validated inside the same transaction (order version/state, no competing pending request, `CancelRequested` at `TargetVersion`, movement `Active`) and drift marks the request `Stale` with a system note instead of applying.
- Edit approval applies the stored payload in one transaction (`OrderEditApplier`): un-issued quantity released, issued preserved with excess moved to `PendingReturnQty` («مرتجع مستحق» for T08), new allocations reserved under D13 ascending (product, warehouse); a failed reservation rolls everything back — order, allocations and reservation unchanged and the request stays `Pending`. No issue orders are auto-created.
- Cancel approval → `Cancelled` + release un-issued only + allocation `Qty` shrunk to issued; rejection restores the prior status (`PartiallyExecuted`/`Pending`) with the decision note as the visible reason. `StockIssueOrder.Cancel` itself does not touch balances — the decision handler owns the release.
- Reversal: reversal movement `EffectiveAtUtc = decision time`, original → `Reversed`, decider recorded as actor, pairs `ApplyIn`/`ApplyOut` inverse to re-derive the balance; `IssueOut` batch reversal restores stock + reservation, rolls back `IssuedQty` accounting and reopens the order via `ReopenAfterBatchRemoval`. Self-approval allowed only for `MovementReversal`; every other type forbids approver = requester (`"Forbidden"`).
- Repository: `InventoryApprovalRepository` projection filters/orders on the entity before projecting (`ProjectRows(IQueryable<InventoryApprovalRequest>)`) — EF cannot translate predicates over the projected record.
- Web: `Approvals/Index.razor` (queue, type/status filters, requested-by labels), `DecideApprovalDialog` (اعتماد/رفض + note required on reject; `Stale`/`Conflict` → warning + reload), `EditPayloadReviewDialog` (current/proposed/delta table + section chips + reject reason), `RequestReversalDialog` opened from `LedgerDialog` «إجراءات» → «طلب عكس» (only `Active` non-`Reversal` rows; `CanRequestReversal` passed from `WarehouseDetails`), `Orders/Editor/OrderEditApprovalDialog` for the editor's reason. Editor `SaveAsync` branches on `"ApprovalRequired"`, shows «تم إرسال التعديل إلى المدير للاعتماد، ولن يُطبَّق حتى يصدر قراره.» and locks saving until reload.
- Nav: «الاعتمادات» → `/inventory/approvals` (visible with `Inventory.Approve`).
- Tests: `ApprovalFlowTests.cs` (12) and `MovementReversalTests.cs` (9) — reason required, payload queued with order untouched, second edit conflict, approved edit (issued preserved + `PendingReturnQty` + reservation), failed reservation rollback, self-approval forbidden (edit + cancel vs reversal allowed), second decision conflict, approved/rejected cancel, drift → `Stale`, queue projection, keeper/stranger matrix, double reversal, reversal-of-reversal, batch reversal restoring reservation/accounting, wrong expected version. Full suite: **457 passed** (Debug + Release); `has-pending-model-changes` reports none.

**Deviations**
- The registry's `EditPayloadReview.razor` is `EditPayloadReviewDialog.razor` (opened from the queue row) plus `DecideApprovalDialog.razor` for cancel/reversal, matching the existing `*Dialog` naming in `Features/Inventory`.
- Reversal-of-reversal is rejected in the backend too (`"لا يمكن طلب عكس حركة عكسية."`, `"Conflict"`), not just hidden in the ledger UI.
- `Stale` rows cannot be applied; the UI shows the system note and the user resubmits by redoing the edit / re-requesting the cancel or reversal (no separate "resubmit" command was added).
- A second `UpdateOrder` while an edit approval is pending returns `"Conflict"` («قيد الاعتماد») rather than replacing the payload — one pending request per order.
- Manual line: the editor now sends the reason inline via `OrderEditApprovalDialog`; the shared `OrderReasonDialog` returns an `OrderOperationReceipt`, so it could not be reused without changing its contract.
- Browser pass deferred: no dev server running and the app needs an authenticated PostgreSQL session, so the approvals/reversal screens are verified by build + tests only.

---

### [ ] T08 — Delivery records, returns/damaged, counts, rebuild, acceptance

**Goal:** Close the quantity chain: delivered tracking (trip and trip-less), returns with intact/damaged, counts with the late-movement review, and the Higher-Manager rebuild tool; then the release checklist.

**Depends on:** T07. **Delivers independently:** the feature is functionally complete and release-ready.

**Locked decisions:** D5, D7, D16, D19, D20, D21, SPEC §5.8–§5.10, §6.5, §6.7, Q6–Q12.

**Files**
- Create `OrderHub.Domain/Models/StockDeliveryRecord.cs`, `InventoryReturn.cs`, `InventoryReturnLine.cs`, `InventoryCount.cs`, `InventoryCountLine.cs` + configurations (+ migration `..._AddDeliveryReturnsCounts`)
- Create `OrderHub.Application/Features/Inventory/`: `Deliveries/RecordStockDelivery` (+ handler), `Returns/RecordReturn`, `Returns/RequestDisposal`, `Counts/StartCount`, `SaveCountLine`, `CompleteCount`, `ReviewLateMovement`, `ApproveCount`, `CancelCount`, `Rebuild/RebuildBalances`
- Create `OrderHub.Web/Components/Pages/Features/Inventory/`: `Returns/Index.razor` (+ `ReturnDialog`, `DisposalDialog`), `Counts/Index.razor` + `Editor.razor` + `Review.razor`, `Rebuild` (Higher-Manager-only page or `[Authorize(Roles="Admin")]` area per the existing pattern)
- Modify `OrderWorkflow` + `RecordStockDeliveryDialog.razor`; add «مرتجع مستحق» queue to the keeper views
- Create `OrderHub.Tests/DeliveryRecordTests.cs`, `InventoryReturnTests.cs`, `InventoryCountTests.cs`, `InventoryCountApprovalTests.cs`, `RebuildBalancesTests.cs`
- Create `docs/`-equivalent release artifact: `OrderHub.Tests/PostgresLockProof.md` (or `plans/INVENTORY-PG-LOCK-PROOF.md`) — the D13 manual lock script + observed results

**Steps**
1. `StockDeliveryRecord` (D16): order-linked, optional `TripId`/`ShipmentId`, `Quantity ≤ remaining issued` (validated under D13 locks), recorded by sales (`Orders.Write`); delivered/remaining computed by aggregation (Q6); dialog on the order workflow for trip-less paths too.
2. Returns (Q7): keeper records against the originating order — caps enforced; intact → `ReturnIn` + balance; damaged → `ReturnDamagedIn` + `DamagedIn` (Q8 path); «مرتجع مستحق» entries (from T07 shrinking edits) appear in the queue and enter the balance only after sorting; disposal: keeper request + reason → `DamagedDisposal` approval type → Manager decision → movement (≤ `DamagedQty`).
3. Counts (D20/D21): scope Full/Products; snapshot `BookQty` and `BookDamagedQty` per product from movements (`EffectiveAtUtc ≤ snapshot`); save never changes balance; one open count per (warehouse, product) (filtered unique + handler check); movements remain allowed while open; a line is approvable only when both counted values are entered; on approval, a late-dated movement pauses approval → Manager review (`IncludedInCount` → recompute both book balances/deltas, `Recounted` → line reopens); decision recorded; approval is a `Version` CAS on the count (second manager → `"Conflict"`) and creates up to two movements per line — `Adjustment` for `Delta` and `DamagedAdjustment` for `DamagedDelta`, each only if non-zero, applied to the **current** balance; blocked if the sound result would drop `StockOnHand` below `ReservedOnHand` or the damaged result would go below zero; the recorder may approve his own count; open counts follow the current keeper.
4. Rebuild tool (Higher-Manager only): recomputes `StockOnHand`/`DamagedQty` from ledger by `EffectiveAtUtc` and `ReservedOnHand` from active allocations − batches; records actor, time, before/after per row; audited.
5. Acceptance sweep + release checklist:
   - Full restore/build/test chain green; migration applied to a populated copy + rollback rehearsed (documented in the proof file).
   - **PostgreSQL lock proof (D13)**: scripted concurrent reservation attempt against a real PG instance demonstrating single-winner + `"Conflict"`; results recorded in `PostgresLockProof.md`.
   - Browser pass for all four roles (sales / storekeeper / manager / higher manager) + a legacy order without allocations.
   - Numbers: Latin digits everywhere (`CultureInfo.InvariantCulture`).

**Acceptance**
- The full chain holds: allocated → reserved → issued (batches) → delivered → returned → sorted; every invariant Q1–Q10 has a passing test.
- Counts adjust only on approval; late movements route through review; rebuild is HM-only and audited.
- Release checklist documented and green.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build
node OrderHub.Tests/Web/order-local-time.test.mjs
```
Manual: full scenario — stock-in → allocate → issue → batch (shortfall reason) → deliver → return (intact+damaged) → count → adjustment → reversal → rebuild sanity check.

**Traps**
- `Adjustment` delta is relative to the **count-time book balance** applied onto the **current** balance — the late-movement review exists precisely to prevent double-counting; test both branches.
- Do not let the delivery dialog appear for non-stocked items (they keep the current trip path untouched).
- The rebuild tool must never run automatically — explicit HM command only.

**Proposed commit:** `feat(inventory): add delivery tracking, returns, counts and rebuild tool`

---

## 4. Release checklist (after T08)

1. `dotnet restore` → Release build → full test suite → Node test, per the CI chain.
2. Migration applied to a populated copy; rollback rehearsed; results recorded.
3. PostgreSQL lock proof (D13) executed and recorded in `PostgresLockProof.md`.
4. Browser regression across roles; legacy order parity confirmed.
5. The owner runs all commits with the proposed Conventional Commit messages.
6. **No tags are pushed by agents** (release CI is owner-only per AGENTS.md).

*Registry v2.1 complete; implementation starts at T01 only after the owner approves this file.*
