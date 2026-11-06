# Feature Spec — Inline role creation from the add-user editor («تحديد الصلاحيات»)

Status: IMPLEMENTED (2026-11-06) — backend returns `Result<int>`, `RoleEditorDialog` added,
`Users/Create` wired, handler test added (42/42 green).
Branch context: `feature/unified-redesign` (T13 roles-editor redesign is committed; T8 add-user page is committed).
Related: the dialog should embed the shared `RolePermissionCards` component once it exists —
see `roles-editor-permission-cards.md` (cards-grid redesign of the same matrix).

## Problem

On `/admin/users/create` the user must pick a role. If no suitable role exists they must leave
the page (losing ALL form state — username, password, switches — there is no draft persistence),
create a role on `/admin/roles/create`, then navigate back and re-enter everything.

Goal: a «إنشاء دور جديد…» button inside the add-user editor that opens role creation **as a dialog**;
saving the dialog closes it, refreshes the roles dropdown, and auto-selects the newly created role —
all without losing the in-progress user form.

## Review of what already exists (verified 2026-11-06)

| Piece | Location | Reuse |
|---|---|---|
| Roles dropdown | `Users/Create.razor` ← `GetRolesQuery` → `RoleListItem(int Id, string Name, int PermissionCount)`; selection binds `_model.RoleId` | Direct |
| Role matrix UI (T13) | `Roles/Editor/Editor.razor` (create+edit routes) | Visual pattern only — `GetFeatureName/GetFeatureDescription/GetFeatureIcon/FeatureTone` are `private static` on the page component → must be extracted |
| Editor state | `RoleEditorState` / `RolePermissionEditorState` under `OrderHub.Web.Components.Pages.Features.Administration.Roles.Editor` | Already `public` — dialog reuses as-is |
| Creation backend | `CreateRoleCommand(CreateRoleRequest) : IRequest<Result>` — handler: `NameExistsAsync` → `new Role(name)` → `SetPermission` loop → `SaveChangesAsync` → `Result.Success()`; `role.Id` is available after SaveChanges but discarded | **Gap: no payload returned** |
| Authorization | `[RequirePermission(Users, Write)]` on the command; page is `[Authorize(Roles="Admin")]` + `[PermissionAuthorize(Users, Write)]` | Compatible — no new permission needed |
| Result plumbing | `Result<TValue> : Result` with `static Result<TValue>.Success(TValue)` exists in `Domain/Common/Result.cs` | Typed result available; generic `Failure` factory not confirmed |
| Tests | None reference `CreateRoleCommand` | Zero blast radius |
| Caller-notify patterns | `SupplierEditorButton.OnCreated` (EventCallback<int>), `DialogResult.Ok(data)` | Dialog returns `DialogResult.Ok<int>` |

## Change spec

### 1) Application (backend touchpoint first — AGENTS rule 4)

- `CreateRoleCommand : IRequest<Result<int>>` (was `IRequest<Result>`).
- Handler: after `SaveChangesAsync` return `Result<int>.Success(role.Id)`; failure (duplicate name) returns `Result<int>.Failure(...)`.
- Verify `Result<TValue>.Failure(string)` exists in `OrderHub.Domain/Common/Result.cs`; only `Success(TValue)` confirmed. If missing, add a small `new static` factory (value-object change allowed; no entity change).
- Callers unaffected: `Roles/Editor/Editor.razor.CreateAsync` only checks `IsSuccess` and `Result<T>` derives from `Result` → compiles unchanged.

### 2) Web — new shared catalog (pure extraction)

- New static class `OrderHub.Web/Components/Pages/Features/Administration/Roles/Editor/RoleFeatureCatalog.cs`
  hosting `Name`, `Description`, `Icon`, `Tone` per `PermissionFeature` (moved verbatim from the T13 page, made `public`).
- `Editor.razor` switches to the catalog — identical rendering, no visual change.

### 3) Web — new `RoleEditorDialog.razor` (the dialog)

- Shared Tailwind dialog pattern: `MudDialog Class="flex max-h-[92vh] flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white p-0 shadow-xl w-full"` + `TitleClass/ContentClass/ActionsClass="p-0"` + `Gutters="false"`; icon header + docked close button; scroll body `min-h-0 flex-1 overflow-y-auto bg-slate-50/70 p-5`; footer with save-readiness dot + cancel/save.
- Dialog options from the page: `MaxWidth.Medium`, `FullWidth = true`, `CloseButton = false`, `CloseOnEscapeKey = false`, `BackdropClick = false` (matches existing page dialogs — matrix needs the width).
- Content (T13 matrix, trimmed, create-only — no load/fetch state, matrix bootstraps from enums):
  - Name field: `MaxLength=100`, custom 0/100 counter rendered with `CultureInfo.InvariantCulture` (MudBlazor's own `Counter` renders in CurrentCulture → Arabic digits; do NOT use it).
  - Bulk buttons: إعادة تعيين (all None) / قراءة للجميع / كتابة للجميع — via `SetAllLevels(PermissionLevel?)`, `StateHasChanged`, guarded while saving.
  - Feature rows: fixed-tone icon tile (`RoleFeatureCatalog.Tone`) + Arabic name + description + segmented `MudToggleGroup` T="PermissionLevel" (لا شيء / قراءة / كتابة) — 9.11 API is `Value`/`ValueChanged` (NOT the old `SelectedValue`); group `Color.Primary`.
  - Zero-permission amber warning (verbatim T13 text) when no level is set — role stays savable with none.
- Save flow: `_form.ValidateAsync()` → `CreateRoleCommand` via `IRequestExecutor`.
  - Success: success toast + `MudDialog.Close(DialogResult.Ok<int>(roleId))`.
  - Failure: inline red `_saveError` (e.g. «اسم الدور مستخدم بالفعل.») — dialog stays open, user input preserved.
- Edit mode is NOT in this dialog (create-only). The existing full-page editor keeps edit/create-page duties.

### 4) Web — `Users/Create.razor` wiring

- Button «إنشاء دور جديد…» (shield/manage-account icon) placed with the roles select in the
  الصلاحيات والوصول card; disabled while `_saving`.
- Handler (own busy flag, `IsBusy`-style guard to prevent double-dialog):
  `DialogService.ShowAsync<RoleEditorDialog>` → on
  `result is { Canceled: false, Data: int newRoleId }`:
  reload `_roles` via `new GetRolesQuery()` → `_model.RoleId = newRoleId` → StateHasChanged (implicit).
- The rest of `_model` (username, password, switches) is untouched — no validation side effects.
- Toast: dialog's own success message is sufficient.

### 5) Conventions / tests

- Zero new CSS, zero JavaScript, Arabic-only copy, numbers InvariantCulture.
- One new SQLite xUnit test in `OrderHub.Tests` (Application-level):
  `CreateRoleCommandHandler_ReturnsNewRoleIdAfterSave`.
- Optional follow-ups (separate tasks, ask before doing):
  1. Same button on `Users/Edit.razor` (same dialog, same wiring).
  2. Extract `BuildPermissionInputs()` into `RoleEditorState` so page + dialog share it
     (or duplicate the ~10 LINQ lines locally in the dialog; extraction preferred).

## Acceptance criteria

1. From the add-user editor, creating a role via the dialog never leaves the page; the user form's
   filled values survive.
2. After a successful dialog save the roles dropdown contains the new role AND it is selected
   (driven by the returned `roleId`, never name-matching).
3. Duplicate role name keeps the dialog open with a clear inline error.
4. Saving a role with zero permissions works (with the existing warning).
5. Build clean; all 41 existing tests + the new test pass.
