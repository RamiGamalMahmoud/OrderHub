# Roles & Permissions — Implementation Specification

**Status:** specification complete; **implementation not started**
**Date:** 2026-11-08
**Source:** `plans/roles-and-permissions-plan.md` (decisions locked 2026-11-08)
**Audited against:** current `main`-line code on branch `feature/delivery-reassignment`
**Scope:** review + implementable specification only. This document does **not** authorize code changes; implementation proceeds after owner approval.

> **Authority.** Where this document and `roles-and-permissions-plan.md` disagree, this document wins. It records two owner decisions taken after the plan was written (DB-checked Higher Manager; preserve role on promotion) and corrects four technical claims in the plan that do not hold against the code.

---

## 1. Review verdict

### 1.1 What the plan gets right

Verified against the codebase:

| Claim | Verification |
|---|---|
| `PermissionLevel` has exactly `None/Read/Write` | [PermissionLevel.cs](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Domain/Enums/PermissionLevel.cs) lines 5–13 |
| 14 `PermissionFeature` values | `Orders … Settings`; no change needed |
| Levels are stored as text, so `Approve` needs no data migration | `RolePermissionConfiguration` uses `HasConversion<string>()` and `varchar(20)`; check constraint only bans `'None'` |
| The level guard silently denies any unlisted level | [AuthorizationService.cs:16](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Authorization/AuthorizationService.cs#L16) whitelists `Read or Write` |
| Policy names are generated per feature × level | [ServiceCollectionExtensions.cs:16-19](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Infrastructure/Authorization/ServiceCollectionExtensions.cs#L16-L19) |
| Adding columns `is_sealed` / `role_key` is purely additive | `roles` has only `id/name/created_at/modified_at`; FK from users is `Restrict` |
| Custom roles can be freely edited today | `Role` has no seal concept; handlers have no guard |
| User↔role is single-valued (no role accumulation) | `User.RoleId` is nullable; `AssignRole` replaces the reference |
| Admin + role already coexist in the model | `User.IsAdmin` and `User.RoleId` are independent; UI already keeps the role when admin is set |
| Permissions are DB-resolved per request | `UserPermissionReader` queries on every check; no permission cache exists |
| Existing pages are Admin-claim gated | Five pages, listed in §3 |
| Dev seed does not collide with preset names | `artifacts/seed/orderhub-dev-seed.sql` roles are «مدير المبيعات، محاسب مالي، مندوب توصيل، مشرف المخزون، خدمة العملاء، مدير المشتريات، مدقق النظام» |

### 1.2 Blocking corrections

These are not stylistic: each one causes a wrong or unreachable outcome if implemented as the plan is written.

| # | Plan text | Reality | Resolution in this spec |
|---|---|---|---|
| **B1** | R4: "when the Users edit handler flips `IsAdmin`, it re-issues the user's auth ticket" (auto-refresh claims) | **Impossible.** The edit handler runs inside the *editor's* request. `HttpContext.SignInAsync` writes a cookie to the editor's response, not the edited user's browser. A Blazor Server circuit cannot write `Set-Cookie` on another user's connection, and an already-authenticated circuit keeps its cached `AuthenticationState`. | Replace cookie-claim authority with **DB-checked Higher Manager** (§5.2). Remove the `Admin` role claim entirely. No forced logout. |
| **B2** | "SQLite tests use `EnsureCreated` → new columns/seed come free" | **False.** `EnsureCreated` builds the model from EF configuration; it never executes migration `Up` bodies or `migrationBuilder.Sql(...)`. Tests would see the columns but **zero preset roles**. | Preset catalog is code (`SealedRolePresets`); the seed is a provider-neutral SQL constant (`SealedRoleSeedSql.V1`) executed by both the migration and the tests (§7.3–§7.5). |
| **B3** | Phase 4 reads `IsSealed` / per-level counts from role DTOs | `RoleListItem(int Id, string Name, int PermissionCount)` and `GetRoleResponse(Id, Name, Permissions)` carry neither flag nor counts. The UI cannot render the badge, view-only mode, or the preview card. | Extend both DTOs and the projection (§6.1). |
| **B4** | Sealed roles are protected by hiding UI actions | Pagination/direct URL still reaches `/admin/roles/{id}`; `UpdateRoleCommandHandler` and `DeleteRoleCommandHandler` have no seal guard. Any HM can rename or edit a preset today. | Domain guards + handler guards + read-only page (§4.5, §6.2, §8.2). |
| **B5** | Admin + role coexistence is undefined | `UpdateUserCommandHandler` applies `RemoveRole()`/`AssignRole()` from the request independently of `IsAdmin`. | Owner decision: **preserve role** while Higher Manager is active; the role re-applies on downgrade. UI enforcement + preserved payload (§6.3, §8.6). |

### 1.3 Corrections that are not blockers but must be implemented

1. **Guard-line whitelist (R1 is real).** `AuthorizationService` line 16 denies `Approve` before reaching the switch. It must whitelist `Approve` and implement the hierarchy. This is the single most likely "everything silently denies" failure.
2. **Policy factory.** `AddPermissionAuthorization` enumerates only `Read, Write`. Enumerate every defined non-`None` level so future enum members cannot be forgotten.
3. **Sealed roles are not just "hidden buttons".** Handlers must reject them; the page must render read-only for direct URLs.
4. **Reserved names apply to rename, not only create.** `UpdateRoleCommandHandler` calls `Rename`; without a guard a custom role can be renamed to «مبيعات».
5. **`PermissionCount` semantics change.** It must include `Approve`, otherwise the roles list shows "0 صلاحيات" for the Manager preset.
6. **Users list labels.** With four sealed roles, «الأدوار المخصصة» currently counts *all* assigned roles. Split the stats into Higher Managers / sealed / custom.
7. **Name-collision skips are invisible.** A legacy DB with a custom role named «مبيعات» silently loses the `sales` preset. The migration cannot `RAISE` portably, so the runbook must verify and offer a repair path (§11).
8. **No bUnit / Web test project reference exists.** Authorization freshness and policy registration must be verified by browser walkthrough plus structurally-safe code, not by a unit test (§9.3).

---

## 2. Locked decisions

### 2.1 Owner decisions from the plan (unchanged)

1. Manager manages Users with **Read only** → superseded by decision 8 (Users = None).
2. `Approve` is added and means `None < Read < Write < Approve`.
3. Higher Manager keeps `IsAdmin`, re-presented as an account type.
4. Custom roles start from a **blank** matrix; no "create from template".
5. Accountant is deferred.
6. Existing data is preserved; no rewrites.
7. `plans/roles-and-permissions-study.md` is updated in Phase 6 of implementation.
8. Manager has **no system administration**: Users / Logs / Settings = None; all Administration pages remain Higher-Manager-only.
9. **No DB-level change may conflict with legacy data**; constraints are code-level where any conflict is conceivable; migrations are purely additive.
10. Deletes stay at Write; duplicate preset names are blocked; preview card is included.

### 2.2 New owner decisions from this review

| Topic | Decision | Consequence |
|---|---|---|
| **R4 / Admin claim staleness** | **DB-checked admin** | `IsAdmin` is read from `users.is_admin` per operation. Promotion and demotion are effective immediately without logout. The `Admin` cookie claim is removed. |
| **Higher Manager + role coexistence** | **Preserve role** | Selecting «مدير أعلى» disables the role picker but keeps the stored `RoleId`. Removing HM status restores the role's permissions with no re-assignment. |

### 2.3 Reading order for implementers

`§3` (facts) → `§4` (domain) → `§5` (authorization) → `§6` (application) → `§7` (persistence) → `§8` (UI) → `§9` (tests) → `§11` (deployment). `§14` is the change map.

---

## 3. Verified current state

| Area | File / line | Current behavior | Impact |
|---|---|---|---|
| Level guard | [AuthorizationService.cs:16](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Authorization/AuthorizationService.cs#L16) | `requiredLevel is not (Read or Write) → false` | Must include `Approve` (§5.1) |
| HM bypass | [AuthorizationService.cs:19-20](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Authorization/AuthorizationService.cs#L19-L20) | `currentUser.IsAdmin → true` | Becomes DB-fresh via the identity builder (§5.2) |
| Satisfaction switch | [AuthorizationService.cs:23-28](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Authorization/AuthorizationService.cs#L23-L28) | Read←Read\|Write; Write←Write | Add Approve (§5.1) |
| Permission reader | `Infrastructure/Features/Administration/UserPermissionReader.cs` | Projection through `user.Role.Permissions`; no `IsActive` filter | Unchanged (§13.4) |
| Policy factory | [ServiceCollectionExtensions.cs:16-19 (Web)](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Infrastructure/Authorization/ServiceCollectionExtensions.cs#L16-L19) | `{Read, Write}` only | Enumerate defined levels; add HM policy |
| Result handler | `PermissionAuthorizationResultHandler.cs` | Redirects/403 only when the policy has `PermissionRequirement` | Extend to `HigherManagerRequirement` |
| Command guard | `AuthorizationBehavior.cs` | Reads `[RequirePermission]` only | Add `[RequireHigherManager]` |
| Identity builder | [AuthenticatedUser.cs:30](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Services/AuthenticatedUser.cs#L30) | `principal.IsInRole("Admin")` | Replace with DB read |
| Login claims | [Auth.cs:41](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Controllers/Auth.cs#L41) | Adds `ClaimTypes.Role = "Admin"` | Delete the block |
| Admin page gates | `Roles/Index.razor:11`, `Roles/Editor/Editor.razor:16`, `Users/Index.razor:3`, `Users/Create.razor:14`, `Users/Edit.razor:13` | `[Authorize(Roles = "Admin")]` | Replace with `[HigherManagerAuthorize]` |
| Role model | [Role.cs](file:///C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Domain/Models/Role.cs) | Name + permissions; no flags, no guards | Add `IsSealed`, `RoleKey`, guards |
| Role permission validation | `RolePermission.cs` | Rejects only `None` | `Approve` passes unchanged |
| Role DTOs | `RoleListItem.cs`, `GetRoleResponse.cs` | No seal flags or per-level counts | Extend |
| Role reads | `RoleReadService.cs:31-34, 47-54` | Projects `Permissions.Count` | Extend projection |
| Role handlers | Create/Update/Delete handlers | Duplicate-name check only; no seal/reserved guard | Add guards |
| User update | `UpdateUserCommandHandler.cs` | Last-active-admin guard; assign/remove role from request | Preserve role via UI payload; no handler change |
| Roles list | `Roles/Index.razor:134-150` | Edit + delete icon buttons for every row | Sealed rows → view-only |
| Role editor | `Roles/Editor/Editor.razor:401-411` | 3 count buckets | Add Approve; read-only mode |
| Cards | `Roles/Editor/RolePermissionCards.razor:121-143, 215-275` | 3 segments, 3 bulk actions | 4 segments, 4 bulk actions |
| Editor state | `RoleEditorState.cs` | Builds inputs from non-`None` levels | Works unchanged with `Approve` |
| Users list | `Users/Index.razor:209-220, 72-87` | Plain «بدون دور»; stat «مديرو النظام» / «الأدوار المخصصة» | Warning chip; relabel/split |
| User create/edit | `Users/Create.razor:168-239`, `Users/Edit.razor` | Flat role select; IsAdmin switch; role preserved textually | Grouped select, HM disables picker, preview card, wording |
| Nav | `NavMenu.razor:294` | `user.IsInRole("Admin")` | `PermissionAccess.IsHigherManagerAsync()` |
| Layout | `MainLayout.razor:172` | `user.IsInRole("Admin")`; chip «مدير النظام» | HM check; «مدير أعلى» |
| Sealed guard surface | `Role.Rename`, `Role.SetPermission` | No guard | Throw when sealed |
| Tests | `OrderHub.Tests` | No authorization/seed tests; no Web reference | New suites (§9) |
| Latest migration | `20261107120704_AllowDeliveryStepCancellationWithoutReplacement` | — | New migration must sort after it |

---

## 4. Domain specification

### 4.1 `PermissionLevel`

```csharp
public enum PermissionLevel
{
    [Description("لا شيء")]
    None,

    [Description("قراءة")]
    Read,

    [Description("كتابة")]
    Write,

    [Description("اعتماد")]
    Approve
}
```

- Ordinal order matters: `None < Read < Write < Approve` is satisfied by declaration order, but **do not** rely on numeric comparisons in authorization logic; use the explicit switch (§5.1).
- No other enum changes.

### 4.2 `Role`

Add to `OrderHub.Domain/Models/Role.cs`:

```csharp
public bool IsSealed { get; private set; }
public string? RoleKey { get; private set; }

internal static Role CreateSealed(string roleKey, string name)
{
    if (string.IsNullOrWhiteSpace(roleKey))
        throw new ArgumentException("Role key is required.", nameof(roleKey));

    var role = new Role(name)
    {
        IsSealed = true,
        RoleKey = roleKey
    };
    return role;
}

private void EnsureNotSealed()
{
    if (IsSealed)
        throw new InvalidOperationException("Role is sealed.");
}
```

- `Rename` begins with `EnsureNotSealed();`.
- `SetPermission` begins with `EnsureNotSealed();`.
- The private setter is enough for EF materialization; `CreateSealed` exists so domain tests can build sealed roles without SQL.
- `RoleKey` is an identity token (`sales`, `purchasing`, `data-entry`, `manager`) and is never shown as a user-facing name.

### 4.3 `RolePermission`

No change. `ValidateLevel` rejects only `None`, so `Approve` is accepted. The DB check constraint `level <> 'None'` also accepts it. Do **not** add a `level IN (...)` constraint (decision 9).

### 4.4 Preset catalog

New file `OrderHub.Domain/Authorization/SealedRolePresets.cs`:

```csharp
public sealed record RolePermissionPreset(
    PermissionFeature Feature,
    PermissionLevel Level);

public sealed record SealedRolePreset(
    string RoleKey,
    string Name,
    IReadOnlyList<RolePermissionPreset> Permissions);

public static class SealedRolePresets
{
    public const string SalesKey = "sales";
    public const string PurchasingKey = "purchasing";
    public const string DataEntryKey = "data-entry";
    public const string ManagerKey = "manager";

    public static IReadOnlyList<SealedRolePreset> All { get; } = [ /* §4.4.1 */ ];

    public static bool IsReservedName(string? name)
    {
        string candidate = name?.Trim() ?? string.Empty;
        return All.Any(preset =>
            string.Equals(preset.Name, candidate, StringComparison.OrdinalIgnoreCase));
    }

    public static SealedRolePreset? FindByKey(string? key) =>
        All.FirstOrDefault(preset =>
            string.Equals(preset.RoleKey, key, StringComparison.Ordinal));
}
```

This catalog is the single code definition of the presets. It drives the reserved-name check and the drift test. It **must not** generate the migration SQL at runtime (migrations must stay frozen); the SQL is a separate constant (§7.3) and a test asserts the two agree.

#### 4.4.1 Authoritative matrix

Arabic names in the UI, keys in code/DB. `—` = `None` = no row.

| RoleKey | Name | Orders | Documents | Products | Categories | Properties | Clients | Suppliers | Deliverymen | ShippingCarriers | Cities | PaymentMethods | Logs | Users | Settings | Count |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `sales` | مبيعات | Write | Read | Read | Read | — | Write | — | Read | Read | Read | Read | — | — | — | **9** |
| `purchasing` | مشتريات | Read | Read | Read | — | — | — | Write | — | — | Read | Read | — | — | — | **6** |
| `data-entry` | إدخال البيانات | — | — | Write | Write | Write | — | — | — | — | Write | Read | — | — | — | **5** |
| `manager` | مدير | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | Approve | — | — | — | **11** |

Higher Manager is **not** a role row; it is `users.is_admin = true` and bypasses every check.

Semantic consequence (accepted, recorded): `Approve` satisfies `Write`, and deletes are Write-gated, so the Manager preset can delete operational records (R3: deletes stay at Write in this batch).

### 4.5 Sealed guards and user-facing errors

| Location | Guard | Result |
|---|---|---|
| `Role.Rename` | `EnsureNotSealed()` | `InvalidOperationException("Role is sealed.")` (safety net; should be unreachable through the app) |
| `Role.SetPermission` | `EnsureNotSealed()` | same |
| `UpdateRoleCommandHandler` | `if (role.IsSealed) return Result.Failure(...)` | «لا يمكن تعديل دور جاهز؛ الأدوار الجاهزة محمية من التعديل.» |
| `DeleteRoleCommandHandler` | `if (role.IsSealed) return Result.Failure(...)` | «لا يمكن حذف دور جاهز؛ الأدوار الجاهزة جزء من النظام.» |
| `CreateRoleCommandHandler` | `SealedRolePresets.IsReservedName(input.Name)` | «هذا الاسم محجوز لأحد الأدوار الجاهزة.» |
| `UpdateRoleCommandHandler` | reserved-name check **before** `Rename` | same message |

Ordering inside `DeleteRoleCommandHandler`: not-found → sealed → assigned-to-users. This makes the sealed message deterministic.

---

## 5. Authorization specification

### 5.1 Level satisfaction

`IAuthorizationService.IsAllowedAsync` becomes:

```csharp
if (!currentUser.IsAuthenticated || currentUser.Id is not > 0)
    return false;

if (!Enum.IsDefined(feature) ||
    requiredLevel is not (PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve))
    return false;

if (currentUser.IsAdmin)
    return true;

var actual = await reader.GetPermissionLevelAsync(currentUser.Id.Value, feature, cancellationToken);

return requiredLevel switch
{
    PermissionLevel.Read    => actual is PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve,
    PermissionLevel.Write   => actual is PermissionLevel.Write or PermissionLevel.Approve,
    PermissionLevel.Approve => actual is PermissionLevel.Approve,
    _                       => false
};
```

| Requirement ↓ / Actual → | None | Read | Write | Approve | HM (`IsAdmin`) |
|---|---|---|---|---|---|
| Read | deny | allow | allow | allow | allow |
| Write | deny | deny | allow | allow | allow |
| Approve | deny | deny | deny | allow | allow |
| None / undefined level | deny | deny | deny | deny | **deny** (guard runs before HM bypass) |

The `None` requirement stays a programming error and must deny even for HM.

### 5.2 DB-checked Higher Manager

#### 5.2.1 Application contract

New `OrderHub.Application/Authorization/IUserAdminReader.cs`:

```csharp
public interface IUserAdminReader
{
    Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default);
}
```

New `OrderHub.Application/Authorization/CheckHigherManagerQuery.cs`:

```csharp
public sealed record CheckHigherManagerQuery : IRequest<bool>;
```

- No `[RequirePermission]` and no `[RequireHigherManager]` on it: it is the primitive used by the pipeline itself.
- Handler returns `IAuthorizationService.IsHigherManagerAsync(cancellationToken)`.

`IAuthorizationService` gains:

```csharp
Task<bool> IsHigherManagerAsync(CancellationToken cancellationToken = default);
```

Implementation:

```csharp
public Task<bool> IsHigherManagerAsync(CancellationToken cancellationToken = default) =>
    Task.FromResult(currentUser.IsAuthenticated && currentUser.Id is > 0 && currentUser.IsAdmin);
```

`IsAdmin` is DB-fresh because of §5.2.2. No extra query is needed here.

#### 5.2.2 Identity builder

`AuthenticatedUser` injects `IUserAdminReader` and replaces the claim read:

```csharp
int? userId = int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
    && id > 0 ? id : null;

bool isAdmin = userId is > 0
    && await userAdminReader.IsAdminAsync(userId.Value, cancellationToken);

return new CurrentUserIdentity(
    userId,
    principal.FindFirstValue(ClaimTypes.Name),
    principal.FindFirstValue("DisplayName"),
    true,
    isAdmin);
```

- `GetIdentityAsync` is called once per `IRequestExecutor` operation, so promotion/demotion is observed on the user's **next operation** without a new login and without touching their cookie.
- The query is a primary-key projection: one small read per operation. This is the accepted cost of correctness; do not cache it (caching reintroduces staleness).
- Infrastructure implementation, mirroring `UserPermissionReader`:

```csharp
internal sealed class UserAdminReader(AppDbContextFactory factory) : IUserAdminReader
{
    public async Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var context = factory.CreateDbContext();
        return await context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.IsAdmin)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

Register in `OrderHub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, next to `IUserPermissionReader`:

```csharp
services.AddScoped<IUserAdminReader, UserAdminReader>();
```

#### 5.2.3 Requirement, attribute, policy

New `OrderHub.Web/Infrastructure/Authorization/HigherManagerRequirement.cs`:

```csharp
internal sealed record HigherManagerRequirement : IAuthorizationRequirement;

internal sealed class HigherManagerRequirementHandler(PermissionAccess access)
    : AuthorizationHandler<HigherManagerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HigherManagerRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            await access.IsHigherManagerAsync())
            context.Succeed(requirement);
    }
}
```

New `HigherManagerAuthorizeAttribute`:

```csharp
public sealed class HigherManagerAuthorizeAttribute : AuthorizeAttribute
{
    public const string PolicyName = "HigherManager";

    public HigherManagerAuthorizeAttribute() => Policy = PolicyName;
}
```

`PermissionAccess` gains:

```csharp
public Task<bool> IsHigherManagerAsync(CancellationToken cancellationToken = default) =>
    executor.ExecuteAsync(new CheckHigherManagerQuery(), cancellationToken);
```

Policy registration in `AddPermissionAuthorization`:

```csharp
services.AddScoped<IAuthorizationHandler, HigherManagerRequirementHandler>();

services.AddAuthorization(options =>
{
    foreach (var feature in Enum.GetValues<PermissionFeature>())
    foreach (var level in Enum.GetValues<PermissionLevel>())
    {
        if (level == PermissionLevel.None)
            continue;

        options.AddPolicy(PermissionAuthorizeAttribute.PolicyName(feature, level), policy =>
            policy.RequireAuthenticatedUser()
                  .AddRequirements(new PermissionRequirement(feature, level)));
    }

    options.AddPolicy(HigherManagerAuthorizeAttribute.PolicyName, policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new HigherManagerRequirement()));
});
```

Deriving the enumeration from the enum prevents R11 from recurring when a future level is added.

#### 5.2.4 Result handler

`PermissionAuthorizationResultHandler` must treat an HM denial like a permission denial. Replace the condition with:

```csharp
if (result.Forbidden &&
    policy.Requirements.Any(requirement =>
        requirement is PermissionRequirement or HigherManagerRequirement))
{ /* existing redirect / 403 body */ }
```

**Why this matters:** a direct HTTP request to `/admin/users` that fails the HM policy must go to `/access-denied`, not return a bare 403 page.

#### 5.2.5 Command-level guard

New `OrderHub.Application/Authorization/RequireHigherManagerAttribute.cs`:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireHigherManagerAttribute : Attribute;
```

`AuthorizationBehavior` checks it **after** the permission requirement:

```csharp
var permission = request.GetType().GetCustomAttribute<RequirePermissionAttribute>();
if (permission is not null &&
    !await authorization.IsAllowedAsync(permission.Feature, permission.RequiredLevel, cancellationToken))
    throw new AuthorizationDeniedException();

if (request.GetType().GetCustomAttribute<RequireHigherManagerAttribute>() is not null &&
    !await authorization.IsHigherManagerAsync(cancellationToken))
    throw new AuthorizationDeniedException();
```

Apply `[RequireHigherManager]` to **all** administration requests:

| Feature | Types |
|---|---|
| Roles | `CreateRoleCommand`, `UpdateRoleCommand`, `DeleteRoleCommand`, `GetRolesQuery`, `GetRoleQuery` |
| Users | `CreateUserCommand`, `UpdateUserCommand`, `ActivateUserCommand`, `DeactivateUserCommand`, `GetUsersQuery`, `GetUserQuery` |

Verified callers of these reads are all HM-gated pages (`Roles/Index`, `Roles/Editor`, `Users/Index`, `Users/Create`, `Users/Edit`), so no legitimate non-HM path breaks.

Do **not** add the attribute to `CreateAdminCommand` (bootstrap path; currently unreferenced).

**Why command-level, not page-level only:** a custom role granted `Users: Write` currently passes `[RequirePermission]` and could invoke these commands from any future UI or service. System administration is HM-only by decision 8; enforce it where the data changes.

#### 5.2.6 Claim removal and complete sweep

1. Delete the `if (user.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));` block in `Controllers/Auth.cs`.
2. Replace the five `[Authorize(Roles = "Admin")]` page attributes with `[HigherManagerAuthorize]`.
3. Replace `user.IsInRole("Admin")` in:
   - `MainLayout.razor:172` → `_isAdmin = await PermissionAccess.IsHigherManagerAsync();`
   - `NavMenu.razor:294` → `_isAdmin = await PermissionAccess.IsHigherManagerAsync();` (drop the now-unused principal read; keep `AuthenticationStateTask` only if something else uses it — it does not).
4. Remove `principal.IsInRole("Admin")` from `AuthenticatedUser` (§5.2.2).
5. `grep -rn 'IsInRole("Admin")'` and `grep -rn 'Roles = "Admin"'` must return **zero** results after the change.

Existing cookies that still contain an `Admin` role claim are harmless: nothing reads the claim any more. No forced logout, no ticket re-issue.

**Behavioral note:** `AuthorizeRouteView` renders the `<NotAuthorized>` warning on in-circuit navigation; the HTTP result handler redirects full-page requests. This split already exists for permission policies and is unchanged.

---

## 6. Application specification

### 6.1 DTOs

`RoleListItem`:

```csharp
public sealed record RoleListItem(
    int Id,
    string Name,
    bool IsSealed,
    string? RoleKey,
    int ReadCount,
    int WriteCount,
    int ApproveCount)
{
    public int PermissionCount => ReadCount + WriteCount + ApproveCount;
}
```

- Keeping `PermissionCount` as a computed member means the existing `Roles/Index` row (`role.PermissionCount > 0`) compiles and now counts `Approve`.
- Do not project the computed property in EF; `RoleReadService` supplies the three counts.

`GetRoleResponse`:

```csharp
public sealed record GetRoleResponse(
    int Id,
    string Name,
    bool IsSealed,
    string? RoleKey,
    IReadOnlyList<RolePermissionItem> Permissions);
```

`RolePermissionItem`, `UserListItem`, `GetUserResponse`: unchanged.

### 6.2 Role commands and queries

| Handler | Change |
|---|---|
| `CreateRoleCommandHandler` | After trimming: `if (SealedRolePresets.IsReservedName(input.Name)) return Result<int>.Failure("هذا الاسم محجوز لأحد الأدوار الجاهزة.");` **before** `NameExistsAsync` |
| `UpdateRoleCommandHandler` | 1) load → null check; 2) **sealed check** → failure; 3) reserved-name check → failure; 4) duplicate-name check; 5) rename + permission diffs |
| `DeleteRoleCommandHandler` | load → null; **sealed check**; assigned-to-users check; remove |
| `GetRolesQuery` / `GetRoleQuery` | Add `[RequireHigherManager]`; otherwise unchanged |

`RoleEditorState.BuildPermissionInputs()` already filters `None`, so `Approve` flows through with no change.

### 6.3 User commands

- `CreateUserCommandHandler`: unchanged. `IsAdmin + RoleId` remains valid (HM with a preserved role).
- `UpdateUserCommandHandler`: **unchanged**. The UI keeps sending the existing `RoleId` while HM is on, so `AssignRole` preserves it. Do not add handler-side "preserve" logic: if the caller explicitly clears the role, the role must be cleared.
- `ActivateUserCommand` / `DeactivateUserCommand`: add `[RequireHigherManager]`.
- Last-active-admin guard (`لا يمكن إلغاء صلاحية المدير أو تعطيل الحساب لأنه المدير النشط الوحيد في النظام.`) stays exactly as-is and must keep working with the new "Higher Manager" wording.

### 6.4 Read services

`RoleReadService.GetAsync`:

```csharp
.Select(role => new RoleListItem(
    role.Id,
    role.Name,
    role.IsSealed,
    role.RoleKey,
    role.Permissions.Count(permission => permission.Level == PermissionLevel.Read),
    role.Permissions.Count(permission => permission.Level == PermissionLevel.Write),
    role.Permissions.Count(permission => permission.Level == PermissionLevel.Approve)))
```

`GetRoleDetailsAsync` adds `role.IsSealed, role.RoleKey` to the projection.

Ordering: sealed roles first, then by name, so presets are visually stable:
`.OrderByDescending(role => role.IsSealed).ThenBy(role => role.Name)`.

`UserReadService` unchanged.

---

## 7. Persistence, seed, and migration

### 7.1 EF configuration

`RoleConfiguration` adds:

```csharp
builder.Property(x => x.IsSealed)
    .IsRequired()
    .HasDefaultValue(false);

builder.Property(x => x.RoleKey)
    .HasColumnType("varchar(100)")
    .HasMaxLength(100);

builder.HasIndex(x => x.RoleKey)
    .IsUnique();
```

- Unique index on a nullable column is safe on PostgreSQL and SQLite: both ignore `NULL` duplicates, so all legacy custom roles (`role_key = NULL`) coexist.
- No new check constraint on `level` (decision 9).

### 7.2 Migration

Name: `20261108100000_SeedSealedRoles` (must sort after `20261107120704`; adjust the timestamp to the actual creation time, keep the `_SeedSealedRoles` suffix).

`Up`:

1. `AddColumn<bool>("is_sealed", "roles", type: "boolean", nullable: false, defaultValue: false)`
2. `AddColumn<string>("role_key", "roles", type: "varchar(100)", maxLength: 100, nullable: true)`
3. `CreateIndex("ix_roles_role_key", "roles", "role_key", unique: true)`
4. `migrationBuilder.Sql(SealedRoleSeedSql.V1)`

`Down`:

1. `DropIndex("ix_roles_role_key", "roles")`
2. `DropColumn("role_key", "roles")`
3. `DropColumn("is_sealed", "roles")`

**Do not delete seeded role rows in `Down`.** They may already be assigned to users; deleting them cascades role assignments and destroys configuration. After a rollback they remain as ordinary editable roles, which is the intended safe degradation. Document this in the migration XML comment.

The `.Designer.cs` and `AppDbContextModelSnapshot.cs` must be regenerated by `dotnet ef`; do not hand-edit them.

### 7.3 Seed SQL constant

New `OrderHub.Infrastructure/Migrations/SealedRoleSeedSql.cs`:

```csharp
namespace OrderHub.Infrastructure.Migrations;

internal static class SealedRoleSeedSql
{
    // Frozen: provider-neutral (PostgreSQL + SQLite), idempotent, insert-only.
    public const string V1 = """
        INSERT INTO roles (name, is_sealed, role_key, created_at)
        SELECT candidates.name, TRUE, candidates.role_key, CURRENT_TIMESTAMP
        FROM (
            SELECT 'مبيعات' AS name, 'sales' AS role_key
            UNION ALL SELECT 'مشتريات', 'purchasing'
            UNION ALL SELECT 'إدخال البيانات', 'data-entry'
            UNION ALL SELECT 'مدير', 'manager'
        ) AS candidates
        WHERE NOT EXISTS (SELECT 1 FROM roles existing WHERE existing.role_key = candidates.role_key)
          AND NOT EXISTS (SELECT 1 FROM roles existing WHERE existing.name = candidates.name);

        INSERT INTO role_permissions (role_id, feature, level)
        SELECT role_row.id, cells.feature, cells.level
        FROM roles role_row
        INNER JOIN (
            SELECT 'sales' AS role_key, 'Orders' AS feature, 'Write' AS level
            UNION ALL SELECT 'sales', 'Documents', 'Read'
            UNION ALL SELECT 'sales', 'Products', 'Read'
            UNION ALL SELECT 'sales', 'Categories', 'Read'
            UNION ALL SELECT 'sales', 'Clients', 'Write'
            UNION ALL SELECT 'sales', 'Deliverymen', 'Read'
            UNION ALL SELECT 'sales', 'ShippingCarriers', 'Read'
            UNION ALL SELECT 'sales', 'Cities', 'Read'
            UNION ALL SELECT 'sales', 'PaymentMethods', 'Read'
            UNION ALL SELECT 'purchasing', 'Orders', 'Read'
            UNION ALL SELECT 'purchasing', 'Documents', 'Read'
            UNION ALL SELECT 'purchasing', 'Products', 'Read'
            UNION ALL SELECT 'purchasing', 'Suppliers', 'Write'
            UNION ALL SELECT 'purchasing', 'Cities', 'Read'
            UNION ALL SELECT 'purchasing', 'PaymentMethods', 'Read'
            UNION ALL SELECT 'data-entry', 'Products', 'Write'
            UNION ALL SELECT 'data-entry', 'Categories', 'Write'
            UNION ALL SELECT 'data-entry', 'Properties', 'Write'
            UNION ALL SELECT 'data-entry', 'Cities', 'Write'
            UNION ALL SELECT 'data-entry', 'PaymentMethods', 'Read'
            UNION ALL SELECT 'manager', 'Orders', 'Approve'
            UNION ALL SELECT 'manager', 'Documents', 'Approve'
            UNION ALL SELECT 'manager', 'Products', 'Approve'
            UNION ALL SELECT 'manager', 'Categories', 'Approve'
            UNION ALL SELECT 'manager', 'Properties', 'Approve'
            UNION ALL SELECT 'manager', 'Clients', 'Approve'
            UNION ALL SELECT 'manager', 'Suppliers', 'Approve'
            UNION ALL SELECT 'manager', 'Deliverymen', 'Approve'
            UNION ALL SELECT 'manager', 'ShippingCarriers', 'Approve'
            UNION ALL SELECT 'manager', 'Cities', 'Approve'
            UNION ALL SELECT 'manager', 'PaymentMethods', 'Approve'
        ) AS cells ON cells.role_key = role_row.role_key
        WHERE NOT EXISTS (
            SELECT 1 FROM role_permissions existing
            WHERE existing.role_id = role_row.id
              AND existing.feature = cells.feature
        );
        """;
}
```

Design constraints baked into the SQL:

- **No explicit IDs** — identity allocates, so sequences never desync (R5).
- **No `UPDATE`, no `DELETE`, no renames** — purely additive (decision 9).
- **Role dedupe by `role_key`; name collision skips the preset** rather than failing the unique index.
- **Permission cells dedupe by `(role_id, feature)`** — re-running re-adds a manually deleted cell but never changes a stored level.
- `TRUE` and `CURRENT_TIMESTAMP` are supported by PostgreSQL and SQLite (3.23+).
- `INSERT … SELECT … FROM (UNION ALL …)` avoids provider-specific `VALUES`/`ON CONFLICT` syntax.
- Enum `feature`/`level` values are stored as names, matching `HasConversion<string>()`.

**Drift rule:** a future change to a preset requires a **new migration** (and owner approval). Because the seed never updates levels, that migration may contain targeted `UPDATE` statements scoped to `role_key = '…'`; this is the only sanctioned exception to "no row rewrites", and it must be explicitly called out in its comment.

### 7.4 Name-collision behavior

If a legacy custom role already owns one of the four names:

- The role insert is skipped (both `NOT EXISTS` predicates fail).
- Its permission cells insert nothing because no row with that `role_key` exists.
- The custom role is untouched: still `is_sealed = false`, still editable.
- The app shows three presets; reserved-name guards prevent creating the fourth name later.

This is deliberate. The repair path is manual and documented in §11.

### 7.5 Test seeding

Because `EnsureCreated` skips migration SQL (B2), every test that needs presets must execute `SealedRoleSeedSql.V1` explicitly against its SQLite connection:

```csharp
using var connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite(connection)
    .UseSnakeCaseNamingConvention()
    .Options;

using var context = new AppDbContext(options);
context.Database.EnsureCreated();
context.Database.ExecuteSqlRaw(SealedRoleSeedSql.V1);
```

`SealedRoleSeedSql` is `internal`; `OrderHub.Infrastructure.csproj` already grants `InternalsVisibleTo OrderHub.Tests`.

The identical SQL must run on PostgreSQL; this is verified by the rehearsal in §11, not by CI (CI has no PostgreSQL instance).

### 7.6 Deployment order

1. Apply the migration to the target database.
2. Deploy the new application build.

The migration is forward-compatible with the old app (it only adds columns and new rows). The new app is **not** compatible with the old schema (`RoleReadService` projects the new columns), so never deploy first.

No auto-migration on startup. No runtime seeding.

---

## 8. Web UI specification

All UI text is Arabic RTL. No new JavaScript. MudBlazor only for functional/data components; layout stays HTML + Tailwind + app CSS. Informative text floor is 12px (`text-xs`); existing 11px labels in the role editor predate the rule — do not add new ones below 12px.

### 8.1 Roles list `/admin/roles`

- Page attribute: `@attribute [HigherManagerAuthorize]` (keep `[PermissionAuthorize(Users, Read)]`).
- Sealed rows:
  - Show a badge «دور جاهز» next to the name (amber/neutral, non-interactive).
  - Replace edit/delete icon buttons with a single «عرض» action that navigates to the editor (read-only mode).
  - No delete confirmation path is reachable.
- Custom rows: keep «تعديل» and «حذف».
- `PermissionCount` now includes `Approve`; the existing text helper is unchanged.
- Sorting: sealed first, then custom by name.

### 8.2 Role editor `/admin/roles/{id}`

- Page attribute: `@attribute [HigherManagerAuthorize]`.
- Load `GetRoleResponse`; set `_isSealed = role.IsSealed`.
- When sealed:
  - Banner: «دور جاهز — للعرض فقط. الأدوار الجاهزة تُدار من النظام ولا يمكن تعديلها.»
  - Name input: disabled.
  - `RolePermissionCards Disabled="true"`.
  - Save button: hidden (not merely disabled).
  - Page description: «عرض صلاحيات الدور الجاهز».
  - No create-role shortcut.
- When custom: unchanged behavior plus the four-level matrix.
- Direct URL to a sealed role must render this read-only view, never a form that can submit.

### 8.3 `RolePermissionCards`

- Segment grid: `grid-cols-3` → `grid-cols-4`; add «اعتماد».
- Bulk actions: add «الكل: اعتماد» (`SetAllLevelsAsync(PermissionLevel.Approve)`); keep reset/read/write.
- Tone mapping (use consistently for card border, badge, active segment):
  - `None` — slate
  - `Read` — blue
  - `Write` — indigo (replaces the current red active segment; the red segment contradicts the indigo badge and reads as "danger")
  - `Approve` — emerald
- Add a compact legend line at the top: «قراءة < كتابة < اعتماد — كل مستوى يمنح المستوى الأدنى منه.» at ≥12px.
- Header counters: «N قسماً · قراءة X · كتابة Y · اعتماد Z» computed from the supplied list.
- Search, empty state, and disabled behavior are unchanged.
- `RoleEditorDialog` consumes this component; no separate change is required beyond verifying it renders four segments within the dialog width.

### 8.4 Role editor summary

`Editor.razor`:

- Add `ApprovePermissionCount`.
- `GrantedPermissionCount = Read + Write + Approve`.
- Summary chips become four; keep Latin digits via the existing `FmtCount`.
- `SaveButtonText` unchanged for custom roles.

### 8.5 Users list `/admin/users`

- Page attribute: `[HigherManagerAuthorize]`.
- Role column:
  - Assigned role → keep the blue chip; append a subtle «جاهز» marker when the role is sealed. To do this without N+1 reads, extend `UserListItem` with `bool IsRoleSealed` and project it in `UserReadService` (`user.Role != null && user.Role.IsSealed`). This is the only additional user DTO field in this batch.
  - No role and not HM → amber warning chip «بدون دور — لا صلاحيات» replacing the plain gray text.
  - HM → the type column already shows the admin badge; the role column may still show the preserved role (this is the visible proof of the "preserve" behavior).
- Type column and stats wording:
  - «مدير النظام» → «مدير أعلى».
  - Stat card 1: «مستخدمون نشطون» (unchanged).
  - Stat card 2: «مديرون أعلى» / «حسابات بوصول كامل».
  - Stat card 3: split into two lines or two cards: «أدوار جاهزة» (distinct sealed roles assigned) and «أدوار مخصصة» (distinct custom roles assigned). Use `IsRoleSealed`; do not count role definitions, only assignments, matching the current stat's intent.
- Search/filter by role continues to work (it uses `RoleId`).

### 8.6 User create / edit

Both pages share the same access section.

- Page attributes: `[HigherManagerAuthorize]`.
- Role select:
  - Grouped list:
    ```razor
    <MudListSubheader>أدوار جاهزة</MudListSubheader>
    @foreach (var role in _roles.Where(r => r.IsSealed)) { … }
    <MudListSubheader>أدوار مخصصة</MudListSubheader>
    @foreach (var role in _roles.Where(r => !r.IsSealed)) { … }
    ```
  - `MudListSubheader` exists in MudBlazor 9.11. Omit the custom subheader when the group is empty.
  - Keep `Clearable="true"` for the "no role" option.
- HM switch:
  - Label «مدير أعلى».
  - `Disabled="@(_saving || _model.IsAdmin)"` is **not** applied to the switch; apply the disabled state to the **role select** when `_model.IsAdmin`.
  - Toggling HM on must **not** clear `_model.RoleId`.
  - Warning: «مدير أعلى يمتلك وصولاً كاملاً لكل أقسام النظام ولا يعتمد على صلاحيات الدور.», plus when a role is stored: «سيظل الدور محفوظًا ويُطبَّق تلقائيًا إذا أُزيلت صفة مدير أعلى.»
- No-role warning (non-HM, no role): keep, reworded «لن يحصل هذا المستخدم على أي صلاحيات حتى يتم تعيين دور له.»
- Preview card beside the role select (read-only):
  - Selected role → name, «جاهز/مخصص», and counts «قراءة X · كتابة Y · اعتماد Z» plus total.
  - HM → «وصول كامل — يتجاوز جميع الصلاحيات», no counts.
  - No role → «لا توجد صلاحيات».
  - Display only; no copy/template action.
- `Create.razor`'s inline «إنشاء دور جديد…» dialog is unchanged (create-only, blank matrix).
- Buttons and busy states follow the existing `oh-btn` system; no new primary CTA is added.

### 8.7 Navigation and layout

- `NavMenu.razor`: `_isAdmin = await PermissionAccess.IsHigherManagerAsync();` inside the existing identity branch. `AdminOnly` items (Users, Roles) filter on this flag exactly as before.
- `MainLayout.razor`: same replacement; chip label «مدير أعلى» instead of «مدير النظام».
- No other wording changes.

### 8.8 Design-system constraints

- One primary CTA per page (roles list keeps «دور جديد»; the editor keeps one save button).
- Sealed banner and no-role warning use existing alert/callout patterns (blue informational, amber warning).
- No new CSS files and no JS.

---

## 9. Tests

Baseline: the existing suite is green. New tests use the established SQLite `EnsureCreated` + `UseSnakeCaseNamingConvention` pattern.

### 9.1 Domain (`OrderHub.Tests`)

| # | Test | Assert |
|---|---|---|
| 1 | `Role_Sealed_Rename_Throws` | `InvalidOperationException`, name unchanged |
| 2 | `Role_Sealed_SetPermission_Throws` | exception; permission collection unchanged |
| 3 | `Role_CreateSealed_SetsKeyAndFlag` | `IsSealed == true`, `RoleKey == "sales"`, name trimmed |
| 4 | `Role_SetPermission_Approve_Persists` | level stored as `Approve` |
| 5 | `RolePermission_None_StillThrows` | unchanged |
| 6 | `SealedRolePresets_Matrix_MatchesApprovedMatrix` | exact 4 presets; counts 9/6/5/11; every cell matches §4.4.1 |
| 7 | `SealedRolePresets_ReservedNames_AreTrimmedAndCaseInsensitive` | `"  مبيعات "` reserved; `"مبيعات فرع"` not |

### 9.2 Application (`OrderHub.Tests`)

| # | Test | Assert |
|---|---|---|
| 8 | `Authorization_Read_IsSatisfiedBy_ReadWriteApprove` | Theory over 3 actual levels |
| 9 | `Authorization_Write_IsSatisfiedBy_WriteApprove` | Theory; Read denied |
| 10 | `Authorization_Approve_IsSatisfiedOnlyByApprove` | Theory; Read/Write denied |
| 11 | `Authorization_NoneRequirement_DeniesEvenForAdmin` | false |
| 12 | `Authorization_Unauthenticated_Denies` | false |
| 13 | `Authorization_Admin_BypassesWithoutReaderCall` | fake reader throws if called |
| 14 | `CreateRole_ReservedPresetName_ReturnsArabicFailure` | Theory over 4 names + whitespace variant |
| 15 | `UpdateRole_Sealed_ReturnsArabicFailure` | failure; DB unchanged |
| 16 | `DeleteRole_Sealed_ReturnsArabicFailure` | failure; row still present |
| 17 | `UpdateRole_ApproveLevel_Persists` | reload shows Approve |
| 18 | `UpdateUser_PromoteToHigherManager_KeepsAssignedRole` | `RoleId` preserved |
| 19 | `UpdateUser_LastActiveHigherManager_GuardStillBlocks` | existing message |
| 20 | `RoleEditorState_BuildPermissionInputs_KeepsApprove` | 11 inputs for manager state |

Test doubles: use a `FakeCurrentUser` + `FakeUserPermissionReader` (in-memory dictionaries), matching the existing handler-test style. No mocking framework is referenced in the test project; hand-written fakes only.

### 9.3 Seed / persistence (`OrderHub.Tests`)

| # | Test | Assert |
|---|---|---|
| 21 | `Seed_OnEmptyDatabase_CreatesFourSealedRoles` | 4 roles, `is_sealed = true`, unique keys, 31 permission rows |
| 22 | `Seed_IsIdempotent` | execute twice; same counts; no exception |
| 23 | `Seed_SkipsPreset_WhenNameCollides` | pre-create custom «مبيعات»; seed; no `sales` key; custom role still `IsSealed == false`; its permissions intact |
| 24 | `Seed_ReAddsMissingPermissionCell` | delete one manager cell; re-run; cell restored; other cells unchanged |
| 25 | `Seed_MatchesCatalog` | DB rows for each key equal `SealedRolePresets.FindByKey(key)` exactly (guards SQL/catalog drift) |
| 26 | `RoleReadService_ProjectsSealFlagsAndPerLevelCounts` | manager → 0/0/11; `PermissionCount == 11` |
| 27 | `UserReadService_ProjectsIsRoleSealed` | user on manager preset → true; custom role → false |

### 9.4 Behavior that cannot be unit-tested here

The test project has **no reference to `OrderHub.Web`** and no bUnit. Do not add a Web project reference just for policy registration; instead:

- The policy factory derives from `Enum.GetValues<PermissionLevel>()` (§5.2.3), which structurally cannot omit a level.
- `grep` checks in §5.2.6 are part of the acceptance checklist.
- The 10-step browser walkthrough in §10 verifies freshness and gates end-to-end.

---

## 10. Manual verification checklist

Run against a local PostgreSQL database with at least two browser profiles.

1. **HM visibility.** Log in as HM: nav shows Users/Roles; chip reads «مدير أعلى».
2. **Immediate promotion.** In profile A (normal user), copy the session; promote that user to HM in profile B. Profile A's next navigation shows admin items without re-login.
3. **Immediate demotion.** Demote in profile B. Profile A's admin items disappear on the next operation; direct navigation to `/admin/users` shows the access-denied page.
4. **Sealed list.** Roles list shows four «دور جاهز» rows with «عرض» only.
5. **Sealed direct URL.** `/admin/roles/{managerId}` renders the read-only banner, disabled matrix, no save button; POST is unreachable.
6. **Reserved names.** Creating or renaming a role to «مبيعات» (and «مبيعات » with spaces) fails with the Arabic message.
7. **Four-level matrix.** Approve survives save/reload for the manager preset only; a custom role can also be granted Approve.
8. **Approve semantics.** A custom role with `Orders: Approve` can perform a Write-gated order action (e.g. delete). Document that this is intended.
9. **Preserve role.** HM + role user: role chip remains; remove HM → the role's permissions apply immediately.
10. **No role.** A non-HM user with no role sees only the login/home; Users list shows the amber «بدون دور — لا صلاحيات» chip.
11. **Manager scope.** A user with the `manager` preset cannot reach Users/Roles/Logs/Settings by URL.
12. **No JS errors.** Browser console clean on all changed pages.

---

## 11. Migration and deployment runbook

### 11.1 Pre-flight

1. Back up the target PostgreSQL database.
2. Record the current migration: `dotnet ef migrations list`.
3. Confirm the environment has **no** role named exactly «مبيعات / مشتريات / إدخال البيانات / مدير»:

```sql
SELECT id, name
FROM roles
WHERE name IN ('مبيعات', 'مشتريات', 'إدخال البيانات', 'مدير');
```

Any rows here will cause that preset to be skipped (§7.4).

### 11.2 Generate (development only)

```bash
dotnet ef migrations add SeedSealedRoles \
  --project OrderHub.Infrastructure \
  --startup-project OrderHub.Web
```

Then add `migrationBuilder.Sql(SealedRoleSeedSql.V1);` as the final `Up` statement. Design-time factory: `DesignTimeAppDbContextFactrory.cs` (existing spelling).

### 11.3 Apply

```bash
dotnet ef database update \
  --project OrderHub.Infrastructure \
  --startup-project OrderHub.Web
```

No auto-migrate; no startup seeding.

### 11.4 Post-seed verification

```sql
SELECT r.role_key,
       r.name,
       r.is_sealed,
       COUNT(rp.feature) AS permission_count
FROM roles r
LEFT JOIN role_permissions rp ON rp.role_id = r.id
WHERE r.role_key IN ('sales', 'purchasing', 'data-entry', 'manager')
GROUP BY r.role_key, r.name, r.is_sealed
ORDER BY r.role_key;
```

Expected: exactly four rows, `is_sealed = true`, counts `9 / 6 / 5 / 11`.

Also verify the hierarchy cells:

```sql
SELECT r.role_key, rp.feature, rp.level
FROM roles r
JOIN role_permissions rp ON rp.role_id = r.id
WHERE r.role_key = 'manager'
ORDER BY rp.feature;
```

Expected: 11 rows, all `Approve`, none for `Logs`/`Users`/`Settings`.

Save both queries as `artifacts/seed/sealed-roles-verify.sql` for reuse.

### 11.5 Collision repair (only if a preset is missing)

1. Rename the colliding custom role to a free name and update any users assigned to it (manual, owner decision).
2. Re-run the seed statements (the same `SealedRoleSeedSql.V1` text) against the database. It is idempotent and will insert the missing preset plus its cells.
3. Repeat §11.4.

Do **not** delete the custom role to free the name; it may be assigned to users.

### 11.6 Rollback

```bash
dotnet ef database update <previous-migration-name> \
  --project OrderHub.Infrastructure \
  --startup-project OrderHub.Web
```

Effect: drops the index and both columns. Seeded roles remain as ordinary custom roles; their permissions remain. The previous app build continues to work. Re-applying the migration will re-seal matching roles? **No** — re-applying would insert new rows and leave the old ones unsealed. If a rollback is followed by a re-apply, inspect and reconcile manually. Record this in the deployment notes.

### 11.7 Dev seed compatibility

`artifacts/seed/orderhub-dev-seed.sql` uses different role names (no preset collisions) and does not set `is_sealed`/`role_key`. Order between it and the migration does not matter. Do not modify the dev seed in this batch.

---

## 12. Out of scope

- Approval workflows on orders/documents (they will consume `Approve` later).
- Role hierarchy/rank beyond the four-level scale.
- Seeding the Accountant role.
- Removing `IsAdmin`; it remains the Higher Manager flag.
- Changing delete gates from Write to Approve (R3).
- Forced logout / ticket invalidation for demoted users (unnecessary with DB checks).
- A startup integrity check or admin diagnostics page for preset completeness.
- `UserPermissionReader` filtering by `IsActive` (see §13.4).
- Case-insensitive uniqueness of custom role names.

---

## 13. Residual risks and follow-ups

### 13.1 Missing presets on legacy databases

If a name collides, one preset is absent with no in-app warning. Mitigation: pre-flight query, reserved-name guards, and §11.5. Accepted for this batch; a future diagnostics widget can surface it.

### 13.2 Extra DB read per operation

`AuthenticatedUser` now performs one primary-key read per executor operation (in addition to the permission read, which HM bypasses). Accepted for correctness. If profiling shows an issue, batch the admin flag into the permission read — do not cache across operations.

### 13.3 Sealed roles can be mutated directly in SQL

Seals are code-level (decision 9). A DBA can still edit `is_sealed` or permission rows. Drift is caught by the seed-drift test and by the verification query, not prevented.

### 13.4 Deactivated users keep their session

`AuthorizationService` never checks `IsActive`; a user deactivated while logged in keeps permissions until the cookie expires (8h sliding). This is **pre-existing** and unchanged by this batch. If the owner wants deactivation to be immediate, the clean follow-up is to return `(IsActive, IsAdmin)` from the reader and treat `!IsActive` as unauthenticated. Do not implement that here without sign-off.

### 13.5 In-circuit denial UX

`AuthorizeRouteView` renders the `<NotAuthorized>` warning instead of redirecting to `/access-denied` for in-circuit navigation. Pre-existing; unchanged.

### 13.6 Write-level deletes

Manager (Approve) can delete operational records because deletes remain Write-gated. Explicitly accepted (R3).

---

## 14. File-by-file change map

### Phase 1 — Domain

| File | Change |
|---|---|
| `OrderHub.Domain/Enums/PermissionLevel.cs` | Add `Approve` with `[Description("اعتماد")]` |
| `OrderHub.Domain/Models/Role.cs` | Add `IsSealed`, `RoleKey`, `CreateSealed`, `EnsureNotSealed`; guard `Rename`/`SetPermission` |
| `OrderHub.Domain/Authorization/SealedRolePresets.cs` | **New** catalog + reserved-name helper |

### Phase 2 — Application

| File | Change |
|---|---|
| `Authorization/AuthorizationService.cs` | Guard whitelist + satisfaction switch + `IsHigherManagerAsync` |
| `Authorization/IAuthorizationService.cs` | Add `IsHigherManagerAsync` |
| `Authorization/IUserAdminReader.cs` | **New** |
| `Authorization/CheckHigherManagerQuery.cs` | **New** query + handler |
| `Authorization/RequireHigherManagerAttribute.cs` | **New** |
| `Authorization/AuthorizationBehavior.cs` | Evaluate `[RequireHigherManager]` |
| `Roles/GetRoles/RoleListItem.cs` | Add seal fields + per-level counts |
| `Roles/GetRole/GetRoleResponse.cs` | Add seal fields |
| `Roles/Create/CreateRoleCommandHandler.cs` | Reserved-name guard |
| `Roles/Update/UpdateRoleCommandHandler.cs` | Sealed + reserved-name guards |
| `Roles/Delete/DeleteRoleCommandHandler.cs` | Sealed guard |
| `Roles/*/*Command.cs`, `Roles/*/*Query.cs` | `[RequireHigherManager]` |
| `Users/*/*Command.cs`, `Users/*/*Query.cs` | `[RequireHigherManager]` |
| `Users/GetUsers/GetUsersQuery.cs` (`UserListItem` record) | `IsRoleSealed` |

### Phase 3 — Infrastructure

| File | Change |
|---|---|
| `Configurations/RoleConfiguration.cs` | Map `is_sealed`, `role_key`, unique index |
| `Features/Administration/UserAdminReader.cs` | **New** |
| `Features/Administration/RoleReadService.cs` | Project seal fields/counts; sort sealed first |
| `Features/Administration/Users/UserReadService.cs` | Project `IsRoleSealed` |
| `DependencyInjection/ServiceCollectionExtensions.cs` | Register `IUserAdminReader` |
| `Migrations/SealedRoleSeedSql.cs` | **New** frozen SQL constant |
| `Migrations/20261108100000_SeedSealedRoles.cs` (+ `.Designer.cs`, snapshot) | **New** migration |

### Phase 4 — Web

| File | Change |
|---|---|
| `Services/AuthenticatedUser.cs` | DB-checked `IsAdmin` |
| `Services/PermissionAccess.cs` | `IsHigherManagerAsync` |
| `Infrastructure/Authorization/HigherManagerRequirement.cs` | **New** requirement + handler |
| `Infrastructure/Authorization/HigherManagerAuthorizeAttribute.cs` | **New** |
| `Infrastructure/Authorization/ServiceCollectionExtensions.cs` | Enum-driven policies + HM policy + handler |
| `Infrastructure/Authorization/PermissionAuthorizationResultHandler.cs` | Handle HM requirement |
| `Controllers/Auth.cs` | Remove the `Admin` role claim |
| `Components/Layout/MainLayout.razor` | HM check; «مدير أعلى» |
| `Components/Layout/NavMenu.razor` | HM check |
| `Pages/.../Roles/Index.razor` | HM gate; badge; view-only rows |
| `Pages/.../Roles/Editor/Editor.razor` | HM gate; read-only mode; Approve counts |
| `Pages/.../Roles/Editor/RolePermissionCards.razor` | 4 segments, 4 bulk actions, tones, legend |
| `Pages/.../Roles/Editor/RoleEditorDialog.razor` | Verify only (consumes the cards) |
| `Pages/.../Users/Index.razor` | HM gate; no-role chip; sealed marker; stats/wording |
| `Pages/.../Users/Create.razor` | Grouped select; HM disables role picker; preview card; wording |
| `Pages/.../Users/Edit.razor` | Same as Create |

### Phase 5 — Tests

| File | Change |
|---|---|
| `OrderHub.Tests/SealedRoleDomainTests.cs` | **New** (tests 1–7) |
| `OrderHub.Tests/AuthorizationServiceTests.cs` | **New** (tests 8–13) |
| `OrderHub.Tests/RoleCommandGuardTests.cs` | **New** (tests 14–17) |
| `OrderHub.Tests/UserRolePreservationTests.cs` | **New** (tests 18–19) |
| `OrderHub.Tests/RolePermissionEditorStateTests.cs` | **New** (test 20) |
| `OrderHub.Tests/SealedRoleSeedTests.cs` | **New** (tests 21–25) |
| `OrderHub.Tests/RoleReadServiceTests.cs` | **New** (tests 26–27) |

### Phase 6 — Docs

| File | Change |
|---|---|
| `plans/roles-and-permissions-study.md` | Mark decisions locked; remove superseded options |
| `artifacts/seed/sealed-roles-verify.sql` | **New** verification queries (§11.4) |

---

## Appendix A — Error strings

| Case | Message |
|---|---|
| Sealed update | «لا يمكن تعديل دور جاهز؛ الأدوار الجاهزة محمية من التعديل.» |
| Sealed delete | «لا يمكن حذف دور جاهز؛ الأدوار الجاهزة جزء من النظام.» |
| Reserved name | «هذا الاسم محجوز لأحد الأدوار الجاهزة.» |
| Role not found (existing) | «الدور غير موجود.» |
| Duplicate name (existing) | «اسم الدور مستخدم بالفعل.» |
| Role assigned (existing) | «لا يمكن حذف الدور لأنه مرتبط بمستخدمين.» |
| Last active HM (existing) | «لا يمكن إلغاء صلاحية المدير أو تعطيل الحساب لأنه المدير النشط الوحيد في النظام.» |
| Authorization denied (existing) | `AuthorizationDeniedException` default message («غير مصرح لك بتنفيذ هذه العملية.») |

---

## Appendix B — Acceptance criteria

1. `PermissionLevel.Approve` exists; every `Permission:{feature}:Approve` policy is registered; an Approve requirement is satisfied only by Approve or HM.
2. Higher Manager authority comes from `users.is_admin` on every operation; promotion and demotion take effect without logout; the `Admin` role claim is no longer issued or read.
3. All five administration pages are gated by `[HigherManagerAuthorize]`, and every administration command/query carries `[RequireHigherManager]`.
4. Sealed roles are read-only in the UI, protected at the handler level, and immutably guarded in the domain.
5. Creating or renaming to a preset name fails with the Arabic reserved-name message.
6. The migration adds both columns, the unique index, and exactly four sealed roles with 31 permission cells; it is idempotent and collision-safe; `Down` drops only schema artifacts.
7. Tests 1–27 pass; the existing suite stays green; Release build has zero warnings.
8. Users list and user editor reflect «مدير أعلى», the no-role warning, the grouped role select, the sealed marker, and the read-only preview card.
9. The role matrix is four-level with a consistent color system and a ≥12px legend.
10. The PostgreSQL rehearsal (§11) reproduces the exact matrix; the verification query returns `9 / 6 / 5 / 11`.
