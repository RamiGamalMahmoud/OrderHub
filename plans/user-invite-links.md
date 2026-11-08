# Plan: User Invite Links — دعوة مستخدم عبر رابط

Created: 2026-11-08 · Status: analysis/planning approved by owner — **not yet implemented**
Companion plan file; standalone (the task registry `plans/orderhub-redesign-tasks.md` must
NOT be modified for this feature — owner instruction 2026-11-08).

## Purpose

Replace the "admin types username + password" flow with invite links:

- The admin creates a link and sends it to the new user by any channel.
- The user opens the link and chooses a username, display name and password,
  with strong password rules and live username-availability checks.
- The account is created pending admin approval; only after approval can the user log in.
- The link is single-use and expires 3 days after creation.

## Locked decisions (owner-approved, 2026-11-08)

| Item | Decision |
|---|---|
| Permissions on invite | Fully deferred — the invite carries **no role**; role is assigned by the admin after approval via the normal user-edit screen |
| Management UI | New standalone admin page `/admin/invitations` |
| Completion form | Username + display name + password |
| Pending approval | User is created **inactive** (`IsActive = false`); the existing `DatabaseLoginCommandHandler` check already blocks inactive users from logging in; "approval" = the existing Activate action on the Users page. Pending users appear as غير مفعّل — **no new status field on `User`** |
| Password policy | Min 8 characters; uppercase + lowercase + digit **required**; symbol optional |
| Username policy | 3–**10** characters, Latin letters / digits / underscore only |
| Share mechanism | Readonly text field for manual copy only — no JS |
| Availability check | Live while typing (Blazor interactive) + authoritative check on submit |
| Expired invite | Listed with an "Expired" badge; only remedy is creating a new link (no extend / resend buttons) |
| Token storage | **Raw token stored in DB** (re-copyable later from the list for active invites). Accepted trade-off: token is single-use, 3-day life, locally-run app |
| Policy scope | New username/password policies apply **only** to the invite flow — the current admin create-user screen stays untouched |
| User origin flag | None — invite-created users are not distinguishable in the Users list |

## Existing code the design builds on (verified)

- `OrderHub.Domain/Models/User.cs` — `User.Create(userName, passwordHash, displayName, isActive, isAdmin)`,
  private setters, validation by `ArgumentException`; no status field beyond `IsActive`.
- `OrderHub.Application/Features/Administration/Users/Create/CreateUserCommandHandler.cs` —
  username uniqueness via `IUserRepository.UsernameExistsAsync`, hashing via `IHashShervice`.
- `OrderHub.Application/Features/Authentication/DatabaseLogin/DatabaseLoginCommandHandler.cs` —
  rejects login when `user is null || !user.IsActive`.
- `OrderHub.Application/Features/Administration/Admin/CreateAdmin/` — password hashing precedent.
- `OrderHub.Web/Components/Pages/Features/Authentication/Login.razor` — anonymous standalone
  auth shell (grid background, blue glows, rounded-3xl card, native eye toggle).
- `OrderHub.Web/Controllers/Auth.cs` — `/auth/login`, `/auth/logout` precedent.
- There is currently **no** token/invitation/expiry infrastructure anywhere in the repo.

## Implementation

### 1) Domain — `OrderHub.Domain/Models/UserInvitation.cs`
- New entity `UserInvitation`: `Token`, `CreatedAtUtc`, `ExpiresAtUtc`,
  `ConsumedUserId` (int?), `ConsumedAt` (DateTime?).
- Lifecycle: active / consumed / revoked — **"Expired" is derived** from
  `ExpiresAtUtc` vs now, not stored.
- House style: private setters, private parameterless ctor, factory + behavior
  methods with validation:
  - `Issue(DateTime utcNow, TimeSpan lifetime)` (default 3 days)
  - `Consume(int userId, DateTime utcNow)` — only when active + not expired
  - `Revoke()` — only when still active
  - `IsUsable(DateTime utcNow)`

### 2) Application
- `Features/Administration/Invitations/`:
  - `CreateInvitationCommand` (Users/Write) → `Result<InvitationCreated>` (token, URL, expiry date).
  - `GetInvitationsQuery` → paged list (status, created, expires).
  - `RevokeInvitationCommand` → may revoke only an active invite.
  - `CheckUsernameAvailabilityQuery` → powers the live check (format valid? not taken?).
- `Common/PasswordPolicy.cs` → the password rules with Arabic messages (reusable later).
- `Features/Authentication/CompleteInvitation/CompleteInvitationCommand` → in one transaction:
  1. token usable? (not consumed / not expired / not revoked)
  2. username format + uniqueness check
  3. password policy + hash via `IHashShervice`
  4. `User.Create(..., isActive: false, isAdmin: false)` with no role
  5. mark invitation consumed (Consume) + `SaveChangesAsync`
- Typed failures with Arabic messages: منتهية / مستخدمة / ملغاة / الاسم مأخوذ /
  كلمة مرور ضعيفة / صيغة الاسم غير صالحة / التوكن غير موجود.

### 3) Infrastructure
- `Configurations/UserInvitationConfiguration.cs` — snake_case `user_invitations`,
  unique index on token, status-derived columns + timestamps. Must survive
  SQLite in-memory tests (`EnsureCreated` + snake_case naming convention, FK constraints).
- `IInvitationRepository` (Application/Interfaces/Repositories) + implementation in
  `OrderHub.Infrastructure/Reopsitories/` (exact misspelled path preserved).
- New migration via the standard `dotnet ef migrations add AddUserInvitations` workflow
  (design-time factory `DesignTimeAppDbContextFactrory.cs`).

### 4) Web
- **Admin page** `/admin/invitations`:
  - `PageScaffold` + `MudTable` + `TableFooter`, paged.
  - Page gate: `[PermissionAuthorize(PermissionFeature.Users, PermissionLevel.Read)]`
    + `[Authorize(Roles = "Admin")]`; Write level required for create/revoke actions.
  - «إنشاء رابط دعوة» primary action → creates immediately → result dialog with a
    readonly link field + expiry date (manual copy; NO JS clipboard).
  - Table columns: الحالة (نشطة/مستهلكة/منتهية/ملغاة), تاريخ الإنشاء, تاريخ الانتهاء,
    الرابط (active invites only, readonly), إجراء «إلغاء».
  - Inline red/amber banners instead of `MudAlert`.
- **Completion page** `/invite/{token}`:
  - Anonymous (same pattern as `Login.razor`), `@layout StandaloneLayout`, same
    32px grid background + blurred blue glows + `rounded-3xl` card shell.
  - Fields: username (live green/red availability indicator), display name,
    password + confirm password with a visual rule checklist and show/hide toggle —
    pure Blazor interactivity, **no new JavaScript**.
  - Success card: «تم إنشاء حسابك، بانتظار موافقة الإدارة» + link to `/login`.
  - Invalid / expired / used / revoked token: same-shaped error card.
  - Does NOT touch `/setup/admin` or `CreateAdmin.razor.cs.cs`.
- **NavMenu**: item «روابط الدعوة» under the Administration group, permission-filtered.
  `NavMenu.razor` is the single nav-item source (Desktop dropdown + Mobile variants both).

### 5) Tests — xUnit + SQLite in-memory (identical options to existing tests)
- Complete-invitation handler: success (inactive user created + token consumed),
  expired, consumed twice, revoked, revoked→reuse blocked, username taken,
  weak password, bad username format, password hash stored (never plaintext).
- Password policy: every rule + Arabic messages.
- Repository: snake_case survival, unique token index, expiry-flag behavior.

## Constraints / house rules honored
- No new JavaScript; no runtime font/CDN fetching (local Tajawal only).
- Arabic UI, RTL; numbers/dates via `CultureInfo.InvariantCulture`; logical properties.
- No commits unless asked; no changes to `plans/orderhub-redesign-tasks.md`.
- Design language: page card `rounded-2xl`, inner cards `rounded-xl`,
  one primary CTA per page, `oh-btn` button system, 12px text floor.

## Verification (when implementation starts)
- `dotnet restore OrderHub.sln` → `dotnet build OrderHub.sln -c Release` →
  `dotnet test OrderHub.sln -c Release --no-build`.
- Manual flow: create invite → copy link → open in a private window →
  bad-format attempt → taken-username attempt → success → confirm the user shows
  غير مفعّل in Users → approve (Activate) → user logs in → link refuses reuse.
