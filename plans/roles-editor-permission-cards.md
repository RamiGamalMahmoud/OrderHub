# Feature Spec — Roles editor: permissions matrix as a cards grid

Status: IMPLEMENTED (2026-11-06) — cards grid shipped in `feature/unified-redesign`; open
decisions resolved as (a) omit save-as-draft and (b) omit functional category, matching T13.
Branch context: `feature/unified-redesign`. Parent of the matrix UI: T13 (committed).
Related: `user-editor-inline-role-creation.md` (the new `RoleEditorDialog` embeds this same component).

## Problem / request (user request, 2026-11-06)

The T13 permissions matrix is currently a rows list (tile + name + description left,
segmented selector right). Convert it to a visually calmer **grid of self-contained cards**
(3 columns) per the template:

- Card top: colored feature icon tile + bold Arabic feature name + **current-level status badge**.
- Card middle: the existing Arabic description line explaining the feature.
- Card bottom: the three options (لا شيء | قراءة | كتابة) as a modern **segmented selector**
  stretched across the card bottom, with a clear selected color.
- Plus: an **instant search field** above the cards; keep the **quick bulk-select buttons**
  (all-write / all-read / reset-to-none).
- Keep the rest of the screen intact: unified header (crumbs + RBAC badge), role data card
  (name + required marker + 0/100 counter), guide card (3 levels) with distribution summary,
  sticky action bar.

## Conflicts with prior decisions / backend reality (flagged to user, answers pending)

1. **«حفظ كمسودة» (save-as-draft)** — listed in the user's template recap, but T13 explicitly
   dropped it and `Role` has NO draft state (no field, no command). Choices:
   a) omit (default, honest) — b) scope a real draft backend (new domain field + migration +
   application commands — large, separate task).
2. **«تصنيف الدور الوظيفي» (functional role category)** — no domain field; dropped in T13.
   Choices: a) omit (default) — b) add a category to the domain (separate backend task).
3. The top-nav/footer chrome items in the user recap are the **global app shell (T1, committed)**,
   not part of this screen — treated as satisfied, not re-implemented here.

## Implementation spec

### New shared component `RolePermissionCards.razor`
Location: `OrderHub.Web/Components/Pages/Features/Administration/Roles/Editor/`.

- **Parameters**: `IList<RolePermissionEditorState> Permissions` (the public state list; component
  mutates `Level` directly, same as the page today), `bool Disabled` (save-in-progress),
  optional `bool ShowBulkActions = true` (calendar for dialog/page reuse).
- **Structure**:
  - Toolbar row: `MudTextField` instant search (`Immediate`, dense, outlined, «ابحث عن قسم…»)
    + bulk buttons (إعادة تعيين → `SetAllLevels(null)`, قراءة للجميع, كتابة للجميع), all guarded
    by `Disabled`.
  - Cards grid: `grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3` rendering
    `VisiblePermissions` (name/description match the Arabic search, InvariantCulture-safe
    filtering on strings only).
  - Card: `rounded-2xl border border-slate-200 bg-white p-4 shadow-sm` —
    - Header row: fixed-tone icon tile (`RoleFeatureCatalog.Tone` — the catalog extraction from
      the inline-creation spec is a prerequisite or ships together) + bold name + **status badge**
      showing the CURRENT level (لا شيء = slate pill, قراءة = sky pill, كتابة = brand pill);
      badge text/tones from one `LevelBadge(state.Level)` helper. Selected-level badge updates
      live when the user toggles.
    - Body: `RoleFeatureCatalog.Description` line (existing Arabic text, unchanged).
    - Bottom: `MudToggleGroup T="PermissionLevel"` with the three `MudToggleItem`s bound to
      `permission.Level` (9.11 API: `Value`/`ValueChanged`, NOT the old `SelectedValue`),
      full width (`Class="w-full"` on the group, stretch items), `Color.Primary`,
      `Size.Small`… keep readable; disabled while `Disabled`.
  - Search-empty state: small centered «لا يوجد قسم مطابق للبحث» block.
- Bulk ops + search live in the component; `StateHasChanged` handled internally.
- Zero new CSS: Tailwind utilities only; **no JavaScript**.

### `Roles/Editor/Editor.razor` (page)
- Replace the current matrix section's inner content with `<RolePermissionCards Permissions="_state.Permissions" Disabled="_saving" />`
  inside the same section-card chrome (keep the section header «الصلاحيات» + description,
  or move that header INTO the component — decide by template fidelity).
- Everything else unchanged: PageScaffold + crumbs + RBAC badge, role data card, guide card,
  summary card, zero-permission warning, sticky action bar.

### `RoleEditorDialog.razor` (from `user-editor-inline-role-creation.md`)
- Embeds the same `<RolePermissionCards Permissions="_state.Permissions" Disabled="_saving" ShowBulkActions="true" />`
  instead of a private matrix copy — page and dialog stay visually identical by construction.

## Prerequisite ordering

1. `RoleFeatureCatalog` extraction (shared by cards + tiles).
2. `RolePermissionCards` component.
3. Editor page swap.
4. `RoleEditorDialog` consumes it (inline-creation feature task).

## Acceptance criteria

1. 14 feature cards render in a responsive 3/2/1-column grid; each card shows tone tile, bold name,
   live status badge, description, and a full-width segmented none/read/write selector.
2. Search filters cards instantly (Arabic name/description) with a graceful empty state.
3. Bulk buttons + per-card toggles behave exactly as today (same state classes, same guard).
4. Page and dialog use the SAME component — no duplicated matrix markup.
5. No CSS, no JS additions; build clean; 41 existing tests + suite green.
