# Roles & Permissions Redesign — Final Plan (decisions locked 2026-11-08)

## Owner decisions
1. **Manager** manages Users with **Read** only (no user management writes).
2. **Approve level = YES**, added in this batch (`None < Read < Write < Approve`).
3. **Higher Manager = keep `IsAdmin`**, re-presented as an explicit account type with clear display/wording.
4. **Custom roles = blank matrix only** (no "create from template") — prevents misuse.
5. **Accountant = deferred** — not seeded now; added later as a sealed role when needed.
6. **Existing data** — studied below; see "Existing data handling".
7. Update the repo doc `plans/roles-and-permissions-study.md` with these decisions when executing.
8. (2026-11-08, late clarification) **Manager = business/operational administration only — no system administration whatsoever.** Users, Logs, Settings = **None** for his preset. All system-administration pages stay gated by the Admin role claim (Higher Manager only). This resolves R2 — the Users page Admin gate stays exactly as-is; no gate widening.
9. (2026-11-08, constraint philosophy) **No DB-level change may conflict with legacy data.** Where any conflict with saved/old data is even conceivable, enforce the constraint **in code** (Domain guards / Application handlers) rather than in migrations or DB constraints. Migrations must remain **purely additive** (new columns with defaults, new nullable columns, new rows); no rewrites of existing rows, no renames, no data-type changes.
10. (2026-11-08, final round) **All four remaining items resolved with recommended options:**
    - **R3 → Keep deletes at Write level** in this batch; raising them is revisited with the approval-workflows epic.
    - **R4 → Auto-refresh claims**: when the Users edit handler flips `IsAdmin`, it re-issues the user's auth ticket so the Admin claim matches immediately (no 8h drift).
    - **R12 → Block duplicate names**: Create/Rename role commands reject preset role names with a clear Arabic message (code guard, no DB change).
    - **Preview card → YES**: read-only permission-summary card beside the role dropdown on user pages (display only, not the rejected template flow).
    - **Feature design: COMPLETE — no open questions remain.**

## Locked preset matrix (sealed, seeded — 4 roles)

| Role | RoleKey | Orders | Documents | Products | Categories | Properties | Clients | Suppliers | Deliverymen | ShippingCarriers | Cities | PaymentMethods | Logs | Users | Settings |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| مبيعات | `sales` | Write | Read | Read | Read | — | Write | — | Read | Read | Read | Read | — | — | — |
| مشتريات | `purchasing` | Read | Read | Read | — | — | — | Write | — | — | Read | Read | — | — | — |
| إدخال البيانات | `data-entry` | — | — | Write | Write | Write | — | — | — | — | Write | Read | — | — | — |
| مدير | `manager` | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | — | — | — |
| (Higher Manager) | — flag `IsAdmin` | full bypass, incl. everything above | | | | | | | | | | | | | |

(`—` = None; no permission row stored.)

## Approve level semantics
- `PermissionLevel`: add `Approve`.
- `AuthorizationService.IsAllowedAsync` (Application): `Read` satisfied by Read|Write|Approve; `Write` by Write|Approve; `Approve` by Approve only.
- `RolePermission.ValidateLevel` unchanged (None rejected).
- UI: `RolePermissionCards` / `SetAllLevels` / editor states go 3 → 4 segments.

## Existing data handling
- **New columns** on `roles`: `is_sealed` (bool, NOT NULL default false), `role_key` (varchar(100), NULL allowed, unique index — Postgres allows multiple NULLs).
- **Existing custom roles**: untouched — remain custom (`is_sealed=false`, `role_key=null`), their `role_permissions` rows preserved.
- **Existing users**:
  - `IsAdmin=true` → become Higher Manager seamlessly (same flag, just re-presented; no data change).
  - Users with assigned roles → unchanged.
  - Users with **no role and not admin** → unchanged data; the Users list page will surface a «بدون دور / بدون صلاحيات» indicator so the admin notices.
- **Seed logic**: insert presets **by `role_key` only when missing** (idempotent upsert). Skips never overwrite or seal anything existing.
- **Edge case — name collision**: a pre-existing custom role already named «مبيعات/مشتريات/…» would violate `ix_roles_name`. Mitigation in the seed SQL: insert with name-conflict check; if a custom role holds the name, the seed **does not insert that preset** (custom role continues as-is; owner can rename it later to free the name). Documented in the migration.
- **Existing `role_permissions.level` values**: stored as text; `Approve` never existed before → no data rewriting required.
- Migrations applied manually via the standard `dotnet ef` workflow (no auto-migrate; design-time factory `DesignTimeAppDbContextFactrory.cs`; descending timestamp naming, e.g. `20261108000000_SeedSealedRoles.cs`).
- SQLite tests use `EnsureCreated` → new columns/seed come free; add new xUnit tests for the guards.

## Implementation phases (build order per House rule 4)

### Phase 1 — Domain
- `PermissionLevel`: add `Approve` member with `[Description("اعتماد")]`.
- `Role`: add `IsSealed` (private) + `RoleKey` (private, null-ok), internal factory support; guards: `Rename`/`SetPermission` throw if sealed («Role is sealed.»).

### Phase 2 — Application
- `AuthorizationService.IsAllowedAsync`: new level semantics (above) **including the guard-line whitelist `(Read, Write, Approve)`** (see R1).
- Permission-policy factory enumeration extended to include Approve (see R11).
- Role commands (`CreateRole/UpdateRole/DeleteRole` + handlers): reject sealed roles for edit/delete with clear Arabic error; reject duplicate preset names (R12).
- Verify any switch over `PermissionLevel` across Application (grep) compiles with the new member.

### Phase 3 — Infrastructure
- `RoleConfiguration`: map `is_sealed`, `role_key` + unique index.
- Migration (descending timestamp): add columns/index + idempotent seed of the 4 presets (`sales`, `purchasing`, `data-entry`, `manager`) with the exact matrix above — raw SQL, no explicit IDs, name-collision skip.

### Phase 4 — Web UI
- Roles list (`/admin/roles`): «دور جاهز» badge on sealed rows; hide/disable edit + delete (sealed → view only).
- Role editor page + `RoleEditorDialog`: matrices become 4-level (None/Read/Write/Approve); sealed roles render read-only.
- Users pages (create/edit): account-type section — either assign a role (split select: «أدوار جاهزة» group + «أدوار مخصصة» group) or Higher Manager («مدير أعلى» with explicit description that it bypasses all roles/permissions); choosing Higher Manager disables the role picker.
- Permission preview card beside the role select (counts per level) — read-only summary, no template-copy flow.
- Users edit command/handler: re-issue the auth ticket when `IsAdmin` changes (R4 resolution).
- Users list: indicator for users without role (and not admin).
- Verify (no changes expected) all `[Authorize(Roles = "Admin")]` gates in Administration pages match the manager-is-not-system-admin decision.

### Phase 5 — Tests
- Domain: sealed guard tests; Approve validation.
- Application: authorization semantics matrix (Read/Write/Approve satisfaction), sealed-rejection and reserved-name rejection in role commands, preset-matrix drift assertion (R15).
- SQLite tests identical options to existing suite (snake_case + EnsureCreated).

### Phase 6 — repo doc sync
- Update `plans/roles-and-permissions-study.md` in the repo: mark decisions as locked (this file's matrix + existing-data section), remove superseded options.

## Re-think pass 1 (2026-11-08) — risks, impacts, verification checklist

### R1 · Silent deny in `AuthorizationService.IsAllowedAsync` (HIGH — must handle)
The guard line `requiredLevel is not (PermissionLevel.Read or PermissionLevel.Write) → return false` explicitly whitelists Read/Write. If we add `Approve` without touching this line, **every Approve check silently fails**. Phase 2 must include rewriting this guard to `(Read, Write, Approve)` plus the satisfaction switch. Verification: test "user with Approve satisfies `[RequirePermission(F, Approve)]`".

### R2 · `[Authorize(Roles = "Admin")]` gates vs Manager Reads — **RESOLVED (decision 8)**
Manager has no Users/Logs/Settings access — business operations only. All Administration pages keep their Admin-claim gates unchanged; no gate widening anywhere. (Kept in the record because it drove the matrix change.)

### R3 · Deleting data stays a Write-level action — **RESOLVED (keep Write)**
Deletes stay Write-gated in this batch; raising them belongs to the approval-workflows epic.

### R4 · Cookie staleness on the Admin claim — **RESOLVED (auto-refresh)**
When a user's `IsAdmin` flag changes, the Users edit handler re-signs the user's authentication ticket so the Admin claim matches immediately.

### R5 · Preset seeding vs identity sequence (avoided by design)
EF `HasData` with fixed int PKs + Postgres identity is a known drift trap. Avoided: **raw SQL inserts without explicit IDs** (identity allocates), deduped by `role_key` (unique index), skipping any preset whose name collides with an existing custom role. The migration runs inside a PG transaction with the column additions — all-or-nothing.

### R6 · Raw SQL in migration vs snake_case schema (verification step)
With `UseSnakeCaseNamingConvention()`, quoted identifiers must use real names (`"roles"`, `"role_permissions"`, `"is_sealed"`, `"role_key"`, enums stored as text). Verify against the generated migration; rehearse against a scratch Postgres DB (CI does not rehearse PG).

### R7 · `PermissionLevel` text storage supports `Approve`
`role_permissions.level` is `varchar(20)` storing enum names; the check constraint only bans `'None'`. Impact: none; assert in tests.

### R8 · Tests / UI swept by the new enum member
Exhaustive switches/tests over `PermissionLevel` will fail compile (good). Sweep `PermissionLevel` across Tests/Application. UI: `MudToggleGroup` goes 3 → 4 segments — verify compact width fit.

### R9 · No auto-mapping of existing users to presets (intentional)
Seed never reassigns users. Permission-less users remain so, surfaced via a «بدون دور» indicator in the Users list. No silent access gain/loss.

### R10 · Legacy `user_permissions` table (dead, impact: none)
Superseded by roles; untouched.

## Re-think pass 2 (2026-11-08) — saved-data & legacy-data audit (program/business logic)

### Verdict on old data: NO migration/DB conflict possible — the change is purely additive
- **New columns** `is_sealed` (NOT NULL default false) and `role_key` (all-NULL): every legacy `roles` row keeps its exact values; the unique index on `role_key` is over a new all-NULL column — no legacy value can violate it (Postgres/SQLite unique indexes ignore NULLs).
- **Legacy `role_permissions` rows** are not touched by the seed (upsert by `role_key` touches nothing existing; name-collision presets are skipped, never renamed/deleted).
- **Legacy users**: `IsAdmin`, `RoleId`, and all role assignments unchanged — zero permission shifts caused by the migration itself.
- **Backward compatibility of the Approve member**: adding it cannot change any legacy check outcome — `Read` still satisfied by Read|Write (+Approve now), `Write` satisfied by Write (+Approve now). No user gains or loses access *from the enum extension alone*; only new explicit Approve grants do.
- **Everything behavioral is enforced in code, per owner directive (decision 9)**: seed-skip logic, seal guards (`Rename`/`SetPermission` throw on sealed), command-level edit/delete rejections, reserved-preset-name guard (R12), and the silent-deny fix (R1). DB carries only the additive columns/index.

### R11 · Permission-policy factory may enumerate only Read/Write (HIGH, code-level fix)
`AddPermissionAuthorization` dynamically registers policies for `{Feature} × {Read, Write}`. If that enumeration doesn't include `Approve`, a future `[PermissionAuthorize(F, Approve)]` produces an unregistered policy name → authorization failure/exception. Phase 2 must extend the enumeration to include Approve. Grep target: `OrderHub.Web\Infrastructure\Authorization\ServiceCollectionExtensions.cs`.

### R12 · Reserved preset names — **RESOLVED (block duplicates)**
Create/Rename role commands reject names colliding with the sealed presets («مبيعات»، «مشتريات»، «إدخال البيانات»، «مدير») with an Arabic message. Code-level guard only — no DB constraint (owner directive).

### R13 · Substitution-not-accumulation already holds (positive audit)
User↔role is a single nullable `RoleId` — assigning a new role replaces the old one; there is no multi-role accumulation path, matching enterprise "substitute on move" practice. No change needed; noted as verified.

### R14 · Editing a user while logged in (business logic, already safe)
Permissions resolve from the DB per request, so role changes apply on the user's very next action; there is no permission cache to invalidate. Only the Admin role claim was session-stable — now auto-refreshed (R4).

### R15 · Preset drift protection (code-level)
Preset permissions live in DB rows but their definition is versioned in code (seed SQL + tests). Guard rank: seed is idempotent; a future change to a preset means a *new migration* (owner-approved), never an ad-hoc UI edit (sealed) and never a code edit without a migration. Tests assert the exact preset matrix to catch accidental drift.

### Feature-completeness verdict — **DESIGN COMPLETE**
No open decisions remain. Implementation-time verification checkpoints only: R1 (guard-line + Approve semantics), R5/R6 (additive migration + snake_case raw SQL rehearsal), R7/R8 (enum sweep + 4-segment matrix fit), R11 (policy factory enumeration).

## Explicitly out of scope (future separate epic)
- Approval workflows on orders/documents (will consume the `Approve` level via `[RequirePermission(feature, Approve)]`).
- Role-based hierarchy/rank beyond the Approve level.
- Accountant role seeding (deferred until needed; added as a sealed preset migration).
- Removing `IsAdmin` / claims changes.
