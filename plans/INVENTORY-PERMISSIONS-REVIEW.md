# Inventory × Roles & Permissions — Integration Review

- **Version:** 1.2 (review complete — all decisions locked; spec v1.3 written)
- **Date:** 2026-11-09 · revision 1.2 locks the six default decisions
- **Inputs:** `plans/INVENTORY-MANAGEMENT-SPEC.md` v1.2 · `plans/roles-and-permissions-plan.md` · `plans/roles-and-permissions-study.md` · `AGENTS.md`
- **Scope:** verify the inventory spec's permission assumptions against the planned roles redesign and the code as it exists today; produce the permission model and the build-order dependency. **Implementation is explicitly out of scope.**

---

## 0. Verdict

**Revision 1.1:** the roles redesign is now implemented (Approve level, 4 sealed presets, Higher Manager gates). The earlier silent-deny item is resolved; the remaining blockers are inventory-specific.

The stock ledger, reservation, issue-order and reversal mechanics are internally sound. The permission layer still needs the amendments in §5:

- `Approve` satisfies `Write`, so a feature gate alone **over-privileges** execution unless the keeper-binding check runs in the handlers.
- The sealed presets have **no rows** for the two new features → every preset role starts with zero inventory access.
- There is **no keeper/storekeeper role**; keepers would need custom roles or the over-privileged Manager preset.
- Keeper assignment needs a user directory, and `GetUsersQuery` is gated by `Users.Read`, which Manager does not have.

All ten decisions (D-RP1–D-RP10) are now locked, and `plans/INVENTORY-MANAGEMENT-SPEC.md` **v1.3** carries them. Inventory A1 is ready to start; the roles prerequisite (R0) is complete.

### 0.1 Post-implementation delta (verified 2026-11-09)

| Item | Status | Evidence |
|---|---|---|
| `Approve` level | **Implemented** | `PermissionLevel.cs:16` |
| Approve guard-line + hierarchy | **Implemented** (Read←R/W/A, Write←W/A, Approve←A) | `AuthorizationService.cs:16` |
| Policies for Approve | **Implemented** (loops all non-None levels) | Web authorization registration |
| Sealed roles + `RoleKey` | **Implemented** — 4 presets, 31 cells | `Role.cs:23`, `20261108214724_SeedSealedRoles` |
| Higher Manager gate | **Implemented** | `HigherManagerAuthorizeAttribute`, `IsHigherManagerAsync` |
| Inventory features | **Not present** — enum still 14 | `PermissionFeature.cs` |
| Storekeeper preset | **Not present** | `SealedRolePresets.cs` |
| `User.Phone` | **Not present** | `User.cs` |
| Inventory code | **Not present** | repo-wide grep |

Consequence: **F1 is resolved; F2–F16 remain.** The seed is now frozen as `SealedRoleSeedSql.V1` with tests asserting 4 roles / 31 cells — inventory must add a **V2** seed (new migration, new SQL constant) and update `SealedRolePresets`, never edit V1.

---

## 1. Verified current state (2026-11-08)

| Claim in the plans | Reality in the code | Evidence |
|---|---|---|
| `Approve` level exists | **Implemented** (`None / Read / Write / Approve`) | `OrderHub.Domain/Enums/PermissionLevel.cs:16` |
| `Approve` can be checked | **Implemented** — guard and hierarchy include Approve | `AuthorizationService.cs:16` |
| Policies exist for `Approve` | **Implemented** — loops all non-None levels | `ServiceCollectionExtensions.cs:18` |
| Sealed preset roles exist | **Implemented** — 4 presets, 31 permission cells | `Role.cs:23`, `20261108214724_SeedSealedRoles` |
| Higher Manager gate exists | **Implemented** | `HigherManagerAuthorizeAttribute`, `IsHigherManagerAsync` |
| A keeper/storekeeper role exists | **No such preset.** Only the 4 planned presets | `plans/roles-and-permissions-plan.md:20` |
| `PermissionFeature` has inventory entries | 14 features, none for stock | `OrderHub.Domain/Enums/PermissionFeature.cs:5` |
| Adding a feature is UI-complete | Editor enumerates the enum automatically, but names/descriptions/icons/tones are a hard-coded switch | `RoleEditorState.cs:11`, `RoleFeatureCatalog.cs:8` |
| Permission checks can read a user directory | User listing requires `Users.Read`; Manager gets `Users = None` | `GetUsersQuery.cs:9`, roles plan decision 8 |
| Row-level authz is `PermissionAccess` | `PermissionAccess` is **feature-level only**; it cannot prove *which warehouse* a keeper owns | `OrderHub.Web/Services/PermissionAccess.cs:9` |
| `User` has a phone | **No `Phone` field** | `OrderHub.Domain/Models/User.cs:7` |
| Inventory code exists | **No** `Warehouse`, `StockItem`, `InventoryMovement`, `StockIssueOrder` anywhere | repo-wide grep |
| Existing order items support a null supplier | Confirmed (`SupplierId` nullable) | `OrderHub.Domain/Models/OrderItem.cs:53` |
| RolePermission levels are stored as text | Confirmed (Feature + Level string conversions; `None` check constraint) | `RolePermissionConfiguration.cs` |
| Order lifecycle supports cancellation | Confirmed (`WorkRecordStatus.Pending/Completed/Cancelled/Failed`) | `OrderHub.Domain/Enums/WorkRecordStatus.cs:5` |

---

## 2. Dependency map — what inventory consumes from R&P

| R&P deliverable | Inventory use | Blocks |
|---|---|---|
| `PermissionLevel.Approve` + guard-line fix + policy factory | Reversal approval; manager preset authority | Phase B2 |
| `Role.IsSealed` / `RoleKey` + preset seed | New feature rows must be seeded into sealed presets; presets cannot be edited in UI | A1 (preset access) |
| Preset drift tests | Must be extended with the two new features or they will fail by design | A1 |
| Roles editor matrix (4 levels) | New cards must render as 4-segment, not 3 | A1 |
| Manager = no system administration (decision 8) | Warehouse keeper assignment cannot use the Users page/query | A1 |
| Higher Manager = `IsAdmin` | Admin fallback for execution/cancel and for the rebuild tool | A4 / B3 |
| Users page (Higher Manager only) | `User.Phone` is set here; affects WhatsApp keeper messaging | A5 |
| Nav permission filtering | New inventory group must be hidden for roles without access | A1 |

---

## 3. Findings (triaged)

Severity: **BLOCKER** = fails at implementation as written · **SECURITY** = wrong access outcome · **DECISION** = owner choice required · **DOC-GAP** = no runtime failure, spec incomplete.

| # | Sev | Finding | Consequence | Required amendment |
|---|---|---|---|---|
| F1 | RESOLVED | Spec D9/§7 say "admin" approves reversals; roles plan introduces `Approve` for exactly this | Roles now implements Approve end-to-end (verified 2026-11-09) | Still gate approval with `Inventory + Approve`; Higher Manager passes via bypass |
| F2 | SECURITY | D20 (keeper-only execution) is written as a feature permission, but `Approve` ≥ `Write` | A Manager (Approve) would pass the `Inventory.Write` gate and could execute/cancel any warehouse's issue order | Feature gate is only the outer gate; add a mandatory handler-level **keeper-binding** check (`IsAdmin || warehouse.KeeperUserId == user.Id`) |
| F3 | BLOCKER | No keeper/storekeeper role exists, and §7 says "keeper + admins" | Keepers must either get `Manager` (over-privileged, includes system-ish control) or a hand-made custom role per person — reopening the pain the redesign removes | Add a 5th sealed preset `storekeeper` («أمين مخزن») in the inventory migration — **LOCKED 2026-11-09** |
| F4 | BLOCKER | Sealed presets would have **no rows** for `Warehouses`/`Inventory` | Sales cannot see availability; purchasing cannot record stock-in; manager cannot approve; every preset behaves as `None` | Add a new migration inserting the two feature rows per preset (idempotent, `role_key`-keyed) and update the drift test |
| F5 | BLOCKER | Keeper assignment needs a user list; `GetUsersQuery` requires `Users.Read`; Manager has `Users = None` | Manager **will** manage warehouses (locked), so the keeper dropdown would be denied/dead | Add a minimal `GetWarehouseKeeperCandidatesQuery` gated by `Warehouses.Write` returning `Id, DisplayName, IsActive, HasInventoryWrite` only — **LOCKED 2026-11-09** |
| F6 | DECISION | Roles plan sets Manager = `Approve` on **all** operational features; Warehouse CRUD has no approval flow | Semantically odd, and makes "Manager can create warehouses" implicit | **LOCKED:** Manager `Warehouses=Write`, `Inventory=Approve` (2026-11-09) |
| F7 | BLOCKER | Reversal approval listed among admin pages would inherit `[Authorize(Roles = "Admin")]` | Manager (Approve) would be locked out of the page entirely | Host reversal approval under `/inventory/reversals`, **not** under `/admin` |
| F8 | SECURITY | No separation-of-duties rule for reversals | A keeper who is also a Manager/approver can approve their own reversal | Enforce `ApproverUserId != RequestedByUserId` in the handler; add test |
| F9 | DECISION | D20 binds execution to the keeper, but issue orders snapshot `AssignedUserId` | After a keeper transfer, pending orders are stuck with the old user; the new keeper cannot act | Evaluate against the **current** `Warehouse.KeeperUserId` at action time; keep `AssignedUserId` as request-time audit only |
| F10 | DECISION | Order editor availability requires `Inventory.Read`; a custom role may have `Orders.Write` without it | Missing data path: item sourcing would either leak stock numbers or dead-end | If `Inventory.Read` is absent: hide availability, disable stock sourcing, show an Arabic hint; supplier-only path stays; reservation remains part of `Orders.Write` |
| F11 | DECISION | Stock-in needs product names/suppliers, but `Inventory.Write` is independent of `Products.Read` | A custom role with only `Inventory.Write` gets a dead stock-in screen | Require `Products.Read` for the product picker; document it and include it in the `storekeeper` preset |
| F12 | DOC-GAP | `User.Phone` is editable only on the Higher-Manager-only Users page | Manager cannot set the keeper's phone, so direct `wa.me` may be unavailable | Keep copy + recipient-picker fallback; document that the phone is a Higher-Manager setup step (or open a scoped decision) |
| F13 | DOC-GAP | New enum members render with `feature.ToString()` + generic icon until the catalog is updated | English "Warehouses"/"Inventory" cards in the permissions matrix | Add Arabic name/description/icon/tone entries in the same slice |
| F14 | DOC-GAP | Roles drift test asserts the exact preset matrix | It will fail intentionally once the enum grows | Update the expected matrix as part of the inventory migration slice |
| F15 | DOC-GAP | §3 says "row-level authz via `PermissionAccess.IsAllowedAsync`" | `PermissionAccess` cannot prove warehouse ownership | Clarify: feature-level via `PermissionAccess`; warehouse-binding check in Application handlers returning `Result.Failure` |
| F16 | DECISION | Returns (phase B) have no actor rule | Unclear whether sales or the keeper records a return | Recommend `Inventory.Write` + keeper binding (stock physically re-enters that warehouse) |

---

## 4. Proposed inventory permission model

### 4.0 Locked owner decisions (2026-11-09)

| # | Decision | Locked answer |
|---|---|---|
| D-RP1 | Keeper access | **Add a 5th sealed preset** `storekeeper` («أمين مخزن») |
| D-RP2 | Manager & warehouses | **Manager `Warehouses=Write` + `Inventory=Approve`**; minimal `Warehouses.Write`-gated keeper-candidate query (no `Users` access) |
| D-RP3 | Reversal approval | **`Inventory=Approve`** (Manager + Higher Manager); requester ≠ approver |
| D-RP4 | Manager execution | **Keeper/admin only** — a Manager with `Approve` is rejected unless he is that warehouse's current keeper |

These four supersede the corresponding recommendations in §8. `D-RP5`–`D-RP10` remain proposed defaults.

### 4.1 Features

Two additions to `PermissionFeature` (append after `PaymentMethods`, before the Administration block):

| Enum | Arabic (Description) |
|---|---|
| `Warehouses` | المخازن |
| `Inventory` | المخزون |

Automatic consequences (no extra work): policy registration loops the enum; the role editor enumerates the enum; `Enum.IsDefined` validation passes.

Manual consequences: `RoleFeatureCatalog` name/description/icon/tone; `NavMenu` group; preset seed rows; drift test.

### 4.2 Level semantics applied to inventory

Per the roles plan: `Read` ← Read|Write|Approve · `Write` ← Write|Approve · `Approve` ← Approve only.

**Important:** because `Approve` satisfies `Write`, a feature gate alone is never sufficient for execution authority. The keeper-binding rule in §4.4 is mandatory.

### 4.3 Preset matrix delta

Only the two new columns; all other cells follow `roles-and-permissions-plan.md`.

| Role | Warehouses | Inventory | Rationale |
|---|---|---|---|
| مبيعات `sales` | Read | Read | Sees per-warehouse availability while sourcing order items |
| مشتريات `purchasing` | Read | Write | Records stock-in purchases |
| إدخال البيانات `data-entry` | — | — | No stock responsibility |
| مدير `manager` | **Write** ✅ | **Approve** ✅ | Operational warehouse control; reversal approval |
| أمين مخزن `storekeeper` **(new, sealed)** ✅ | Read | Write | Executes/cancels issue orders, records stock-in/returns for his warehouse |
| مدير أعلى `IsAdmin` | bypass | bypass | Unchanged |
| Existing custom roles | — | — | Additive default `None`; owner grants manually |

`storekeeper` locked preset: `Warehouses=Read`, `Inventory=Write`, `Products=Read`, `Orders=Read`, everything else `None`. (Orders read so the issue-order page can link to the originating order; Products read for the stock-in picker.)

### 4.4 Action → gate matrix

| Action | Feature gate | Row / data gate |
|---|---|---|
| Warehouses list & details | `Warehouses.Read` | — |
| Warehouse create / edit / deactivate | `Warehouses.Write` | Deletion/deactivation guards (stock or pending issue orders) |
| Keeper candidate list | `Warehouses.Write` | Minimal fields only |
| Inventory balances / ledger | `Inventory.Read` | — |
| Record stock-in | `Inventory.Write` | Product picker needs `Products.Read` |
| Issue orders list / details | `Inventory.Read` | — |
| Execute / cancel issue order | `Inventory.Write` | **`IsAdmin` OR current warehouse keeper** (not the snapshot) |
| Request reversal | `Inventory.Write` | **`IsAdmin` OR current warehouse keeper** |
| Approve / reject reversal | `Inventory.Approve` | **Approver ≠ requester** |
| Record customer return (B) | `Inventory.Write` | **`IsAdmin` OR current warehouse keeper** |
| Rebuild balances from ledger (B3) | `[Authorize(Roles = "Admin")]` | Higher Manager only (data-repair tool) |
| Item sourcing in order editor | `Orders.Write` to save; `Inventory.Read` for availability | Live availability + reservation; conflict on race |
| Notification bell | authenticated | Recipient filter only |

### 4.5 Keeper-binding rule (code-level, never a DB constraint)

```
allowed = currentUser.IsAdmin
       || warehouse.KeeperUserId == currentUser.Id
```

- Evaluated **inside the Application handlers**, returning `Result.Failure("...", "Forbidden")` — not thrown, not DB-enforced.
- Applies to execute, cancel, request reversal (phase B), record return (phase B).
- Uses the **current** keeper, so a keeper transfer immediately moves pending-work authority.
- Admin override is audited (`ExecutedByUserId` / `CancelledByUserId`), per D20.
- The Manager's `Approve` level does **not** bypass this rule.

---

## 5. Required amendments to `INVENTORY-MANAGEMENT-SPEC.md` (proposed v1.3)

1. **D9** — replace "admin approval" with `Inventory + Approve`; Higher Manager passes by bypass; add "approver must not be the requester".
2. **D20** — keep keeper-or-Higher-Manager; explicitly state that `Approve` does **not** grant execution; state that authority follows the **current** keeper, not the issue-order snapshot; add the same rule to cancellation.
3. **§5.1 / §5.5** — document the keeper-binding check and the current-keeper semantics; keep `AssignedUserId` as audit only.
4. **§7** — replace the feature table with §4.4 above; remove the words "admins" and "keeper" as if they were roles.
5. **§8** — place reversal approval under `/inventory/reversals`; never under an `[Authorize(Roles = "Admin")]` page.
6. **§3** — correct "row-level authz via `PermissionAccess`" to distinguish feature-level checks from warehouse-binding checks.
7. **§12** — insert **R0: roles prerequisite** before A1 (see §6); move the preset/catalog/nav wiring into A1.
8. **New D22 (LOCKED 2026-11-09)** — sealed `storekeeper` preset: `Warehouses=Read`, `Inventory=Write`, `Products=Read`, `Orders=Read`.
9. **New D23 (LOCKED 2026-11-09)** — Manager `Warehouses=Write`, `Inventory=Approve`; minimal `Warehouses.Write`-gated keeper-candidate query.
10. **New D24 (LOCKED default 2026-11-09)** — returns actor: `Inventory.Write` + keeper binding.
11. **§13** — add risks R17–R22 from §7 below.
12. **§10** — note that `Products.Read` is a documented prerequisite for stock-in, and `Orders.Read` for the `storekeeper` preset.

---

## 6. Revised build order

```
R0 — Roles & Permissions redesign  ✅ DONE (2026-11-09)
     Domain: Approve, IsSealed, RoleKey
     Application: guard-line + Approve hierarchy, policy factory, command guards
     Infrastructure: 20261108214724_SeedSealedRoles (4 presets, 31 cells)
     Web: 4-segment matrix, sealed read-only, Higher Manager gates
     Tests: authorization matrix, sealed guards, seed drift
            │
            ▼
Inventory Phase A
     A1  Warehouses
         + PermissionFeature.Warehouses/Inventory
         + RoleFeatureCatalog entries
         + NavMenu inventory group
         + migration: preset permission rows (+ storekeeper preset if approved)
         + GetWarehouseKeeperCandidatesQuery
         + drift-test update
     A2  Ledger + StockItem + stock-in
     A3  Order item sourcing + locking
     A4  StockIssueOrder + keeper-binding handler guard
     A5  Notification + bell + WhatsApp keeper message
     A6  Tests
            │
            ▼
Inventory Phase B
     B1  Returns (Inventory.Write + keeper binding)
     B2  Reversal request + approval (requires Approve)
     B3  Rebuild tool (Higher Manager) + aggregate reporting
```

**Hard sequencing rule:** the roles seed now exists as `20261108214724_SeedSealedRoles` with **frozen** `SealedRoleSeedSql.V1` (tests assert 4 roles / 31 cells). Timestamp the inventory migration **after** it, add a new `V2` SQL constant + migration, extend `SealedRolePresets` (Domain), and update the seed tests' counts. Make every insert idempotent (`WHERE NOT EXISTS` by `role_key` + feature). **Do not edit V1.**

**Parallelism:** roles Phase 4 (users pages) and inventory A1 (warehouses) touch different files; they can run concurrently only after roles Phase 3.

---

## 7. Risks added by this integration (proposed R17–R22)

| # | Risk | Class | Mitigation |
|---|---|---|---|
| R17 | Manager over-privilege on issue orders | Security | Keeper-binding handler check; tests proving Manager-with-Approve is rejected when not bound |
| R18 | Sealed presets silently get zero inventory access | Data/UX | Idempotent preset migration + drift test asserting all 16 features |
| R19 | Keeper transfer leaves pending work stranded | Lifecycle | Authority resolved against the current keeper; transfer UI shows affected pending count |
| R20 | Self-approved reversal | Security | `Approver != Requester` guard + test |
| R21 | Manager cannot assign keepers (Users=None) | Flow | Minimal `Warehouses.Write`-gated candidate query |
| R22 | New enum members render as English generic cards | UX | Catalog entries in the same slice; UI test/manual check |

---

## 8. Owner decisions required before implementation

| # | Decision | Recommendation / status | Note |
|---|---|---|---|
| D-RP1 | Add a 5th sealed preset `storekeeper`? | ✅ **LOCKED: Yes** (2026-11-09) | Extends `SealedRolePresets`, the V2 seed, and the seed-test counts |
| D-RP2 | Manager level on `Warehouses` and who assigns keepers | ✅ **LOCKED:** `Warehouses=Write`, `Inventory=Approve`; minimal candidate query | No `Users` access needed for Manager |
| D-RP3 | Reversal approval authority | ✅ **LOCKED:** `Inventory=Approve` (Manager + Higher Manager), approver ≠ requester | Higher Manager passes via `IsAdmin` bypass |
| D-RP4 | May a Manager execute/cancel issue orders? | ✅ **LOCKED: No** unless bound as that warehouse's keeper | Handler returns `Result.Failure`; test required |
| D-RP5 | Separation of duties on reversals | ✅ **LOCKED (default): Enforce** — even for Higher Manager | Approver ≠ requester |
| D-RP6 | Order editor when the user lacks `Inventory.Read` | ✅ **LOCKED (default):** hide availability, disable stock sourcing, Arabic hint, supplier-only | Handler rejects any stock portion |
| D-RP7 | Stock-in product picker gate | ✅ **LOCKED (default):** require `Products.Read`; included in `storekeeper` | `Inventory.Write` alone does not expose the catalog |
| D-RP8 | Keeper transfer vs pending issue orders | ✅ **LOCKED (default):** authority follows current keeper; snapshot audit-only | No bulk reassignment |
| D-RP9 | Reversal approval page location | ✅ **LOCKED (default):** `/inventory/reversals` (no Admin claim) | Manager remains eligible |
| D-RP10 | Who records customer returns (phase B) | ✅ **LOCKED (default):** `Inventory.Write` + keeper binding | Sales cannot post returns |

All ten decisions are locked; the six defaults were accepted on 2026-11-09 and written into `INVENTORY-MANAGEMENT-SPEC.md` v1.3.

---

## 9. Test plan (permissions-focused)

**Application (SQLite, `EnsureCreated`, snake_case):**
1. Authorization matrix: each level satisfies exactly the levels in §4.2, including `Approve`.
2. `Approve` policy is registered and does not silently deny (roles R1 regression).
3. Execute/cancel: keeper succeeds; `Inventory.Write` non-keeper fails with `Result.Failure`; Manager-with-`Approve` non-keeper fails; Higher Manager succeeds and is audited.
4. Keeper transfer: old keeper rejected, new keeper accepted on the same pending order.
5. Reversal: request by keeper succeeds; approval requires `Approve`; requester ≠ approver; double reversal rejected.
6. Preset drift: the 5 presets × 16 features match the locked matrix exactly.
7. Sealed role: rename/delete/set-permission rejected; preset seed is idempotent.
8. Candidate query: returns only minimal fields; requires `Warehouses.Write`; marks `HasInventoryWrite` correctly (including `IsAdmin`).
9. Order save with stock items: reservation under `Orders.Write`; race returns `"Conflict"`.

**Web/UI:**
- Nav group hidden/shown by permission.
- Role editor renders 16 cards with 4 segments; sealed roles read-only.
- Issue-order action buttons hidden for non-bound users; handler still rejects if forced.
- Reversal approval visible only with `Approve`.

---

## 10. What does not change

- Roles plan locked decisions 1–10, `IsAdmin`/Higher Manager, sealed-role immutability, additive-only migrations.
- `Supply`, delivery, documents, pricing and the shared work-record pipeline (inventory D2/D16).
- No manual stock editing (D21); no DB-level `NOT NULL`/`CHECK`/FK changes on existing tables.
- The roles editor's existing component architecture (`RolePermissionCards`, `RoleEditorState`, `RoleFeatureCatalog`).

---

## 11. Cross-document contradictions noticed

| Source A | Source B | Resolution |
|---|---|---|
| roles study §3: Manager `Users=Write`; Accountant seeded | roles plan decisions 1/5/8: Manager `Users=None`; Accountant deferred | The **plan** is authoritative; inventory must not assume Manager can read users |
| roles plan decision 1: Manager `Users=Read` | decision 8: `Users=None` | Decision 8 wins (later, explicit) |
| inventory §7: "keeper + admins" | roles plan: roles, not people; `Approve` level | Rewrite §7 per §4.4 |
| inventory §3: row-level via `PermissionAccess` | `PermissionAccess` is feature-level only | Add the keeper-binding check in handlers (§4.5) |
| redesign decision: "no notification bell (no such services)" | inventory D13: bell required | Inventory introduces the service; the old rationale no longer applies |
| inventory D9: "admin approval" | roles plan: `Approve` is the approval mechanism | Amend D9 (§5) |
