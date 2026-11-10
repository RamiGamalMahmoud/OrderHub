# Inventory (Stock) — Executable Task Registry

- **Version:** 1.0 · 2026-11-09
- **Sources of truth:** `plans/INVENTORY-MANAGEMENT-SPEC.md` v1.3 · `plans/INVENTORY-PERMISSIONS-REVIEW.md` v1.2 · `AGENTS.md`
- **Status:** planning complete. **No inventory production code exists** (verified by repo-wide search). Every task below is unchecked.
- **Execution mode:** sequential vertical slices. Each task is self-contained: it names its goal, dependencies, exact files, steps, acceptance criteria and verification commands. A task is done only when its own acceptance criteria and the global definition of done pass.
- **Out of scope for this registry:** implementation approval. Do not start T01 until the owner approves this file.

---

## 0. How to execute a task

1. Read the task fully, plus the spec decisions it cites. Do not rely on memory of earlier tasks.
2. Implement Domain → Application → Infrastructure → Web in that order.
3. Run `dotnet restore`, then `dotnet build OrderHub.sln --configuration Release`, then the task's targeted tests, then the full suite.
4. Exercise the task's UI acceptance manually in the browser when the task adds or changes UI.
5. Mark the checkbox `[x]` and append a one-line deviation note only when the implementation differs from the task.
6. **Do not commit.** At the end, propose a Conventional Commit message (e.g. `feat(inventory): add warehouse registry`). The owner runs every commit.
7. Never edit a migration that already ran, never edit `SealedRoleSeedSql.V1`, never add JavaScript, never fetch fonts at runtime, never change `Supply`, delivery, documents, pricing or existing numbering.

### Global definition of done (applies to every task)

- `dotnet build OrderHub.sln --configuration Release` is green with zero new warnings introduced by the task.
- All tests pass, including the pre-existing suite.
- New tables/columns are additive-only. Existing columns, rows and enum encodings are untouched.
- All expected failures return `Result`/`Result<T>` with one of `"Unauthorized"`, `"Forbidden"`, `"NotFound"`, `"Conflict"`, `"InvalidOperation"`, `"Persistence"`. No throwing for expected failures.
- Permission gates exist in **both** layers: page/attribute for navigation and UX, and handler-level checks for direct invocation.
- Arabic, RTL UI. Numbers rendered with `CultureInfo.InvariantCulture`. No `MarkupString` for message previews.
- UI uses `PageScaffold`, `TableFooter`, `.oh-btn` and the one-primary-CTA rule; no dead buttons; every async action has a busy flag.
- No new JavaScript files. Clipboard/WhatsApp use the existing interop patterns only.
- Migrations are created with the repo's design-time factory (`DesignTimeAppDbContextFactrory.cs`) and timestamped after `20261108214724_SeedSealedRoles`.
- Seed/permission tests are updated in the same task that changes the matrix. Drift is never left for a later task.

### Build / test commands (from `OrderHub/`)

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

## 1. Final review — verified facts that constrain the tasks

The spec was re-checked against the current code. These are the facts that change or sharpen implementation; each is addressed by the task named on the right.

| # | Verified fact | Evidence | Consequence / task |
|---|---|---|---|
| FR-01 | Roles prerequisite is complete: `Approve`, sealed roles, Higher Manager gates, 4 presets / 31 cells | `PermissionLevel.cs`, `AuthorizationService.cs:16`, `20261108214724_SeedSealedRoles` | Inventory starts at T01; no roles work is repeated. |
| FR-02 | `Approve` satisfies `Write`, so a feature gate alone is not execution authority | `AuthorizationService.cs:26-28` | Execute/cancel/cancel-return/reversal-request **must** run the keeper-binding check in the handler (T11, T17, T18). |
| FR-03 | `SealedRoleSeedSql.V1` is frozen and tests assert 4 roles / 31 cells | `SealedRoleSeedSql.cs`, `SealedRoleSeedTests.cs:23-27` | Add `V2` in a new migration; update counts to 5 / 41 in T01. |
| FR-04 | Manager has `Users = None`, but must assign keepers | `SealedRolePresets.cs:66-81` | Minimal `Warehouses.Write`-gated candidate query in T01; never reuse `GetUsersQuery`. |
| FR-05 | `PermissionFeature` is stored as text; the enum currently has 14 members | `RolePermissionConfiguration.cs`, `PermissionFeature.cs` | Insert `Warehouses`/`Inventory` between `PaymentMethods` and `Logs`; no data migration needed (T01). |
| FR-06 | Role cards use a hard-coded switch for name/description/icon/tone | `RoleFeatureCatalog.cs` | Two new cases in all four methods in T01, otherwise English generic cards appear. |
| FR-07 | Order create/update hard-require a supplier for every item | `CreateOrderCommandHandler.cs:162-163`, `UpdateOrderHandler.cs:114-115` | Stock-only items cannot save until T10 changes the source rule. Supplier remains required for supplier-only and remainder items. |
| FR-08 | Order update replaces all items and EF cascades deletes on `order_items` | `UpdateOrderHandler.cs:59`, `Order.ReplaceOrderItems`, `OrderConfiguration.cs:101-105` | Item identity is not stable. Locking must compare `(ProductId, WarehouseId, stock qty)` allocations against pending issue-order lines, never item IDs (T10). |
| FR-09 | Order cancellation loops shipments/trips only; archive is allowed from `Processing` | `Order.Lifecycle.cs:109-117`, `CanArchive` at line 22 | D18 must hook **both** cancel and archive (T13). |
| FR-10 | Monthly numbering service rejects unknown kinds | `NumberSequenceService.cs:79-88` | Add `NumberSequenceKind.Warehouse` to the allow-list in T09. |
| FR-11 | `IWorkRecord` auto-numbering and status history are generic | `AppDbContext.cs:73-100`, `UnitOfWork.cs:53-102` | Issue orders reuse the pipeline in T09. `ProtectHistory` must also protect `InventoryMovement` (T05). |
| FR-12 | `User` has no phone; only the Higher-Manager Users page can edit users | `User.cs`, `Users/Edit.razor:13` | Add nullable `User.Phone` and edit it only there (T15); WhatsApp always keeps copy/picker fallback. |
| FR-13 | WhatsApp helpers already exist | `WhatsAppMessaging.cs` (`TryNormalize`, `BuildDirectUrl`, `BuildPickerUrl`) | Reuse in T15; no new normalizer, no new JS. |
| FR-14 | There is no notification infrastructure; the earlier app-shell decision explicitly omitted a bell because no service existed | `MainLayout.razor` | T14 introduces the entity, handlers and bell; recipient filtering is in the handler, not the UI. |
| FR-15 | SQLite tests use `EnsureCreated` + snake_case, not migrations | `RbacTestDb.cs`, `AGENTS.md` testing model | Seed SQL must run in test helpers explicitly; new EF columns must survive snake_case (all tasks). |
| FR-16 | Repositories live in the misspelled `OrderHub.Infrastructure/Reopsitories/`; some classes use namespace `...Repositories` | `AGENTS.md`, `OrderRepository.cs` | Match the folder and namespace of the file you touch; do not "fix" the typo. |
| FR-17 | `Warehouse`/`StockItem`/`InventoryMovement`/`StockIssueOrder` do not exist anywhere in source | repo-wide grep (only plans match) | T02/T05/T09 create them from scratch; no partial implementation is being extended. |

### Review conclusions carried into the plan

- **Approve levels never execute.** Execution/cancel authority is `IsAdmin || warehouse.KeeperUserId == currentUser.Id`, evaluated against the **current** keeper at action time. `AssignedUserId` is audit only.
- **Reservations and issue orders are created only on final order save.** Drafts never reserve. Supplier-only orders keep the legacy path and require no inventory permission.
- **Old orders stay loadable.** New `OrderItem` columns are nullable/defaulted; the "every saved item must have a source" rule is enforced in code at save time only.
- **One pending issue order per (order, warehouse)**; cancelled/completed rows remain as history. Enforce with a filtered unique index and re-check in the handler.
- **The cached balance is display-only.** Every change goes through a movement plus an atomic balance update in the same transaction; rebuild is phase B.
- **No hard deletes** for warehouses, movements, purchases, returns or reversals. Errors are fixed by cancel (pending work) or approved reversal (executed work).

---

## 2. Dependency graph

```
T01 Permissions + seed + keeper candidates
 ├─► T02 Warehouse domain/persistence ─► T03 Warehouses application ─► T04 Warehouses UI + nav
 ├─► T05 Ledger + StockItem ─► T06 Stock-in application ─► T07 Inventory read queries ─► T08 Inventory UI + nav
 ├─► T09 StockIssueOrder domain/numbering
 │        └─► T10 Order sourcing + availability + editor + auto-issue + locks
 │                 └─► T11 Issue-order execute/cancel handlers ─► T12 Issue-order UI + nav
 │                          ├─► T13 D18 auto-cancel on order cancel/archive
 │                          └─► T14 Notifications + bell
 │                                   └─► T15 User.Phone + WhatsApp keeper message
 └────────────────────────────────────────► T16 Phase A acceptance sweep

Phase B: T05/T06/T10/T11 ─► T17 Returns ─► T18 Reversals ─► T19 Rebuild + aggregate reporting + Phase B acceptance
```

Rules:
- T04 depends on T03; T08 depends on T07 and T03; T12 depends on T11.
- T10 requires T05–T07 (balances) and T09 (issue orders) before it can be executed.
- T14 modifies the issue-order creation handlers from T10 and the execution handler from T11; do not start it earlier.
- Phase B starts only after T16 is green.

---

## 3. Phase A — tasks

### [ ] T01 — Permission surface, sealed presets V2, keeper candidate query

**Goal:** Make `Warehouses` and `Inventory` first-class permission features, seed the locked 5-role / 41-cell preset matrix, and give warehouse managers a safe way to list keeper candidates without `Users` access.

**Depends on:** nothing. **Delivers independently:** role editor shows 16 Arabic cards; seeded roles have the locked access; Manager can call the candidate query; all existing tests still pass.

**Locked decisions:** D22, D23, §7.1–§7.3, §7.6.

**Files**
- Modify `OrderHub.Domain/Enums/PermissionFeature.cs`
- Modify `OrderHub.Domain/Authorization/SealedRolePresets.cs`
- Modify `OrderHub.Infrastructure/Migrations/SealedRoleSeedSql.cs` (add `V2`; **do not touch `V1`**)
- Create migration `OrderHub.Infrastructure/Migrations/<timestamp>_AddInventoryPermissions.cs` (timestamp after `20261108214724`)
- Modify `OrderHub.Web/Components/Pages/Features/Administration/Roles/Editor/RoleFeatureCatalog.cs`
- Create `OrderHub.Application/Features/Warehouses/GetKeeperCandidates/GetWarehouseKeeperCandidatesQuery.cs` + handler
- Create `OrderHub.Application/Features/Warehouses/GetKeeperCandidates/WarehouseKeeperCandidate.cs`
- Modify `OrderHub.Application/Interfaces/Repositories/IUserRepository.cs` and `OrderHub.Infrastructure/Reopsitories/UserRepository.cs`
- Modify `OrderHub.Tests/RbacTestDb.cs` and `OrderHub.Tests/SealedRoleSeedTests.cs`
- Create `OrderHub.Tests/WarehouseKeeperCandidateTests.cs`

**Steps**
1. Add enum members in this exact position — after `PaymentMethods`, before `Logs`:
   - `[Description("المخازن")] Warehouses`
   - `[Description("المخزون")] Inventory`
2. Update `SealedRolePresets`:
   - `Sales`: append `Warehouses=Read`, `Inventory=Read` (11 cells total).
   - `Purchasing`: append `Warehouses=Read`, `Inventory=Write` (8 cells total).
   - `Manager`: append `Warehouses=Write`, `Inventory=Approve` (13 cells total).
   - Add `StorekeeperKey = "storekeeper"` and preset «أمين مخزن» with exactly `Warehouses=Read`, `Inventory=Write`, `Products=Read`, `Orders=Read` (4 cells).
   - `DataEntry` stays at 5 cells. Expected total: **41**.
   - Existing custom roles are untouched; `IsReservedName` automatically covers the new preset through `All`.
3. Add `SealedRoleSeedSql.V2`:
   - Insert the `storekeeper` role only if neither `role_key` nor a role with the same name exists.
   - Insert only the 10 new cells (`sales` 2, `purchasing` 2, `manager` 2, `storekeeper` 4), each guarded by `WHERE NOT EXISTS (role_id + feature)`.
   - Use `CURRENT_TIMESTAMP`, `TRUE`, and text feature/level names exactly like V1. Keep it provider-neutral (PostgreSQL + SQLite).
4. Create the migration and add a single `migrationBuilder.Sql(SealedRoleSeedSql.V2)` (both `Up` and no destructive `Down`; `Down` must not delete seeded roles).
5. `RbacTestDb.SeedAsync` runs `V1` then `V2`. Update `SealedRoleSeedTests`:
   - 5 roles / 41 cells;
   - manager 13, sales 11, purchasing 8, storekeeper 4, data-entry 5;
   - `storekeeper` name reserved and sealed;
   - idempotency after two runs is still 5 / 41;
   - name-collision test still skips the `sales` preset: with the pre-existing custom «مبيعات» role (1 cell), the expected total is **5 roles / 31 cells** (custom 1 + purchasing 8 + data-entry 5 + manager 13 + storekeeper 4, and no sales cells); assert no role has `role_key = 'sales'`.
6. Candidate query:
   - `WarehouseKeeperCandidate(int Id, string DisplayName, bool IsActive, bool HasInventoryWrite)`.
   - `[RequirePermission(PermissionFeature.Warehouses, PermissionLevel.Write)]`.
   - Query handler returns all users ordered by `DisplayName`; `HasInventoryWrite` is true when `IsAdmin` or the role level for `Inventory` is `Write` or `Approve`.
   - Repository method loads `User.Role.Permissions`; never expose username, password hash, role name or phone.
7. Catalog entries:
   - `Warehouses`: name «المخازن», description «إدارة المخازن وأمناء المخزون وحالة التشغيل», icon `Icons.Material.Filled.Warehouse`, tone `bg-amber-50 text-amber-600`.
   - `Inventory`: name «المخزون», description «أرصدة المنتجات وحركات المخزون وأوامر الإخراج», icon `Icons.Material.Filled.Inventory`, tone `bg-teal-50 text-teal-600`.
8. Tests: the candidate handler returns minimal fields; Manager gets `HasInventoryWrite=true` (Approve); a storekeeper gets true; a sales user gets false; an unauth/unprivileged user is denied by the pipeline; admin is marked true.

**Acceptance criteria**
- `PermissionFeature` has 16 members; the roles editor renders 16 cards with Arabic labels and four segments, without any code change to the matrix component.
- Seed totals are exactly 5 sealed roles / 41 cells; V2 is idempotent; V1 text is byte-for-byte unchanged.
- A Manager can obtain keeper candidates; a Sales user cannot (feature gate); the payload exposes no sensitive fields.
- `storekeeper` appears as a sealed ready role and resists rename/delete/permission edits through the existing guards.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~SealedRoleSeedTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WarehouseKeeperCandidateTests"
```
Manual: open `/admin/roles` as Higher Manager; confirm 16 cards, `أمين مخزن` is read-only, and sales/purchasing/manager show the new cells.

**Traps**
- Do not insert the two enum members inside the Administration block; text storage means position is only compile-time, but the spec fixes it.
- Do not give the candidate query `Users.Read`; do not reuse `GetUsersQuery`.
- Do not update V1 to add cells; a new database runs V1 then V2.
- Counts changed; any other test asserting 31 cells must be found and updated in the same task.

**Proposed commit:** `feat(inventory): add warehouse and inventory permissions with sealed storekeeper preset`

---

### [ ] T02 — Warehouse domain and persistence

**Goal:** Introduce the `Warehouse` aggregate with exactly one keeper, soft activity state and no hard delete, plus its EF mapping, repository and migration.

**Depends on:** T01 (permission surface exists, though the entity itself does not need it). **Delivers independently:** warehouses can be created/loaded/tested; no UI yet.

**Locked decisions:** D4, D5, §5.1, §13.1 (new tables may be strict).

**Files**
- Create `OrderHub.Domain/Models/Warehouse.cs`
- Create `OrderHub.Infrastructure/Configurations/WarehouseConfiguration.cs`
- Modify `OrderHub.Infrastructure/AppDbContext.cs` (add `DbSet<Warehouse>`)
- Create `OrderHub.Application/Interfaces/Repositories/IWarehouseRepository.cs`
- Create `OrderHub.Infrastructure/Reopsitories/WarehouseRepository.cs`
- Modify `OrderHub.Infrastructure/Reopsitories/ServiceCollectionExtensions.cs`
- Create migration `..._AddWarehouses.cs`
- Create `OrderHub.Tests/WarehouseDomainTests.cs`

**Steps**
1. Entity (`Warehouse : ModelBase`):
   - `Name` as `EntityName` (same value object as `City`).
   - `KeeperUserId` (`int`, required) and `Keeper` navigation.
   - `IsActive` (default true).
   - Constructor `Warehouse(string name, int keeperUserId)` and `Rename`, `AssignKeeper`, `Deactivate`, `Activate`.
   - Validation: name through `EntityName`; keeper id `> 0`; deactivation is idempotent; no `Delete` method and no public setters.
   - Optional `ChangeKeeper(int keeperUserId)` must not clear audit data (none exists yet).
2. EF configuration:
   - Table `warehouses`; owned unique `name` as `varchar(100)`; `keeper_user_id` required; FK to `users` with `DeleteBehavior.Restrict`; index on `keeper_user_id`.
   - `is_active` required, `created_at`/`modified_at` from `ModelConfigurationBase`.
3. Repository:
   - `GetAllAsync()` (tracked-off, ordered by name);
   - `GetByIdAsync(int id)`;
   - `Add(Warehouse warehouse)`;
   - `AnyWithNameAsync(string name, int? excludingId = null)`;
   - `GetWithKeeperAsync(int id)` for handlers.
4. Register `IWarehouseRepository` in the existing repository registration file (match its namespace).
5. Migration `AddWarehouses` only creates the `warehouses` table and indexes; no changes to `users`.
6. Tests (`WarehouseDomainTests`): valid construction; blank name rejected; keeper id 0 rejected; rename; deactivate/activate idempotent; EF round-trip preserves name/keeper/active; deleting a referenced user is restricted (FK behavior).

**Acceptance criteria**
- A warehouse requires a positive keeper id and a non-empty unique name.
- There is no way to hard-delete a warehouse from the domain or repository.
- `EnsureCreated` on SQLite produces `warehouses` with snake_case columns and the FK; the migration applies additively on PostgreSQL.
- Existing tests remain green.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WarehouseDomainTests"
```

**Traps**
- `EntityName` is an owned type; mirror `CityConfiguration`, not a plain string mapping.
- Do not add a `Version` column to `Warehouse`; concurrency handling is not part of D4 and last-write-wins is acceptable for this registry.
- Keep the repository in the misspelled `Reopsitories` folder; match the namespace style of the file you edit.

**Proposed commit:** `feat(inventory): add warehouse aggregate and persistence`

---

### [ ] T03 — Warehouses application layer (list, detail, create, update, activity)

**Goal:** Provide the CQRS surface that the warehouses page needs, with duplicate-name, keeper-validity and activity rules enforced in handlers.

**Depends on:** T01, T02. **Delivers independently:** all warehouse operations are callable and testable without UI.

**Locked decisions:** D4, D5, D23, §5.1, §7.4.

**Files**
- Create under `OrderHub.Application/Features/Warehouses/`:
  - `GetWarehouses/GetWarehousesQuery.cs` + handler + `WarehouseListItem.cs`
  - `GetWarehouse/GetWarehouseQuery.cs` + handler + `WarehouseDetails.cs`
  - `CreateWarehouse/CreateWarehouseCommand.cs` + handler
  - `UpdateWarehouse/UpdateWarehouseCommand.cs` + handler
- Create contracts `OrderHub.Application/Features/Warehouses/Contracts/WarehouseInputValidation.cs` if shared validation is useful.
- Create `OrderHub.Tests/WarehouseCommandHandlerTests.cs`

**Steps**
1. `GetWarehousesQuery` → `IReadOnlyList<WarehouseListItem>` with `Id`, `Name`, `KeeperUserId`, `KeeperDisplayName`, `IsActive`. Gate `[RequirePermission(Warehouses, Read)]`.
2. `GetWarehouseQuery(int Id)` → `WarehouseDetails` (same fields plus `KeeperIsActive`, `KeeperHasInventoryWrite`). Gate `Warehouses.Read`; return `null` or a failure for a missing id consistently with existing read handlers.
3. `CreateWarehouseCommand(string Name, int KeeperUserId)` with `[RequirePermission(Warehouses, Write)]`:
   - authenticated actor required;
   - trimmed name non-empty and unique (case-insensitive check through `AnyWithNameAsync`);
   - keeper exists and is active (`IUserRepository` needs a minimal `GetByIdAsync`/`GetActiveByIdAsync` if absent);
   - save via `IUnitOfWork`;
   - return `Result<int>`; map `PersistenceException` to a friendly failure.
4. `UpdateWarehouseCommand(int Id, string Name, int KeeperUserId, bool IsActive)` with `[RequirePermission(Warehouses, Write)]`:
   - load warehouse or `NotFound`;
   - duplicate-name check excluding self;
   - keeper exists and is active; assigning an inactive keeper fails `"InvalidOperation"`;
   - keeper without `Inventory.Write`/`Approve` is **allowed** (the UI warns); the handler does not block it;
   - deactivation is allowed for now; a guard for stock/pending work is added by T06/T09 (documented in the handler as an extension point, but do not add dead abstractions);
   - no hard delete command.
5. Tests: create success; duplicate name fails; inactive/unknown keeper fails; update renames/reassigns; deactivation persists; handler authorization attributes exist; unauthorized actor returns failure/denied.

**Acceptance criteria**
- All four operations are executable and return `Result`-based outcomes.
- Keeper identity is validated in the handler, not only in the UI.
- No delete path exists.
- The list/detail contracts contain no username, phone or role data.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WarehouseCommandHandlerTests"
```

**Traps**
- Do not inject `ICurrentUser` only for display; check authentication explicitly.
- `GetWarehouseKeeperCandidatesQuery` from T01 remains the only source for the dropdown; do not duplicate it here.
- Keep the response codes consistent with existing handlers (`"Unauthorized"`, `"NotFound"`, `"InvalidOperation"`, `"Persistence"`).

**Proposed commit:** `feat(inventory): add warehouse application handlers`

---

### [ ] T04 — Warehouses page and navigation group

**Goal:** Ship the first usable inventory screen: a warehouse list with create/edit dialog, keeper selection, activity control and the locked warning, plus the new navigation group.

**Depends on:** T01, T03. **Delivers independently:** the owner can manage warehouses end to end in the browser.

**Locked decisions:** D4, D5, D23, §7.7, §8.1, UI conventions in `AGENTS.md`.

**Files**
- Create `OrderHub.Web/Components/Pages/Features/Warehouses/Index.razor` (+ `.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Warehouses/Editor/WarehouseEditorDialog.razor`
- Create `OrderHub.Web/Components/Pages/Features/Warehouses/Editor/WarehouseEditorState.cs`
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor`

**Steps**
1. Page `/warehouses` with `[PermissionAuthorize(PermissionFeature.Warehouses, PermissionLevel.Read)]`; `PageScaffold` title «المخازن», description «إدارة المخازن وأمناء المخزن وحالة التشغيل», breadcrumb.
2. List card: `MudTable`/`MudDataGrid` with name, keeper display name, activity badge (green «نشط» / slate «متوقف»), pending-work hint placeholder only when data exists (do not fabricate counts). Search in memory; `TableFooter` with 1-based page conversion.
3. Primary CTA «إضافة مخزن» rendered only when `PermissionAccess.IsAllowedAsync(Warehouses, Write)`; opens the editor dialog. Secondary row action «تعديل».
4. Editor dialog follows `CityEditorDialog` conventions (icon header, close, white section cards, footer readiness dot + إلغاء/حفظ):
   - name (required, max 100);
   - keeper `MudSelect` fed by `GetWarehouseKeeperCandidatesQuery`; show «متوقف» for inactive users;
   - amber inline warning when the selected candidate has `HasInventoryWrite = false`: «المستخدم المحدد لا يملك صلاحية المخزون؛ لن يستطيع تنفيذ أوامر الإخراج حتى تُمنح له.» Saving remains allowed;
   - activity toggle for existing warehouses (create defaults active);
   - validation blocks empty name/keeper; busy flag on save; success snackbar and reload.
5. No hard-delete control. Deactivation is the only way to retire a warehouse.
6. Navigation: extend the existing `Groups` array with group «المخزون», icon `Icons.Material.Filled.Warehouse`, subtitle «المخازن والأرصدة وأوامر الإخراج», `ViewAllHref="/warehouses"`. Add only the «المخازن» item now (`/warehouses`, `Warehouses.Read`, description «المخازن وأمناء المخزن»). Later tasks append their items; never leave a link to a non-existent page.
7. Manual acceptance with a Manager account and a storekeeper-role account.

**Acceptance criteria**
- A user with `Warehouses.Read` sees the page and list; without it, the nav group and page are hidden/denied.
- Create/edit works; duplicate names and invalid keepers show Arabic inline errors.
- The warning appears for a keeper without inventory access but does not block saving.
- Exactly one primary CTA on the page; all busy states disable repeats.
- No JavaScript added; RTL layout correct on desktop and mobile.

**Verify**
```powershell
dotnet build OrderHub.sln --configuration Release
dotnet run --project OrderHub.Web
```
Manual checklist: `/warehouses` as Manager (HM seeded) → create → edit keeper → deactivate → nav visibility as Sales (read-only? Sales has `Warehouses.Read`: list visible, no CTA) and as Data Entry (group hidden).

**Traps**
- The nav group must be hidden when none of its items is allowed; the existing `GroupVisible` logic handles it.
- Do not add a fake count badge.
- Use `MudSelect`/`MudDialog` only for data/widget behavior; page layout stays HTML + Tailwind.
- Keeper candidates require `Warehouses.Write`; do not call the query for read-only users (hide the dialog).

**Proposed commit:** `feat(inventory): add warehouses management page and navigation`

---

### [ ] T05 — Inventory ledger and cached stock balance (domain + persistence)

**Goal:** Add the immutable movement ledger and the per-(warehouse, product) cached balance, with atomic reserve/release/consume operations that later tasks use.

**Depends on:** T02. **Delivers independently:** movements and balances can be recorded and tested; no UI or stock-in flow yet.

**Locked decisions:** D3, D12, D15, D21, §5.2, §5.3, §13.1.

**Files**
- Create `OrderHub.Domain/Enums/InventoryDirection.cs` (`In = 1`, `Out = 2`)
- Create `OrderHub.Domain/Enums/InventoryMovementType.cs` (`StockIn = 1`, `IssueOut = 2`, `CustomerReturnIn = 3`, `Reversal = 4`)
- Create `OrderHub.Domain/Enums/InventoryMovementStatus.cs` (`Active = 1`, `Reversed = 2`)
- Create `OrderHub.Domain/Models/InventoryMovement.cs`
- Create `OrderHub.Domain/Models/StockItem.cs`
- Create `OrderHub.Infrastructure/Configurations/InventoryMovementConfiguration.cs`
- Create `OrderHub.Infrastructure/Configurations/StockItemConfiguration.cs`
- Modify `OrderHub.Infrastructure/AppDbContext.cs` (DbSets + `ProtectHistory`)
- Create `OrderHub.Application/Interfaces/Repositories/IInventoryRepository.cs`
- Create `OrderHub.Infrastructure/Reopsitories/InventoryRepository.cs`
- Modify `OrderHub.Infrastructure/Reopsitories/ServiceCollectionExtensions.cs`
- Create migration `..._AddInventoryLedger.cs`
- Create `OrderHub.Tests/InventoryLedgerTests.cs`

**Steps**
1. `InventoryMovement`:
   - Fields: `WarehouseId`, `Direction`, `MovementType`, `ProductId`, `Quantity` (`> 0`), `SourcePurchaseId?`, `SourceOrderIssueId?`, `SourceReturnId?`, `ReversalOfMovementId?`, `Status` (Active), `Note?`, `CreatedByUserId`, `CreatedAtUtc`, `Version`.
   - Factory `Create(...)` validates quantity and actor; `Reversal` requires `ReversalOfMovementId` and a non-empty note; non-reversal movements cannot set that id.
   - Only `internal MarkReversed()` changes status. No public setters, no delete.
2. `StockItem`:
   - Fields: `WarehouseId`, `ProductId`, `StockOnHand`, `ReservedOnHand` (both `>= 0`), `Version`.
   - Behaviors: `ApplyIn(qty)`, `ApplyOut(qty)` (throws/fails on negative), `Reserve(qty)`, `Release(qty)`, `Consume(qty)` (stock and reserved both decrease; never below zero); each increments `Version`.
   - One row per `(warehouse_id, product_id)`; unique index.
3. EF configurations:
   - `inventory_movements`: enums stored as strings; FKs to warehouse/product with `Restrict`; no FK for the three optional source ids (documents and issue orders are new tables; use real FKs where the target exists in the same migration, otherwise plain nullable ints); unique filtered index on `reversal_of_movement_id` where not null; check constraints `quantity > 0`, `stock`/`reserved >= 0`; index `(warehouse_id, product_id, created_at)` and `(product_id, created_at)`.
   - `stock_items`: unique `(warehouse_id, product_id)`; checks `stock_on_hand >= 0`, `reserved_on_hand >= 0`; `version` concurrency token (or atomic updates; pick one and test it).
4. `AppDbContext.ProtectHistory` must reject `Modified`/`Deleted` entries for `InventoryMovement` (same exception text pattern as existing history protection).
5. Repository interface/implementation:
   - `GetStockItemAsync(warehouseId, productId)` (tracked), `GetStockAsync(int? warehouseId)` (no-tracking list);
   - `Add(InventoryMovement)`, `AddStockItem(StockItem)` (or `GetOrCreateStockItemAsync`);
   - `TryReserveAsync(warehouseId, productId, qty)` — atomic update guarded by `stock_on_hand - reserved_on_hand >= qty`; returns false when no row/insufficient;
   - `ReleaseAsync(warehouseId, productId, qty)` and `ConsumeAsync(warehouseId, productId, qty)` — atomic, never negative;
   - `GetLedgerAsync(warehouseId, productId, skip, take)` and `CountLedgerAsync`.
   - All operations must work on both PostgreSQL and SQLite (use `ExecuteUpdateAsync` or provider-neutral EF; if a provider-specific SQL is unavoidable, add the SQLite branch and test it).
6. Migration creates both tables, indexes and checks. No changes to `order_items` or products.
7. Tests: quantity/note validation; mark reversed; stock apply/reserve/release/consume boundaries; EF round-trip; unique stock row; history protection rejects edits/deletes; atomic reserve returns false on the second over-book; ledger ordering.

**Acceptance criteria**
- No API can produce a negative `StockOnHand`/`ReservedOnHand` or a zero/negative movement.
- Movements cannot be modified or deleted after insert (protected in `AppDbContext`).
- Reserve is atomic and race-safe at the database level.
- `EnsureCreated` + snake_case works in tests; migration is additive.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~InventoryLedgerTests"
```

**Traps**
- Do not add a `Version` to `InventoryMovement` and also try to update it; the ledger is immutable except `Status` transition to `Reversed`, which is internal to a reversal approval (phase B).
- `StockItem.Version` is for optimistic concurrency; if atomic `ExecuteUpdate` is used, increment it in the same update.
- Do not create a generic "inventory service" abstraction; keep operations on the repository used by T06/T10/T11.

**Proposed commit:** `feat(inventory): add stock ledger and cached balances`

---

### [ ] T06 — Stock-in application (record purchase into a warehouse)

**Goal:** Let an authorized user record a stock-in purchase that creates one `StockIn` movement and increases the cached balance per line, atomically.

**Depends on:** T05. **Delivers independently:** stock can be entered and verified through handlers/tests; UI arrives in T08.

**Locked decisions:** D2, D21, D26, §5.4, §7.4.

**Files**
- Create `OrderHub.Domain/Models/StockPurchase.cs`
- Create `OrderHub.Domain/Models/StockPurchaseItem.cs`
- Create `OrderHub.Infrastructure/Configurations/StockPurchaseConfiguration.cs` (+ item configuration)
- Modify `OrderHub.Infrastructure/AppDbContext.cs` (DbSets)
- Create `OrderHub.Application/Features/Inventory/RecordStockIn/RecordStockInCommand.cs` + handler + contracts (`StockInLineInput.cs`, `StockInReceipt.cs`)
- Modify `OrderHub.Application/Interfaces/Repositories/IInventoryRepository.cs` / `InventoryRepository.cs` as needed
- Create migration `..._AddStockPurchases.cs`
- Create `OrderHub.Tests/RecordStockInHandlerTests.cs`

**Steps**
1. Domain:
   - `StockPurchase`: `WarehouseId`, optional `SupplierId`, optional `Note` (max 500), `RecordedByUserId`, `RecordedAtUtc`, lines collection. Factory validates at least one line, no duplicate products (or aggregate duplicates deterministically — choose aggregation and test it), quantities `> 0`.
   - `StockPurchaseItem`: `ProductId`, `Quantity`.
   - No cancel-after-record; mistakes are corrected by reversal in phase B. No update method.
2. Command `RecordStockInCommand(int WarehouseId, int? SupplierId, string? Note, IReadOnlyList<StockInLineInput> Lines)` with `[RequirePermission(Inventory, Write)]`.
3. Handler:
   - authenticated actor required;
   - **`Products.Read` required** through `IAuthorizationService` (D26) → `"Forbidden"`;
   - warehouse exists and `IsActive`, else `"InvalidOperation"`/`"NotFound"`;
   - supplier, when provided, must exist (use the existing supplier reader/repository; do not add `Suppliers.Read` as a gate — the field is optional and the storekeeper preset has no supplier access, so the UI hides it);
   - all product ids exist; unknown product → `"InvalidOperation"` with the product name if available;
   - begin a transaction; add the purchase; `SaveChangesAsync` to assign `Id`; for each line get-or-create the stock item, apply in, and add a `StockIn` movement with `SourcePurchaseId`;
   - commit; on any failure roll back and return `"Persistence"`.
4. Extend the warehouse deactivation rule from T03: `UpdateWarehouseCommand` now rejects deactivation when any `StockItem.StockOnHand > 0` exists for the warehouse (`"InvalidOperation"`: «لا يمكن إيقاف مخزن يحتوي على رصيد.»). Add a repository query.
5. Tests: successful multi-line stock-in creates movements/balance; missing `Products.Read` forbidden; inactive warehouse rejected; unknown product rejected; optional supplier saved; failure rolls back stock and movements; duplicate lines handled per the chosen rule; deactivation guard.

**Acceptance criteria**
- Every recorded line has exactly one `StockIn` movement and an increased balance, in one transaction.
- `Inventory.Write` alone cannot bypass the `Products.Read` prerequisite.
- No manual balance editing exists.
- Deactivation is blocked while positive stock remains.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~RecordStockInHandlerTests"
```

**Traps**
- The purchase id is needed for the movement reference; it is acceptable to save twice inside the transaction.
- Do not reuse the order-bound `Supply` entity (D2/D16).
- Stock-in is not keeper-bound; purchasing and manager may record it for any active warehouse.
- Use `CultureInfo.InvariantCulture` only in presentation; handler quantities are integers.

**Proposed commit:** `feat(inventory): record stock-in purchases with ledger movements`

---

### [ ] T07 — Inventory read queries (balances, aggregate, ledger)

**Goal:** Provide the read models for balances per warehouse, aggregated totals per product, and a product ledger drill-down.

**Depends on:** T05. **Delivers independently:** all inventory data can be queried and tested; UI arrives in T08.

**Locked decisions:** D3, D4, D29, §8.2.

**Files**
- Create `OrderHub.Application/Features/Inventory/GetBalances/GetInventoryBalancesQuery.cs` + handler + `InventoryBalanceRow.cs`, `InventoryProductTotal.cs`
- Create `OrderHub.Application/Features/Inventory/GetLedger/GetProductLedgerQuery.cs` + handler + `ProductLedgerEntry.cs` + `PagedResult<T>` (reuse an existing paged contract if one exists)
- Create `OrderHub.Application/Features/Inventory/Contracts/IInventoryReader.cs`
- Create `OrderHub.Infrastructure/Features/Inventory/InventoryReader.cs` + registration
- Create `OrderHub.Tests/InventoryReadTests.cs`

**Steps**
1. `GetInventoryBalancesQuery(int? WarehouseId)` with `[RequirePermission(Inventory, Read)]`:
   - rows: warehouse id/name, product id/name/category, `StockOnHand`, `ReservedOnHand`, `Available = StockOnHand - ReservedOnHand`, `IsWarehouseActive`;
   - when `WarehouseId` is null, return all warehouses;
   - separate aggregate list: product id/name, total stock, total reserved, total available across warehouses;
   - order by warehouse name, then product name.
2. `GetProductLedgerQuery(int WarehouseId, int ProductId, int Page, int PageSize)`:
   - `[RequirePermission(Inventory, Read)]`;
   - entries: date, direction label (in/out), type label, quantity, source reference (purchase/issue/return/reversal id, or null), actor display name, note, status;
   - order newest first; page size 25 default; capped at 100;
   - totals row (sum in, sum out, current balance) if the existing paged pattern supports it.
3. Reader implementation joins `stock_items`, `warehouses`, `products` (and category if available) and `inventory_movements`/`users`; no per-row N+1.
4. Do not expose cost/value; the column is a units balance (D3).
5. Tests: availability math, warehouse filter, company-wide rows, aggregate totals, ledger mapping/actor, paging bounds, empty state.

**Acceptance criteria**
- A Sales user with `Inventory.Read` sees all warehouses and products, no cost data.
- A user without `Inventory.Read` cannot call either query.
- Ledger entries are read-only projections and contain no entity references.
- Numbers are formatted in the UI later; contracts stay numeric.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~InventoryReadTests"
```

**Traps**
- Do not compute balances by summing the ledger per row; read the cached balance (display) and keep the ledger for drill-down.
- Do not scope by keeper: D29 is company-wide read.
- Use no-tracking queries.

**Proposed commit:** `feat(inventory): add balances and ledger read queries`

---

### [ ] T08 — Inventory page, stock-in page, and navigation items

**Goal:** Ship the read screens and the stock-in screen so balances, ledger and recording stock-in are usable in the browser.

**Depends on:** T03, T04, T06, T07. **Delivers independently:** `/inventory` and `/inventory/stock-in` work with real data.

**Locked decisions:** D26, D29, §7.7, §8.2, §8.3.

**Files**
- Create `OrderHub.Web/Components/Pages/Features/Inventory/Index.razor` (+ `.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Inventory/Balances/LedgerDialog.razor`
- Create `OrderHub.Web/Components/Pages/Features/Inventory/StockIn/StockIn.razor` (+ `.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Inventory/StockIn/StockInLineState.cs`
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor`

**Steps**
1. `/inventory` with `[PermissionAuthorize(Inventory, Read)]`:
   - `PageScaffold` title «المخزون», description «أرصدة المنتجات في المخازن وحركات المخزون»;
   - warehouse filter (`MudSelect`, «كل المخازن» default);
   - summary tiles (total products, total units, total reserved) from the aggregate data;
   - table: warehouse, product, category, stock, reserved, available; available highlighted amber when 0 and rose never (no negative values possible);
   - row action «السجل» opens `LedgerDialog` with the product ledger and paging;
   - aggregate section/tab «إجمالي المنتجات» listing product totals across warehouses;
   - primary CTA «تسجيل توريد» only when `Inventory.Write`; no other primary.
2. `/inventory/stock-in` with `[PermissionAuthorize(Inventory, Write)]`:
   - if the user lacks `Products.Read`, show a blocking Arabic card («تحتاج صلاحية عرض المنتجات لاختيار الأصناف») and no form; handler already enforces it;
   - warehouse select (active only), optional supplier select only when `Suppliers.Read` exists (otherwise hide the field), note (max 500);
   - line rows: product `MudAutocomplete` (needs `Products.Read`), quantity; add/remove line; totals units; validation;
   - submit → `RecordStockInCommand`; success snackbar, reset form, navigate to `/inventory`; busy flag; failure banner with the handler message.
3. Nav: append «المخزون» (`/inventory`, `Inventory.Read`, description «أرصدة المخازن والحركات») and set the group create CTA «تسجيل توريد» (`/inventory/stock-in`, `Inventory.Write`, icon Add). Keep «المخازن» from T04.
4. Manual acceptance: seed stock via the form; verify balances, aggregate, ledger, permission hiding.

**Acceptance criteria**
- Balances and ledger match the handler data exactly; no client-side arithmetic beyond display totals.
- Stock-in works for Purchasing (Write + Products.Read) and Storekeeper; is denied for Sales.
- The stock-in page has one primary CTA («تسجيل التوريد»); the inventory page has one («تسجيل توريد») and it disappears without Write.
- No dead controls when a prerequisite permission is missing; hints are Arabic.
- RTL and mobile layout correct; numbers use `CultureInfo.InvariantCulture`.

**Verify**
```powershell
dotnet build OrderHub.sln --configuration Release
dotnet run --project OrderHub.Web
```
Manual: `/inventory` → filter → ledger → `/inventory/stock-in` → record 2 lines → `/inventory` reflects both; repeat as Sales/Storekeeper.

**Traps**
- Do not show a supplier picker without `Suppliers.Read`; the storekeeper preset has none.
- Do not fetch all products client-side for the autocomplete if an existing product search query exists; reuse it.
- The ledger dialog must not use `MarkupString`.
- Keep page-level permission attributes; the write CTA is an additional check, not a replacement.

**Proposed commit:** `feat(inventory): add inventory and stock-in pages`

---

### [ ] T09 — Stock issue order domain, numbering and persistence

**Goal:** Add the `StockIssueOrder` work record with monthly per-warehouse numbering, pending lines, execute/cancel transition rules and status history, ready for order-save integration.

**Depends on:** T02, T05. **Delivers independently:** issue orders can be created in tests, numbered, transitioned and persisted; no producers or UI yet.

**Locked decisions:** D7, D8, D10, D20 (data model only), §5.5.

**Files**
- Modify `OrderHub.Domain/Enums/WorkRecordType.cs` (add `StockIssueOrder = 4`)
- Modify `OrderHub.Domain/Enums/NumberSequenceKind.cs` (add `Warehouse = 5`)
- Modify `OrderHub.Infrastructure/Services/NumberSequenceService.cs` (allow `Warehouse` in `NextMonthlyAsync`; `NextGlobalAsync` unchanged)
- Create `OrderHub.Domain/Models/StockIssueOrder.cs`
- Create `OrderHub.Domain/Models/StockIssueOrderLine.cs`
- Create `OrderHub.Infrastructure/Configurations/StockIssueOrderConfiguration.cs` (+ line configuration)
- Modify `OrderHub.Infrastructure/AppDbContext.cs` (DbSets)
- Create `OrderHub.Application/Interfaces/Repositories/IStockIssueOrderRepository.cs`
- Create `OrderHub.Infrastructure/Reopsitories/StockIssueOrderRepository.cs`
- Modify `OrderHub.Infrastructure/Reopsitories/ServiceCollectionExtensions.cs`
- Create migration `..._AddStockIssueOrders.cs`
- Create `OrderHub.Tests/StockIssueOrderTests.cs` (+ numbering test additions)

**Steps**
1. `StockIssueOrder : ModelBase, IWorkRecord`:
   - `OrderId`/`Order`, `WarehouseId`/`Warehouse`, `Status` (`Pending` initial);
   - `WorkRecordNumber`, `SequenceYear`, `SequenceMonth`, `RequestedByUserId`, `RequestedByUserName`, `RequestedAtUtc`, `AssignedUserId` (request-time snapshot), `ExecutedByUserId?`, `ExecutedAtUtc?`, `CancelledByUserId?`, `CancelledByUserName?`, `CancelReason?`, `Version`;
   - lines collection `StockIssueOrderLine` (`ProductId`, `ProductName`, `RequestedQty`, `ExecutedQty`, `Id`).
   - Explicit `IWorkRecord` members: `NumberSequenceKind = Warehouse`, `NumberingPartyId = WarehouseId`, number assignment validated to the same warehouse, status history like `Supply` with `WorkRecordType.StockIssueOrder`.
   - `TransitionTo(WorkRecordStatus target, string? reason, DateTime? atUtc)` restricted to `Pending → Completed/Cancelled`; cancel requires a reason (≤200); completed records executor/time; no transition from Completed/Cancelled.
   - `AddLine` allowed only while Pending; no duplicate `ProductId` in one order (aggregate at creation).
2. EF mapping:
   - tables `stock_issue_orders` and `stock_issue_order_items`;
   - FK to orders (`Restrict`), warehouses (`Restrict`), products (`Restrict`), users (`Restrict`, nullable for system cancel);
   - **filtered unique index** `(order_id, warehouse_id) WHERE status = 'Pending'` (SQLite and PostgreSQL both support it; verify `EnsureCreated`);
   - unique index `(warehouse_id, sequence_year, sequence_month, work_record_number)` where number > 0;
   - checks: `requested_qty > 0`, `executed_qty >= 0 AND executed_qty <= requested_qty`, reason length ≤200.
3. Numbering: add `Warehouse` to the monthly allow-list; unit test that two warehouses get independent sequences and that a new month restarts at 1. Existing numbering behavior must not change.
4. Repository: `Add`, `GetByIdAsync` (with warehouse + lines), `GetPendingForOrderAsync(orderId)`, `GetAllForOrderAsync`, `GetPendingForWarehouseAsync`, paged list support for T11.
5. Migration additive only.
6. Tests: creation validation, duplicate line aggregation, numbering per warehouse/month, status history initial Pending, cancel reason required, completed/cancelled terminal, EF round-trip, filtered pending uniqueness, `ProtectHistory` behavior for work-record history remains intact.

**Acceptance criteria**
- Issue orders are work records with monthly per-warehouse numbers and a Pending initial history row.
- A second Pending issue order for the same `(order, warehouse)` is rejected by the database and by the repository/domain.
- Completed and cancelled issue orders are terminal and immutable except audit fields set at transition time.
- Existing `Supply`/`DeliveryTrip` numbering and tests are unchanged.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockIssueOrderTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~WorkRecordNumberingTests"
```

**Traps**
- `IWorkRecord` is internal; implement it explicitly like `Supply`/`DeliveryTrip`.
- Do not add a global sequence; D8 is monthly per warehouse.
- `UnitOfWork` auto-numbers added work records on save; do not also assign a number manually.
- Do not add `Inventory = Approve` behaviour here; authority is T11.

**Proposed commit:** `feat(inventory): add stock issue order work records`

---

### [ ] T10 — Order item sourcing: contracts, availability, editor UI, issue-order creation, reservation and locks

**Goal:** Let order items explicitly source quantity from a warehouse with the remainder from a linked supplier; create/reserve issue orders on final save; lock allocated items until the pending issue order is cancelled.

**Depends on:** T05, T06, T07, T09. **Delivers independently:** a user can create/edit an order with stock sourcing and see the resulting pending issue order and reservation.

**Locked decisions:** D1, D6, D7, D15, D17, D25, §5.9, §6.2.

**Files**
- Modify `OrderHub.Domain/Models/OrderItem.cs`
- Modify `OrderHub.Domain/Models/Order.cs` (`AddOrderItem` signature)
- Modify `OrderHub.Application/Features/Orders/Contracts/OrderInput.cs`
- Modify `OrderHub.Application/Features/Orders/Create/CreateOrderCommandHandler.cs`
- Modify `OrderHub.Application/Features/Orders/Update/UpdateOrderHandler.cs`
- Modify `OrderHub.Application/Features/Orders/GetForEdit/OrderEditorItem.cs` and its reader (`OrderHub.Infrastructure/Features/Orders/*`)
- Modify `OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorData.cs`, `GetOrderItemEditorHandler.cs`, `IOrderItemEditorReader.cs`, `OrderItemEditorReader.cs`
- Modify `OrderHub.Application/Features/Orders/GetItemsEditor/*` if the editor loads availability in bulk
- Modify `OrderHub.Web/Components/Pages/Features/Orders/Editor/State/OrderItemEditorState.cs`
- Modify `OrderHub.Web/Components/Pages/Features/Orders/Editor/OrderEditor.razor.cs` (`BuildOrderInput`, validation)
- Modify `OrderHub.Web/Components/Pages/Features/Orders/Editor/Products/ProductEditorDrawer.razor` and `ProductsStep.razor`
- Modify `OrderHub.Web/Components/Pages/Features/Orders/Editor/OrderDetails/DetailsStep.razor.cs` if it aggregates validation
- Create `OrderHub.Application/Features/Inventory/GetAvailability/GetStockAvailabilityQuery.cs` + handler + contracts (if not folded into item editor reads)
- Create `OrderHub.Tests/OrderSourcingTests.cs`, `OrderSourcingPersistenceTests.cs`

**Steps**
1. Domain (`OrderItem`):
   - Add `WarehouseId` (`int?`), `StockQuantity` (`int`, default 0), `Warehouse` navigation.
   - Extend `Create` and `Order.AddOrderItem` with trailing optional parameters `int? warehouseId = null, int stockQuantity = 0`.
   - Validation: `0 <= StockQuantity <= Quantity`; `WarehouseId` required when `StockQuantity > 0`; `WarehouseId` must be null when `StockQuantity == 0`; supplier required when `StockQuantity < Quantity`; supplier may be null when the whole quantity comes from stock.
   - `SupplierName` remains snapshot data. No methods mutate stock fields directly; replacements recreate items.
2. Contract: extend `OrderInput.Item` with trailing `int? WarehouseId = null, int StockQuantity = 0`. Do not reorder existing parameters (positional call sites in the editor and drafts).
3. Editor read contracts:
   - `OrderEditorItem` gains `WarehouseId`, `StockQuantity`, `IsStockLocked`, `PendingIssueOrderId`.
   - The order-edit reader returns the persisted values and lock state from pending issue-order lines.
   - Item-editor data gains per-warehouse availability: warehouse id/name, on-hand, reserved, available; only when the caller has `Inventory.Read`. Otherwise return an empty list and a `CanReadInventory = false` flag.
4. Availability semantics:
   - New order: selectable stock quantity ≤ available.
   - Existing order with the same warehouse/product still pending: selectable ≤ `available + item.StockQuantity` (its own reservation is already inside `ReservedOnHand`).
   - Availability is live at load; the save handler re-validates (D15).
5. Handler validation (create and update):
   - if any item has `StockQuantity > 0`, require `Inventory.Read` through `IAuthorizationService` (D25); otherwise `"Forbidden"` with an Arabic message;
   - supplier remainder must be linked to the product (`Product.Suppliers`); use the product repository/reader; unknown link → `"InvalidOperation"`;
   - stock-only items no longer require a supplier;
   - supplier-only items still require a supplier (legacy behavior);
   - warehouse must exist and be active.
6. Create handler integration:
   - group stock allocations by warehouse; for each group create one `StockIssueOrder` (requested-by = current user, assigned = current keeper) with aggregated lines;
   - atomically reserve each allocation; on failure return `"Conflict"` with an Arabic message and roll back the transaction;
   - do the work inside the existing order transaction; let `UnitOfWork` assign issue-order numbers on save.
7. Update handler integration:
   - load pending issue orders for the order, build the **locked allocation map** `(WarehouseId, ProductId) → RequestedQty` from their lines;
   - build the desired map from the incoming items;
   - if any locked key changes quantity or disappears → `"InvalidOperation"`: «يجب إلغاء أمر الإخراج أولاً قبل تعديل كمية المخزون لهذا البند.» (D7);
   - for new allocations: reserve and append lines to an existing Pending order for the same warehouse, or create a new Pending order; never create a second Pending order for the same pair;
   - supplier-only changes remain free;
   - on stock changes, run the whole save under a transaction; release nothing (locked allocations cannot be removed);
   - never compare `OrderItem.Id` (items are replaced wholesale).
8. UI:
   - `OrderItemEditorState`: add `WarehouseId`, `StockQuantity`, `PersistedWarehouseId`, `PersistedStockQuantity`, `IsStockLocked`, `PendingIssueOrderId`, `WarehouseOptions`; include them in `CreateWorkingCopy`.
   - Product drawer: quantity; a «المصدر» section with warehouse select and «الكمية من المخزون» (0 = supplier-only); show available per warehouse; compute the supplier remainder and require a supplier only when remainder > 0; restrict suppliers to the linked list (already present).
   - If `CanReadInventory` is false (D25): hide availability, disable stock controls, show «لا تملك صلاحية عرض المخزون؛ سيُحفظ البند عبر مورد فقط.» and force `StockQuantity = 0`.
   - If `IsStockLocked`: disable quantity/stock/warehouse controls for that item, show «هذا البند مرتبط بأمر إخراج معلّق» with a link to `/inventory/issue-orders` (T12 route; until then link to the list route only after T12 exists — if T12 is not yet implemented, show the text without a link, then add the link in T12).
   - `BuildOrderInput` passes the new fields; final validation mirrors the handler.
9. Tests:
   - stock-only saves without supplier and creates issue order + reservation;
   - supplier-only still requires supplier and creates no issue order;
   - split item creates a line for the stock portion only;
   - stock quantity above availability → `"Conflict"`, no order/issue/reservation;
   - no `Inventory.Read` → `"Forbidden"` and no save;
   - update with unchanged allocations succeeds;
   - update that changes a locked allocation is rejected;
   - update that adds a new stock allocation appends/creates the pending issue order;
   - legacy supplier-only order edit remains possible;
   - two concurrent reservations over the last units → exactly one succeeds;
   - product-supplier link enforced.

**Acceptance criteria**
- Every saved item has a valid source; the rule is code-only (columns nullable).
- Stock-only items save end to end; the issue order appears Pending with the right warehouse/lines; `ReservedOnHand` reflects it.
- Locked items cannot change without cancelling the pending issue order first; the error is Arabic and actionable.
- Users without `Inventory.Read` get supplier-only behavior and cannot bypass it by calling the command directly.
- No existing order-editor behavior regresses for supplier-only orders.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~OrderSourcing"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~OrderPricingTests"
```
Manual: create an order with a split item; open `/inventory` and confirm the reservation; edit the order and confirm the lock; cancel the pending issue order (T11) and confirm the item unlocks.

**Traps**
- Do not compare order-item ids; `ReplaceOrderItems` clears the old rows.
- Do not require `Inventory.Read` for supplier-only saves.
- Do not create issue orders from drafts; only from final create/update.
- The item's stock quantity is the issue line `RequestedQty`; there is no per-line movement at save.
- Reservations must be in the same transaction as the order save; do not leave a reservation if the order save fails.
- Keep the trailing optional parameters to avoid touching every positional call site.

**Proposed commit:** `feat(inventory): source order items from warehouse stock`

---

### [ ] T11 — Issue-order execution and cancellation handlers (keeper-bound)

**Goal:** Implement the pending-issue-order workflow: list/detail queries, execute with live balance validation, and cancel with a mandatory reason, all bound to the current warehouse keeper or a Higher Manager.

**Depends on:** T05, T09, T10 (data exists). **Delivers independently:** issue orders can be executed/cancelled through handlers and tests.

**Locked decisions:** D9 (approval is not execution), D10, D15, D20, §6.1, §7.5.

**Files**
- Create under `OrderHub.Application/Features/Inventory/IssueOrders/`:
  - `GetStockIssueOrders/GetStockIssueOrdersQuery.cs` + handler + `StockIssueOrderListItem.cs`
  - `GetStockIssueOrder/GetStockIssueOrderQuery.cs` + handler + `StockIssueOrderDetails.cs`
  - `ExecuteStockIssueOrder/ExecuteStockIssueOrderCommand.cs` + handler
  - `CancelStockIssueOrder/CancelStockIssueOrderCommand.cs` + handler
- Create `OrderHub.Application/Features/Inventory/IssueOrders/StockIssueOrderCancellationService.cs` (shared by manual cancel and T13 auto-cancel)
- Modify `OrderHub.Application/Interfaces/Repositories/IStockIssueOrderRepository.cs` (paged list, detail, pending)
- Create `OrderHub.Tests/StockIssueOrderHandlerTests.cs`, `StockIssueOrderPermissionTests.cs`

**Steps**
1. Queries:
   - list with optional status/warehouse filter, search, paging; fields: number, order number, warehouse, keeper display name, requested-by name, requested at, line count, total requested qty, status;
   - detail: all header fields, lines (product, requested, executed), warehouse keeper id/name, order link data, status history;
   - both gated `[RequirePermission(Inventory, Read)]`;
   - detail/list include `CanAct` computed as `IsAdmin || warehouse.KeeperUserId == currentUser.Id` (no sensitive data).
2. Execute command `(int IssueOrderId, int ExpectedVersion)` with `[RequirePermission(Inventory, Write)]`:
   - authenticated; load issue order + warehouse + lines (tracked); `CheckVersion`;
   - status must be Pending (`"InvalidOperation"` otherwise);
   - **keeper binding**: `currentUser.IsAdmin || warehouse.KeeperUserId == currentUser.Id`, else `"Forbidden"`;
   - transaction; for each line, ensure `StockOnHand >= RequestedQty` (D15); if not, return `"Conflict"` and roll back with no movement;
   - atomically consume stock/reservation and add one `IssueOut` movement per line (`SourceOrderIssueId`, actor/time);
   - set line `ExecutedQty = RequestedQty`, transition to `Completed` with executor/time;
   - commit; return a receipt (id, status, executed at).
   - `Inventory = Approve` alone is **not** authority; Manager must also be the current keeper.
3. Cancel command `(int IssueOrderId, int ExpectedVersion, string Reason)` with `[RequirePermission(Inventory, Write)]`:
   - same authentication/version/Pending/binding gates;
   - reason required, trimmed, ≤200 (D10);
   - release every line's reservation atomically; no movement;
   - transition to `Cancelled` with actor/reason/time;
   - commit.
4. Shared cancellation service: `CancelPendingForOrderAsync(orderId, reason, actor)` used by T13; it loads pending orders, applies the same release + transition, and records the system actor when `actor` is system. Keep the manual handler and the service on one code path for release logic.
5. Tests:
   - current keeper executes; reservation drops; stock drops; `IssueOut` movements created; status/history correct;
   - non-keeper with `Inventory.Write` → `"Forbidden"`, no writes;
   - Manager with `Inventory.Approve` but not keeper → `"Forbidden"`;
   - Higher Manager executes and is audited;
   - keeper transfer: old keeper rejected, new keeper accepted on the same pending order;
   - insufficient live stock at execution → `"Conflict"`, no partial movements, status still Pending;
   - double execute/cancel rejected;
   - cancel requires reason, releases reservation, writes no movement;
   - detail `CanAct` matches the binding rule.

**Acceptance criteria**
- Only the current keeper or a Higher Manager can execute/cancel; this is enforced inside the handler, not only in the UI.
- Execution is all-or-nothing across lines and never makes a balance negative.
- Cancellation always releases reservations and requires a reason.
- `Inventory = Approve` never grants execution.
- Version conflicts return `"Conflict"`.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockIssueOrderHandlerTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockIssueOrderPermissionTests"
```

**Traps**
- Do not use `AssignedUserId` for authority; use `Warehouse.KeeperUserId` at action time.
- Do not add movements on cancel.
- Do not allow partial execution in phase A.
- Do not let the UI be the only keeper check; forced command calls must fail in the handler.

**Proposed commit:** `feat(inventory): execute and cancel stock issue orders with keeper binding`

---

### [ ] T12 — Issue-order list and detail pages

**Goal:** Give keepers and readers the issue-order screens with the correct action visibility, mandatory cancel reason, busy states and order links.

**Depends on:** T11. **Delivers independently:** issue orders are operable in the browser.

**Locked decisions:** D7, D10, D20, §7.7, §8.4.

**Files**
- Create `OrderHub.Web/Components/Pages/Features/Inventory/IssueOrders/Index.razor` (+ `.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Inventory/IssueOrders/Details.razor` (+ `.razor.cs`)
- Create `OrderHub.Web/Components/Pages/Features/Inventory/IssueOrders/CancelIssueOrderDialog.razor`
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor`
- Modify `OrderHub.Web/Components/Pages/Features/Orders/Editor/Products/ProductEditorDrawer.razor` (add the real link to `/inventory/issue-orders` for locked items)

**Steps**
1. `/inventory/issue-orders` with `[PermissionAuthorize(Inventory, Read)]`:
   - filters: status (all/pending/completed/cancelled), warehouse, search by number/order number;
   - columns: number, order number, warehouse, keeper, requested by, date, lines, requested qty, status badge (brand pending, emerald completed, rose cancelled);
   - `TableFooter`, 1-based page conversion; row opens `/inventory/issue-orders/{id:int}`.
2. `/inventory/issue-orders/{IssueOrderId:int}`:
   - header cards: warehouse, keeper, requested by/at, status, order link `/orders/{orderId}`;
   - lines table: product, requested, executed;
   - timeline/status history (source, status, reason, date);
   - actions: «تنفيذ» primary and «إلغاء» danger, visible only when `CanAct` and Pending;
   - execute uses a confirmation dialog showing counts and the live-balance warning; on `"Conflict"` show the Arabic handler message and reload;
   - cancel opens `CancelIssueOrderDialog` with a required reason (counter ≤200), calls the command, busy flag;
   - read-only users see no actions and no disabled dead buttons.
3. Nav: append «أوامر الإخراج» under the inventory group (`/inventory/issue-orders`, `Inventory.Read`, description «تنفيذ وإلغاء أوامر صرف المخزون»).
4. Locked-item link in the order editor becomes active now.
5. Manual acceptance as keeper, Manager-non-keeper, Higher Manager, and Sales.

**Acceptance criteria**
- Action visibility matches `CanAct`; forced calls are still rejected by T11.
- Execute and cancel show correct success/failure feedback; double clicks are impossible while busy.
- Status tones follow the four-hue system; no new colors.
- One primary CTA per page/section.
- No JS; RTL/mobile correct.

**Verify**
```powershell
dotnet build OrderHub.sln --configuration Release
dotnet run --project OrderHub.Web
```
Manual: create an order with stock, open the pending order as its keeper, execute it, verify `/inventory` balance and the status; repeat with cancellation and verify the order item unlocks.

**Traps**
- Do not show the cancel/execute buttons to a Manager who is not the keeper.
- Do not allow reasons over 200 characters; the handler also validates.
- Do not use `MudTable` row-click as the only navigation; provide an explicit «التفاصيل» action for accessibility.
- Do not add the WhatsApp button here; T15 adds it with its data query.

**Proposed commit:** `feat(inventory): add stock issue order screens`

---

### [ ] T13 — Auto-cancel pending issue orders when an order is cancelled or archived (D18)

**Goal:** Guarantee no reservation survives a cancelled or archived order: pending issue orders are cancelled by the system, reservations released, and a system reason recorded.

**Depends on:** T11 (shared cancellation service), order lifecycle code. **Delivers independently:** cancelling/archiving an order cleans up all pending stock work.

**Locked decisions:** D18, R4, R19.

**Files**
- Modify `OrderHub.Application/Features/Orders/Lifecycle/OrderLifecycleHandlers.cs`
- Reuse `OrderHub.Application/Features/Inventory/IssueOrders/StockIssueOrderCancellationService.cs`
- Create `OrderHub.Tests/OrderLifecycleStockCleanupTests.cs`

**Steps**
1. Inject the shared cancellation service and `IStockIssueOrderRepository` into `OrderLifecycleHandlers`.
2. After a successful `change(order, operation)` in `Execute`:
   - if `order.OrderStatus == OrderStatus.Cancelled` **or** `order.IsArchived`, load pending issue orders for `order.Id`;
   - cancel each using the system path: actor id `null`, name «النظام», reason `"إلغاء تلقائي: أُلغي الطلب أو أُرشف."` (≤200);
   - release every reservation; no movements;
   - keep the order save and the issue-order mutations in the same `SaveChangesAsync` call (the handler already saves only when `Version` changed; ensure the save happens when only stock work changed too).
3. Do not auto-cancel `Completed` issue orders; their stock is already out (phase B reversal handles mistakes).
4. Do not touch `Supply`, shipments or trips; their existing cancellation loops stay as-is.
5. Tests:
   - cancellation from any allowed status cancels pending issue orders, releases reservations, records a Cancelled history with the system reason;
   - archive from `Processing` does the same;
   - archive from a final status handles a lingering pending order (defensive) or no-ops;
   - completed issue orders are untouched;
   - order with no stock work saves normally;
   - transaction/conflict path does not leave a released reservation without an order change.

**Acceptance criteria**
- After cancel/archive, no `Pending` issue order and no reserved quantity remain for the order.
- The history shows a system-generated cancellation with a non-empty reason.
- Completed issue orders and their movements are untouched.
- The existing lifecycle behavior/tests for orders without stock remain green.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~OrderLifecycleStockCleanupTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~OrderReassignment"
```

**Traps**
- Archive is allowed from `Processing`; do not only hook the Cancelled target.
- Do not require the current keeper to perform the auto-cancel; this is a system action (D18).
- Do not add a movement; cancellation releases only.
- Ensure the handler saves when issue orders changed even if `order.Version == expectedVersion` (the existing guard could skip the save).

**Proposed commit:** `feat(inventory): auto-cancel pending issue orders on order cancel or archive`

---

### [ ] T14 — Notifications and the header bell

**Goal:** Add recipient-scoped notifications and a header bell, and produce the two phase-A notifications: issue order assigned to the keeper and issue order completed for the requester.

**Depends on:** T10 (creation points), T11 (execution point), T12 (link target). **Delivers independently:** users see a bell with their unread count and notification panel; producers create real rows.

**Locked decisions:** D13, §5.8, §6.4, §8.7.

**Files**
- Create `OrderHub.Domain/Models/Notification.cs` + `OrderHub.Domain/Enums/NotificationType.cs`
- Create `OrderHub.Infrastructure/Configurations/NotificationConfiguration.cs`
- Modify `OrderHub.Infrastructure/AppDbContext.cs`
- Create `OrderHub.Application/Interfaces/Repositories/INotificationRepository.cs` + `OrderHub.Infrastructure/Reopsitories/NotificationRepository.cs` + registration
- Create `OrderHub.Application/Features/Notifications/GetMyNotifications/*`, `GetUnreadNotificationCount/*`, `MarkNotificationRead/*`
- Modify `OrderHub.Application/Features/Orders/Create/CreateOrderCommandHandler.cs` and `Update/UpdateOrderHandler.cs` (assigned notifications)
- Modify `OrderHub.Application/Features/Inventory/IssueOrders/ExecuteStockIssueOrder/*` (completed notification)
- Modify `OrderHub.Web/Components/Layout/MainLayout.razor` (+ `.razor.cs`)
- Create migration `..._AddNotifications.cs`
- Create `OrderHub.Tests/NotificationTests.cs`, `NotificationProducerTests.cs`

**Steps**
1. Entity: `RecipientUserId`, `Type`, `Title`, `Body`, `LinkUrl`, `OrderId?`, `IssueOrderId?`, `IssuedAtUtc`, `ReadAtUtc?`. No global write access to `RecipientUserId` after creation. `MarkRead(atUtc)` idempotent.
2. Types: `StockIssueAssigned = 1`, `StockIssueCompleted = 2`, `StockIssueCancelled = 3`.
3. EF: table `notifications`; FK recipient `Restrict`; index `(recipient_user_id, read_at_utc, issued_at_utc)`; body/title max lengths; additive migration.
4. Queries/commands (no feature permission; authenticated recipient only):
   - `GetMyNotificationsQuery(page, pageSize)` filters `RecipientUserId == currentUser.Id`;
   - `GetUnreadNotificationCountQuery` same filter with `ReadAtUtc IS NULL`;
   - `MarkNotificationReadCommand(id)` filters by recipient and id; missing/foreign → `"NotFound"` (never reveal others' notifications).
5. Producers:
   - On final order save with stock lines: after `SaveChangesAsync` (ids assigned), add one `StockIssueAssigned` notification per keeper (grouped per warehouse), link `/inventory/issue-orders/{issueOrderId}`, title «أمر إخراج جديد», body with order number, warehouse and line count; save again. In create, the existing transaction covers both saves; in update, wrap the stock-changing save and the notification save in one transaction.
   - On execute: add `StockIssueCompleted` to `RequestedByUserId` (if > 0), link to the order or issue order, title «تم تنفيذ أمر الإخراج», body with warehouse/order number.
   - Never notify a user who is not the recipient; never notify the actor's own generic feed unless they are the requester.
6. Bell in `MainLayout`:
   - plain HTML button with Material bell icon and a counter badge (hidden at 0, capped «99+»);
   - panel with the latest notifications, unread dot, relative time, link; «تعليم الكل كمقروء» optional only if it has a real command (otherwise omit);
   - load count/list on layout init, on `Navigation.LocationChanged`, and on open; mark read when an item is opened;
   - no JS, no fake polling timer, no dead «عرض الكل» link (link to a real `/notifications` page only if created; otherwise the panel itself is the list).
7. Tests: recipient isolation (user A cannot read/mark B), unread count, mark-read idempotency, producer rows/links/recipients, no notification when no stock lines, no notification on supplier-only execution errors.

**Acceptance criteria**
- The bell is visible to every authenticated user and shows only that user's unread count.
- Opening a notification marks it read and navigates to a real page.
- Creating a stock order notifies the current keeper; executing it notifies the requester.
- A foreign notification id returns `"NotFound"` without leaking data.
- No JavaScript, no fabricated data, no dead controls.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~Notification"
```
Manual: two browser sessions (keeper + creator): create a stock order as creator → keeper sees the bell; execute as keeper → creator sees completion.

**Traps**
- Issue-order ids are 0 before the first save; create notifications after saving the order/issue orders.
- Update flow may not have an explicit transaction; add one for the notification-producing path.
- Notification links must point to real routes; `/inventory/issue-orders/{id}` exists from T12.
- Do not add a permission feature for notifications.

**Proposed commit:** `feat(inventory): add recipient notifications and header bell`

---

### [ ] T15 — User phone and WhatsApp keeper message

**Goal:** Store an optional WhatsApp number per user (Higher Manager only) and let an issue-order viewer send/copy a real-data message to the warehouse keeper with the existing fallback behavior.

**Depends on:** T02, T11, T12, T14 (link target optional). **Delivers independently:** the phone field is editable; the issue-order detail has a working WhatsApp action with copy/picker fallback.

**Locked decisions:** D19, D28, §6.5, §8.4.

**Files**
- Modify `OrderHub.Domain/Models/User.cs` (`Phone`, `ChangePhone`)
- Modify `OrderHub.Infrastructure/Configurations/UserConfiguration.cs`
- Create migration `..._AddUserPhone.cs`
- Modify `OrderHub.Application/Features/Administration/Users/Update/*` (command/contract/handler/reader) and `GetUser/*`
- Modify `OrderHub.Web/Components/Pages/Features/Administration/Users/Edit.razor` (+ `.razor.cs`)
- Create `OrderHub.Application/Features/Inventory/IssueOrders/GetKeeperContact/GetStockIssueOrderKeeperContactQuery.cs` + handler + contract
- Create `OrderHub.Application/Features/Inventory/IssueOrders/Contracts/StockIssueWhatsAppMessageBuilder.cs`
- Create `OrderHub.Web/Components/Pages/Features/Inventory/IssueOrders/IssueOrderWhatsAppDialog.razor`
- Modify `IssueOrders/Details.razor` (+ code-behind) to add the action
- Create `OrderHub.Tests/UserPhoneTests.cs`, `StockIssueWhatsAppMessageBuilderTests.cs`, `KeeperContactQueryTests.cs`

**Steps**
1. `User.Phone`:
   - nullable string, max 30; `ChangePhone(string? phone)` trims, empty clears, rejects invalid characters/length (`Result` or exception consistent with the entity style);
   - no phone in `User.Create` required; existing users stay null.
2. EF: nullable `phone` varchar(30); additive migration.
3. Users edit (Higher-Manager-only page):
   - field «رقم واتساب» with helper «اختياري — يُستخدم لفتح محادثة واتساب مع أمين المخزن»;
   - inline validation; save through the existing update command; no other role can edit (page already `[HigherManagerAuthorize]`).
4. Keeper contact query:
   - `[RequirePermission(Inventory, Read)]`;
   - input issue-order id; resolves the **current** warehouse keeper (not `AssignedUserId`), returns `DisplayName`, `Phone` (raw), `WarehouseName`;
   - never returns username or role.
5. Message builder (pure, testable):
   - facts only: order number, warehouse name, issue number, product names and requested quantities;
   - Arabic heading like «أمر إخراج جديد»; no prices, no unbacked claims, no links;
   - reuse `WhatsAppMarkdown` conventions if helpful; the preview renders typed segments, never `MarkupString`.
6. Dialog:
   - opened from issue-order detail action «واتساب أمين المخزن»;
   - primary `wa.me/{normalized}?text=...` only when `WhatsAppPhone.TryNormalize` succeeds; otherwise disabled with an Arabic hint;
   - always-enabled copy action (reuse the `OrderSummaryDialog` clipboard interop) and `wa.me/?text=...` recipient picker;
   - no phone → hint «لم يُسجَّل رقم واتساب لأمين المخزن؛ انسخ الرسالة أو اختر المستلم.»
7. Tests: phone validation/clear/round-trip; update command persists phone; contact query returns current keeper after a transfer; message builder exact content (no invented data); normalization fallback flags.

**Acceptance criteria**
- Only a Higher Manager can set/clear a user's phone.
- WhatsApp action works with a valid phone; with an invalid/missing phone, copy and picker remain available and no broken link is shown.
- The message contains only real order/warehouse/line data.
- The query resolves the current keeper, not the request-time snapshot.
- No new JavaScript; clipboard uses the existing interop pattern.

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~UserPhoneTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~StockIssueWhatsAppMessageBuilderTests"
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~KeeperContactQueryTests"
```
Manual: set a phone as HM → open a pending issue order → verify direct link, copy, and picker; clear the phone → verify fallback.

**Traps**
- Do not allow Manager or the keeper to edit phones; D28 is Higher-Manager-only.
- Do not trust the stored number; always normalize.
- Do not expose the phone on issue-order list/detail read models; fetch it only through the contact query when the action is used.
- Do not add a second normalizer; reuse `WhatsAppPhone`/`WhatsAppMessaging`.

**Proposed commit:** `feat(inventory): add keeper WhatsApp contact and user phone`

---

### [ ] T16 — Phase A acceptance sweep

**Goal:** Prove phase A is complete, documented and regression-free before starting phase B.

**Depends on:** T01–T15. **Delivers independently:** a green phase-A baseline and accurate docs.

**Files**
- Modify `plans/INVENTORY-MANAGEMENT-SPEC.md` (status line: phase A implemented; keep decisions intact)
- Modify this file (checkboxes/status)
- Add/adjust tests anywhere gaps were found (no production behavior changes)

**Steps**
1. Run the full suite and the Node test:
   ```powershell
   dotnet build OrderHub.sln --configuration Release
   dotnet test OrderHub.sln --configuration Release --no-build
   node OrderHub.Tests/Web/order-local-time.test.mjs
   ```
2. Manual acceptance matrix (record results in this file under T16):
   - Sales: sees `/warehouses` and `/inventory`; cannot stock-in, execute/cancel, or open issue-order actions; supplier-only fallback works if `Inventory.Read` is revoked.
   - Purchasing: stock-in works; cannot execute/cancel issue orders unless also the keeper.
   - Storekeeper: sees warehouses/inventory/issue orders; executes/cancels own warehouse; cannot execute another warehouse's order; cannot stock-in without `Products.Read` (confirm preset has it).
   - Manager: creates/edits warehouses, assigns keepers, sees all inventory, can approve reversals (phase B), **cannot** execute a non-owned issue order.
   - Higher Manager: fallback execution/cancel; phone editing; all pages.
   - Data Entry: inventory group hidden.
3. Browser checks: create order with split/stock-only/supplier-only items; reservation; lock; cancel issue order; item unlock; execute; balance/ledger; notifications; WhatsApp; order cancel/archive auto-cancel; mobile RTL.
4. Migration rehearsal on a populated copy (owner-run or documented as a deployment step): apply new migrations in order, confirm no data changes to existing tables.
5. Update the spec status and list any deviations; do not rewrite locked decisions.

**Acceptance criteria**
- Full build/test/Node commands pass.
- Every manual matrix row behaves as specified; failures are fixed in the owning task, not papered over.
- Spec status and task checkboxes reflect reality.
- No pending `[ ]` in T01–T15.

**Verify:** the commands above plus the completed manual matrix.

**Proposed commit:** `test(inventory): complete phase A acceptance sweep`

---

## 4. Phase B — tasks

### [ ] T17 — Customer returns (intact/damaged)

**Goal:** Record customer returns against the original order, with intact units rejoining stock and damaged units recorded without a balance effect.

**Depends on:** T05, T06, T10, T11, T16. **Delivers independently:** returns can be recorded and audited; stock effects are correct.

**Locked decisions:** D11, D24, §5.7, §8.5.

**Files**
- Create `OrderHub.Domain/Models/CustomerReturn.cs`, `CustomerReturnItem.cs`
- Create `OrderHub.Infrastructure/Configurations/CustomerReturnConfiguration.cs` (+ item)
- Modify `OrderHub.Infrastructure/AppDbContext.cs`
- Create `OrderHub.Application/Features/Inventory/Returns/RecordCustomerReturn/*` (query returnable items + command + handler + contracts)
- Create `OrderHub.Application/Interfaces/Repositories/ICustomerReturnRepository.cs` + implementation + registration
- Create `OrderHub.Web/Components/Pages/Features/Inventory/Returns/Index.razor` (+ code-behind) and `RecordReturnDialog.razor`
- Modify `OrderHub.Web/Components/Pages/Features/Orders/OrderDetials.razor` (real action opening the return dialog for eligible stock items)
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor`
- Create migration `..._AddCustomerReturns.cs`
- Create `OrderHub.Tests/CustomerReturnTests.cs`

**Steps**
1. Domain: header `OrderId`, `WarehouseId`, `RecordedByUserId`, `RecordedAtUtc`, note; lines `OrderItemId`/`ProductId`, `Quantity`, `Condition` (`Intact`/`Damaged`), `OrderIssueOrderId?`. Validate quantity > 0, no duplicate product with the same condition (or aggregate), and no over-return.
2. Eligibility query: original order's stock-sourced items and executed quantities minus previous returns (intact + damaged), grouped by product/warehouse; only completed issue-order lines are returnable.
3. Command `[RequirePermission(Inventory, Write)]`:
   - current keeper or Higher Manager (D24) binding;
   - order exists; warehouse matches the issue order;
   - quantities ≤ remaining executed; reject `"InvalidOperation"` otherwise;
   - transaction: add return + lines; for intact lines add `CustomerReturnIn` movements and increase stock; damaged lines record only;
   - no negative/zero lines; no movements for damaged.
4. Unknown/mismatched order/product/warehouse → `"InvalidOperation"`/`"NotFound"`.
5. UI:
   - `/inventory/returns` list with order number, warehouse, keeper, date, intact/damaged totals, status none (recorded only);
   - dialog from the order page and the returns page (if it can select an order, otherwise order-page only); lines with condition toggle and remaining quantity; one primary CTA;
   - nav item «المرتجعات» (`Inventory.Read`), page gate Read; action gate Write + binding.
6. Tests: intact increases balance and adds movement; damaged records without balance effect; over-return rejected; non-keeper rejected; Higher Manager allowed; atomicity; eligibility math after partial returns; no return for supplier-only/unexecuted items.

**Acceptance criteria**
- Intact units rejoin the same warehouse balance; damaged units are visible but do not change it.
- Only the current keeper or a Higher Manager can record a return.
- Quantities never exceed what was issued and not yet returned.
- Returns are immutable; mistakes go through reversal (T18).

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~CustomerReturnTests"
```

**Traps**
- Do not create a return for an issue order that is still Pending.
- Do not use the order item quantity; use the executed issue-order quantity as the ceiling.
- Keep damaged lines in the return record even though they produce no movement.
- Add the nav item only after the page exists.

**Proposed commit:** `feat(inventory): record customer returns with intact and damaged conditions`

---

### [ ] T18 — Reversal requests with `Inventory.Approve` approval

**Goal:** Fix executed stock mistakes with a request/approval workflow: keeper or Higher Manager requests with a reason; a different user with `Inventory = Approve` decides; approval writes the paired reversal movement and flags the original.

**Depends on:** T05, T06, T11, T17, T16. **Delivers independently:** reversals are requested, approved/rejected and reflected in the ledger and balances.

**Locked decisions:** D9, D10, D20, R11, R20, §5.6, §6.3, §8.6.

**Files**
- Create `OrderHub.Domain/Models/InventoryReversalRequest.cs` + `OrderHub.Domain/Enums/ReversalRequestStatus.cs`
- Create `OrderHub.Infrastructure/Configurations/InventoryReversalRequestConfiguration.cs`
- Modify `OrderHub.Infrastructure/AppDbContext.cs`
- Create `OrderHub.Application/Features/Inventory/Reversals/RequestReversal/*`, `ApproveReversal/*`, `RejectReversal/*`, `GetReversals/*`
- Create `OrderHub.Application/Interfaces/Repositories/IReversalRepository.cs` + implementation + registration
- Create `OrderHub.Web/Components/Pages/Features/Inventory/Reversals/Index.razor` (+ code-behind), `RequestReversalDialog.razor`, `DecisionDialog.razor`
- Modify `OrderHub.Web/Components/Pages/Features/Inventory/Index.razor` (ledger row action «طلب عكس»)
- Modify `OrderHub.Web/Components/Layout/NavMenu.razor`
- Create migration `..._AddInventoryReversals.cs`
- Create `OrderHub.Tests/InventoryReversalTests.cs`

**Steps**
1. Entity: `MovementId`, `Reason` (required, ≤200), `RequestedByUserId`, `RequestedAtUtc`, `Status` (`PendingApproval`), `DecidedByUserId?`, `DecidedAtUtc?`, `DecisionNote?`. One pending request per movement (filtered unique index).
2. Request command `[RequirePermission(Inventory, Write)]`:
   - movement exists and is Active; not already reversed and no pending request;
   - current keeper of the movement's warehouse or Higher Manager (keeper binding); `Inventory.Approve` alone is not enough to request;
   - reason required; no ledger change at request time.
3. Approve command `[RequirePermission(Inventory, Approve)]`:
   - request Pending; **approver id != requester id** (D9), including Higher Managers (no bypass);
   - transaction: add paired movement (opposite direction, `MovementType = Reversal`, same product/warehouse/quantity, `ReversalOfMovementId`, `Source*` copied, note = request reason, actor = approver), mark original `Reversed`, update `StockItem` (in↔out) without going negative (if the original was `StockIn`, reverse out must not exceed current stock; reject `"Conflict"` if it would);
   - set request Approved with decider/time; no deletion;
   - update the issue order/return derived state only where phase-A/B rules require (completed issue order stays completed; the reversal is the audit fix).
4. Reject command: same `Approve` gate, Pending only, no ledger change; record decider/time/note (optional).
5. UI `/inventory/reversals`:
   - request list + pending approval queue; tabs/filters;
   - request action from the ledger row (visible for keeper/HM and `Inventory.Write`);
   - approve/reject actions visible only with `Approve`, disabled for the requester's own request with an Arabic hint;
   - one primary CTA per context;
   - nav item «العكس» (`Inventory.Approve`), page gate `Approve`; the request dialog from `/inventory` is separately gated by Write + binding.
6. Tests: request requires binding; approve requires Approve; self-approval rejected (including HM); double approval/rejection rejected; approval creates exactly one reversal and flags the original; reverse of a `StockIn` blocked when stock insufficient; rejection leaves ledger/balance untouched; one pending request per movement; approver can be a Manager who is not the keeper; page/handler gates.

**Acceptance criteria**
- No movement is deleted or edited; reversals are paired and auditable.
- Separation of duties always holds, including for Higher Managers.
- Ledger and balance stay consistent in one transaction; no negative stock.
- Only `Inventory = Approve` users can decide; only bound writers can request.
- The reversal page is not under an Admin-only page claim (D27).

**Verify**
```powershell
dotnet test OrderHub.sln --configuration Release --no-build --filter "FullyQualifiedName~InventoryReversalTests"
```

**Traps**
- `Inventory.Write` is satisfied by `Approve`; the request path still needs keeper binding, and the decision path needs `Approve` explicitly.
- Do not allow approving a request for a movement that was reversed by another path in the meantime.
- Do not auto-approve or bypass separation for HM.
- The reversal note is mandatory and comes from the request reason.

**Proposed commit:** `feat(inventory): add reversal requests with approval workflow`

---

### [ ] T19 — Balance rebuild, aggregate reporting refinements, Phase B acceptance

**Goal:** Provide the Higher-Manager-only rebuild-from-ledger tool, finish cross-warehouse reporting, and close phase B with tests and docs.

**Depends on:** T17, T18. **Delivers independently:** drift can be repaired safely; totals are complete; phase B is signed off.

**Locked decisions:** §5.3, §7.4 (rebuild gate), §12 B3, R6.

**Files**
- Create `OrderHub.Application/Features/Inventory/RebuildBalances/RebuildStockBalancesCommand.cs` + handler + `RebuildStockBalancesResult.cs`
- Create `OrderHub.Web/Components/Pages/Features/Inventory/Rebuild.razor` (+ code-behind)
- Modify `OrderHub.Web/Components/Pages/Features/Inventory/Index.razor` (link/entry for HM only, totals refinements)
- Modify `OrderHub.Application/Features/Inventory/GetBalances/*` (cross-warehouse product totals if not complete)
- Modify `plans/INVENTORY-MANAGEMENT-SPEC.md` and this file
- Create `OrderHub.Tests/RebuildStockBalancesTests.cs`, `InventoryReportingTests.cs`

**Steps**
1. Rebuild command:
   - `[RequireHigherManager]` (Higher Manager only; do not rely on an Admin page claim alone);
   - recompute `StockOnHand` for every stock item from `SUM(active movements by direction)`; recompute `ReservedOnHand` from pending issue-order lines;
   - create missing stock rows with zero when a warehouse/product has movements or pending lines;
   - one transaction; return counts (scanned, changed, missing-created) and a before/after diff sample;
   - no movement rows are created by a rebuild.
2. UI `/inventory/rebuild`:
   - Higher-Manager-only page; explanation of what changes; «معاينة» then confirm; busy states; result summary;
   - link from `/inventory` visible only to HM.
3. Reporting refinements:
   - `/inventory` aggregate table includes total stock/reserved/available per product across all warehouses and the number of warehouses holding it;
   - ledger drill-down remains per warehouse/product;
   - no monetary valuation (out of scope).
4. Phase B acceptance:
   - full suite + Node test;
   - manual matrix for returns and reversals (keeper/HM/Manager/approver separation);
   - migration rehearsal note;
   - update spec status to implemented and this file's checkboxes.
5. Tests: rebuild repairs a deliberately drifted cached balance; reserved recomputed from pending lines; idempotent second run reports zero changes; non-HM denied; no movements created; reporting totals correct.

**Acceptance criteria**
- Rebuild is Higher-Manager-only, transactional, idempotent and never deletes movements.
- After rebuild, cached balances equal the active ledger sums and reservations equal pending lines.
- The aggregate view is correct across warehouses.
- Phase B manual matrix and docs are complete; no unchecked task remains.

**Verify**
```powershell
dotnet build OrderHub.sln --configuration Release
dotnet test OrderHub.sln --configuration Release --no-build
node OrderHub.Tests/Web/order-local-time.test.mjs
```

**Traps**
- Do not use rebuild to hide bugs; it is a repair tool.
- Do not let it create movements or notifications.
- Do not expose it to Managers; `Inventory.Approve` is not enough.
- Do not recompute `ReservedOnHand` from the ledger; reservations are not movements.

**Proposed commit:** `feat(inventory): rebuild cached balances and finalize inventory reporting`

---

## 5. Task status board

| Task | Phase | Status | Blocked by |
|---|---|---|---|
| T01 Permissions + seed + candidates | A | [ ] | — |
| T02 Warehouse domain | A | [ ] | T01 |
| T03 Warehouses application | A | [ ] | T01, T02 |
| T04 Warehouses UI + nav | A | [ ] | T01, T03 |
| T05 Ledger + StockItem | A | [ ] | T02 |
| T06 Stock-in application | A | [ ] | T05 |
| T07 Inventory read queries | A | [ ] | T05 |
| T08 Inventory + stock-in UI | A | [ ] | T03, T04, T06, T07 |
| T09 Issue-order domain | A | [ ] | T02, T05 |
| T10 Order sourcing + locks | A | [ ] | T05–T07, T09 |
| T11 Execute/cancel handlers | A | [ ] | T05, T09, T10 |
| T12 Issue-order UI | A | [ ] | T11 |
| T13 D18 auto-cancel | A | [ ] | T11 |
| T14 Notifications + bell | A | [ ] | T10, T11, T12 |
| T15 Phone + WhatsApp | A | [ ] | T02, T11, T12 |
| T16 Phase A acceptance | A | [ ] | T01–T15 |
| T17 Returns | B | [ ] | T05, T06, T10, T11, T16 |
| T18 Reversals + approval | B | [ ] | T05, T06, T11, T17, T16 |
| T19 Rebuild + reporting + acceptance | B | [ ] | T17, T18 |

---

## 6. Acceptance/sign-off

- [ ] Phase A: T01–T16 checked, full suite + Node test green, manual matrix recorded in T16.
- [ ] Phase B: T17–T19 checked, full suite + Node test green, manual matrix recorded in T19.
- [ ] Spec status updated; no locked decision changed without an owner-approved amendment.
- [ ] Deployment note added for the migrations (V2 seed, warehouses, ledger, purchases, issue orders, notifications, user phone, returns, reversals).

**Proposed final commit (after the owner's review):** `feat(inventory): complete warehouse stock management`
