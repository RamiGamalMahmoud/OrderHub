# Roles & Permissions (RBAC) — Implementation Review

- **Reviewed commit:** `95db28a feat(rbac): add Approve level, sealed preset roles, and DB-checked higher managers`
- **Branch:** `feature/delivery-reassignment`
- **Date:** 2026-11-09
- **Reference:** `plans/roles-and-permissions-spec.md` (+ `plans/roles-and-permissions-plan.md`)
- **Diff scope:** 57 files, +5119 / −135
- **Method:** static review file-by-file, Release build, full test suite, Node test, Tailwind regeneration check

> **Scope note.** Uncommitted working-tree changes (`Trips` added to `PermissionFeature`, `.kiro/`, `plans/TRIPS-*`) are *not* part of this commit and are reported separately in §8.

## 1. Verdict

**Functionally complete and mergeable.** No security bypass, no data-destructive defect, and the additive migration/seed is sound. Two medium items should be tracked (stale navigation after promotion/demotion; missing spec test 20), plus three low edge cases. PostgreSQL rehearsal and the manual browser checklist were not run and remain deployment gates.

## 2. Build and test evidence

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build OrderHub.sln --configuration Release` | `Build succeeded. 0 Warning(s) 0 Error(s)` (19.65s) |
| Tests | `dotnet test OrderHub.sln --configuration Release --no-build` | `Passed! — Failed: 0, Passed: 143, Skipped: 0, Total: 143` |
| Node test | `node OrderHub.Tests/Web/order-local-time.test.mjs` | Passed (browser-local formatting, invalid dates, 23/25-hour DST boundaries) |

## 3. Acceptance criteria (Appendix B)

| # | Criterion | Status | Note |
| --- | --- | --- | --- |
| 1 | Four levels `None < Read < Write < Approve` | Met | `PermissionLevel` with `[Description("اعتماد")]`; satisfaction matrix in `AuthorizationService` |
| 2 | Sealed roles protected; Higher Manager DB-checked per operation | Partial | Authorization is fully correct; nav/header cache the HM flag for the circuit lifetime (M1) |
| 3 | Preset names reserved (trim + case-insensitive) | Met | `SealedRolePresets.IsReservedName` + uniform Arabic message |
| 4 | Immediate promotion/demotion without restart | Partial | Enforced per operation; nav and header do not refresh until page reload (M1) |
| 5 | Sealed roles seeded once, idempotent | Met | `SealedRoleSeedSql.V1`: insert-only, name/key collision skip, per-`(role_id, feature)` dedupe |
| 6 | Additive-only migration | Met on SQLite | `SeedSealedRoles` + consistent snapshot; PostgreSQL not rehearsed |
| 7 | Suite green, no warnings | Partial | 143/143 pass, 0 warnings; spec test 20 missing (M2) |
| 8 | Roles UI: sealed badge, read-only, 4 permission segments | Met by inspection | Visual confirmation pending |
| 9 | Users UI: grouped roles, HM switch, `RoleId` preserved | Met by inspection | Visual confirmation pending |
| 10 | Manual live promotion/demotion checklist | Not run | Needs browser session + PostgreSQL |

## 4. Findings by severity

### M1 — Medium: Higher Manager state in navigation is cached for the circuit

- **Files:** `OrderHub.Web/Components/Layout/MainLayout.razor:158, 172, 176`; `OrderHub.Web/Components/Layout/NavMenu.razor:273, 291, 293`.
- **Behavior:** the `_identityResolved` and `_permissionsEvaluated` gates stop recomputation after the first load. After a promotion or demotion, the “Users”/“Roles” links and the «مدير أعلى» chip stay stale until the user reloads (F5) or starts a new circuit.
- **Security impact:** none. Every admin page is guarded by `IsHigherManagerAsync()` and every command by `[RequireHigherManager]`, both hitting the database per operation. A promoted user sees an access-denied page until reload; a demoted user is denied execution even if the link is still visible.
- **Spec deviation:** §5.2.2 and §10 checks 2–3 require immediate reflection without restart.
- **Recommendation:** invalidate `_permissionsEvaluated` on authentication-state change / current-user events, or surface an explicit reload affordance and document reload as intended behavior.

### M2 — Medium: Spec test 20 is missing

- **Required by §9:** `RoleEditorState_BuildPermissionInputs_KeepsApprove` in `RolePermissionEditorStateTests.cs`.
- **Actual:** file absent. `Approve` is covered at handler level (`UpdateRole_ApproveLevel_Persists`) and at authorization level, so runtime risk is low, but the UI-payload contract (filter `None`, keep `Approve`) has no direct test.
- **Recommendation:** add the test. The implementation itself is correct on inspection (`BuildPermissionInputs` filters only `None`).

### L1 — Low: legacy custom role whose stored name equals a preset cannot be saved

- **File:** `OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleCommandHandler.cs:35-36`.
- **Behavior:** for the §7.4 legacy case, the reserved-name guard rejects the save before rename logic runs, so the role is not savable from the UI until renamed.
- **Impact:** low — data is intact and the repair path is rename-first.
- **Recommendation:** allow the role’s own current name (`input.Name` vs `role.Name`) and reject only *new* reserved names.

### L2 — Low: seed name-collision check is not normalized

- **File:** `OrderHub.Infrastructure/Migrations/SealedRoleSeedSql.cs`.
- **Behavior:** the SQL compares names exactly, while `IsReservedName` trims and ignores case. A legacy role named `"المبيعات "` (trailing space) would coexist with the preset of the same meaning.
- **Impact:** cosmetic/organizational, not functional.
- **Recommendation:** normalize with `lower(btrim(name))` on the next seed revision — no rewriting of existing rows.

### L3 — Low (pre-existing): blank name reaches `Role.Rename`

- **File:** `OrderHub.Domain/Models/Role.cs` (`Rename`).
- **Behavior:** `Rename("")` throws `ArgumentException`. The UI prevents it via `RequiredError` and `ReadyToSave`, but a non-UI caller gets an exception instead of a failure result.
- **Recommendation:** defensive check in the handler or return an Arabic failure message; not a merge blocker.

## 5. What checked out

- **Satisfaction matrix:** `AuthorizationService` denies any level not in `Read/Write/Approve` even for the Higher Manager (no silent allow-all), then lets the HM bypass, then applies per-level satisfaction.
- **Command guard:** `AuthorizationBehavior` checks permission then HM and raises `AuthorizationDeniedException`; all 11 admin requests (5 roles, 6 users) carry `[RequirePermission]` + `[RequireHigherManager]`.
- **Role claim removal:** `Controllers/Auth.cs` no longer issues an `Admin` role claim; no `IsInRole("Admin")` or `Roles="Admin"` remains, and every admin query is reached only from HM-gated pages.
- **Domain sealing:** `EnsureNotSealed()` throws on `Rename`/`SetPermission`; handlers return clear Arabic messages for sealed, reserved-name, and assigned-role cases.
- **Migration:** adds `is_sealed` (required, default false), `role_key` (varchar(100)), and unique `ix_roles_role_key`; `Down` drops only index/columns and keeps seeded rows (documented inline). Snapshot and Designer agree.
- **Seed:** 31 permission cells at 9/6/5/11 across the four presets, provider-neutral SQL, insert-only (no rewrites).
- **UI:** roles/users pages gated; sealed roles read-only with badge; editor has four segments and bulk actions; helper text at the 12px floor per the UI standard.
- **No dead ends:** no `TODO`s or unwired placeholder buttons in the feature files.

## 6. Accepted risks and unverified items

- **One extra DB read per operation** (`users.is_admin`) — accepted in §13.2, small cost.
- **Documented deferrals in §13:** open-session handling on demotion, direct-SQL sealing, seed re-application after rollback.
- **Not run:** PostgreSQL rehearsal (§11) — all tests ran on SQLite; manual checklist §10 items 1–12; visual verification of the four segments, `MudListSubheader`, and RTL rendering.
- **Branch hygiene:** the feature is committed on `feature/delivery-reassignment`, not a dedicated branch — worth noting in the merge request.

## 7. Recommendations, in order

1. Resolve M1 (nav/header refresh on HM change) or document reload as intended.
2. Add spec test 20.
3. Run the PostgreSQL rehearsal and manual checklist before release (the two migration steps are manual deployment actions).
4. Batch L1/L2/L3 as a follow-up.

## 8. Working-tree warning (outside this commit)

Uncommitted changes leave the tree internally inconsistent:

- `OrderHub.Domain/Enums/PermissionFeature.cs` inserts `Trips` («المشاوير») **mid-list** between `ShippingCarriers` and `Cities`, shifting numeric values of later members. Current impact is limited (only the dead legacy `user_permissions.feature` int column, with no EF reads), but it violates base rule 7 (do not disturb existing behavior).
- `RoleFeatureCatalog` has no `Trips` cases, so the role editor renders a generic “Trips” card with a blank description and default icon/tone.

**Recommendation:** append `Trips` at the **end** of the enum, and complete `RoleFeatureCatalog` (name/description/icon/tone) in the Trips feature commit itself.

---

*Prepared from static review, build, and tests; no application files were modified during this review.*
