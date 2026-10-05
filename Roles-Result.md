# OrderHub — Application Roles Feature

**Application implementation complete. Infrastructure integration remains pending, as explicitly requested.**

All five use cases follow the current Administration/Users patterns: MediatR commands/queries, Arabic business failures using `Result`, explicit handler dependencies, feature-owned detail/listing services and write-side domain repositories. Domain and Application build successfully; 22 focused tests pass.

## Files and structure

Created 18 Application feature files under `OrderHub.Application.Features.Administration.Roles` and five focused test files. Modified only the existing Application `IRoleRepository` contract. No existing production feature was removed or refactored. This report is an additional review artifact.

```text
Roles/
    RolePermissionInput.cs
    Create/
        CreateRoleCommand.cs
        CreateRoleRequest.cs
        CreateRoleCommandHandler.cs
    Update/
        UpdateRoleCommand.cs
        UpdateRoleRequest.cs
        UpdateRoleCommandHandler.cs
    Delete/
        DeleteRoleCommand.cs
        DeleteRoleCommandHandler.cs
    GetRole/
        GetRoleQuery.cs
        GetRoleResponse.cs
        RolePermissionItem.cs
        IRoleDetailsService.cs
        GetRoleQueryHandler.cs
    GetRoles/
        GetRolesQuery.cs
        RoleListItem.cs
        IRoleListingService.cs
        GetRolesQueryHandler.cs
```

The shared `RolePermissionInput` belongs directly to the Roles feature because Create and Update require the same Feature/Level input. Each meaningful type has its own file. No DTO/Models/Requests/Responses root folders or static wrappers were introduced.

## Create

`CreateRoleCommand(CreateRoleRequest)` accepts Name and an `IReadOnlyList<RolePermissionInput>`.

The handler checks name uniqueness, constructs `Role`, applies each input through `Role.SetPermission`, adds the aggregate and commits once through `IUnitOfWork`. It returns the same non-generic `Result` convention used by Create User. `None` produces no permission row through Domain behavior.

Uniqueness checks use the trimmed name that the Domain stores; no case-insensitive/database-specific comparison is introduced. Domain name validation exceptions remain Domain concerns and are not converted into business failures.

## Update and permission synchronization

`UpdateRoleCommand(RoleId, UpdateRoleRequest)` loads the existing Role and checks uniqueness while excluding the target Role ID, before mutating the aggregate.

The input is complete desired state. The handler snapshots existing Features absent from the submitted Feature set and removes them using `Role.SetPermission(feature, PermissionLevel.None)`. It then applies submitted Feature/Level values through the same method and commits once.

Consequently:

- Omitted permissions are removed, including multiple omissions in one update.
- An empty permission collection removes all permissions.
- Explicit `None` removes that Feature.
- Retained permissions are updated in place rather than clearing/recreating the whole permission collection.
- New Features are added by the Domain.
- Repeated inputs follow the Domain's sequential setting behavior; the final submitted setting determines the resulting Feature permission.

Synchronization uses Feature identity, never a permission entity ID or generated Role ID. Tests cover removal/update with permission Role IDs still at zero. Application does not construct `RolePermission` or modify `Role.Permissions` directly.

## Delete

`DeleteRoleCommand(RoleId)` loads the Role, checks whether any user is assigned, and returns a business failure if assigned. Otherwise it removes the aggregate and commits once. Assignment checking explicitly includes inactive users and administrators; it is not restricted to active users.

The application guard precedes removal and does not rely on a foreign-key exception as normal business validation.

Business messages:

| Case | Result message |
|---|---|
| Missing Role | `الدور غير موجود.` |
| Duplicate Role name | `اسم الدور مستخدم بالفعل.` |
| Assigned Role deletion | `لا يمكن حذف الدور لأنه مرتبط بمستخدمين.` |

Repository/commit failures propagate, consistent with current User handlers, instead of being mislabeled as normal business failures.

## Read-side contracts

The naming and delegation match the existing GetUser/GetUsers use cases:

- `GetRole.IRoleDetailsService.GetRoleDetailsAsync(roleId, cancellationToken)` returns `GetRoleResponse` containing Id, Name and `IReadOnlyList<RolePermissionItem>`. Each permission exposes only Feature and Level.
- `GetRoles.IRoleListingService.GetAsync(cancellationToken)` returns `IReadOnlyList<RoleListItem>`, exposing only Id, Name and PermissionCount.

Details preserve a null not-found result, following the existing GetUser convention. The project's nullable context is currently disabled; the contract documents the null result rather than introducing different annotations solely for Roles. Queries delegate directly to their feature-owned services and do not load Domain aggregates through `IRoleRepository`.

Existing `AddApplication` assembly scanning discovers all five handlers. No new DI/mediator pattern or Application registration extension was needed.

## Write repository changes

The existing `OrderHub.Application.Interfaces.Repositories.IRoleRepository` keeps its original GetByIdAsync/Add/Remove operations and adds only:

```csharp
Task<bool> NameExistsAsync(
    string name,
    int? excludeRoleId = null,
    CancellationToken cancellationToken = default);

Task<bool> IsAssignedToUsersAsync(
    int roleId,
    CancellationToken cancellationToken = default);
```

The contract documents complete permission loading for mutation, normalized-name checking and all-user assignment checking. No list/detail projection operation or EF API was added to the write repository.

## Validation

| Check | Result |
|---|---|
| Domain build | Passed; 0 warnings, 0 errors |
| Application build | Passed; 0 warnings, 0 errors |
| New focused Roles tests | **22 passed, 0 failed, 0 skipped** |
| Application EF/Infrastructure/Web dependency check | Passed, including an assembly-reference test |
| Automatic registration of all five handlers | Passed, including a MediatR listing dispatch with mocked services |
| Complete-state permission removal | Passed |
| Duplicate-name rejection/exclusion of current Role | Passed |
| Assigned and unassigned Role deletion | Passed |
| Cancellation forwarding | Verified for write repository, commit and read calls |
| `git diff --check` | Passed |

Focused tests cover creation, normalization, Read/Write/None, repeated Feature setting, Domain name validation, missing Roles, duplicate failures without mutation/commit, omitted/empty/None synchronization, in-place updates, assigned/unassigned deletion, null details, empty/populated listing, exception propagation, layer references and registration.

The existing test architecture supports xUnit/Moq Application handler tests. However, the normal test project was already unable to compile before changes: `UpdateUserCommandHandlerTests.cs` references the removed `UpdateUserRequestPermission` type (`CS0246`). Those unrelated tests were not weakened, deleted or changed.

The new test sources were therefore executed through a temporary isolated test project referencing only Application/Domain, using the existing package versions and `OrderHub.Tests` friend-assembly name. It compiled the actual new test files directly. The temporary project was removed after validation. Results remain in the ignored test-results directory:

[Roles-focused.trx](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/TestResults/Roles-focused.trx)

## Integration concerns and next Infrastructure work

The full production solution currently cannot build: the existing Infrastructure `RoleRepository` does not implement the two newly required interface operations. The final solution build reports exactly two `CS0535` errors:

- `IRoleRepository.NameExistsAsync(string, int?, CancellationToken)`.
- `IRoleRepository.IsAssignedToUsersAsync(int, CancellationToken)`.

This is a contract-integration gap from this Application-only implementation, not a passing solution build. Infrastructure must implement these operations in its separate phase. No throwing/default interface shim was introduced to hide the gap.

Infrastructure must also implement and register `IRoleDetailsService` and `IRoleListingService` before the new read use cases can be resolved in production. The ordinary test project additionally needs its pre-existing User test assumptions updated to the new Domain model in a separate task.

No Domain change was necessary. Domain, Infrastructure, Web, Users, authentication, authorization, Current User, executor, migrations and production project/package references were left unchanged. No old per-user permission type or behavior was reintroduced.

No commit or staging operation was performed. All changes remain available for review.

## Complete file inventory

Created:

- [OrderHub.Application/Features/Administration/Roles/Create/CreateRoleCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Create/CreateRoleCommand.cs)
- [OrderHub.Application/Features/Administration/Roles/Create/CreateRoleCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Create/CreateRoleCommandHandler.cs)
- [OrderHub.Application/Features/Administration/Roles/Create/CreateRoleRequest.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Create/CreateRoleRequest.cs)
- [OrderHub.Application/Features/Administration/Roles/Delete/DeleteRoleCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Delete/DeleteRoleCommand.cs)
- [OrderHub.Application/Features/Administration/Roles/Delete/DeleteRoleCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Delete/DeleteRoleCommandHandler.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRole/GetRoleQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRole/GetRoleQuery.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRole/GetRoleQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRole/GetRoleQueryHandler.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRole/GetRoleResponse.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRole/GetRoleResponse.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRole/IRoleDetailsService.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRole/IRoleDetailsService.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRole/RolePermissionItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRole/RolePermissionItem.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRoles/GetRolesQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRoles/GetRolesQuery.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRoles/GetRolesQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRoles/GetRolesQueryHandler.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRoles/IRoleListingService.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRoles/IRoleListingService.cs)
- [OrderHub.Application/Features/Administration/Roles/GetRoles/RoleListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/GetRoles/RoleListItem.cs)
- [OrderHub.Application/Features/Administration/Roles/RolePermissionInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/RolePermissionInput.cs)
- [OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleCommand.cs)
- [OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleCommandHandler.cs)
- [OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleRequest.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleRequest.cs)
- [OrderHub.Tests/Application/Features/Administration/Roles/CreateRoleCommandHandlerTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Application/Features/Administration/Roles/CreateRoleCommandHandlerTests.cs)
- [OrderHub.Tests/Application/Features/Administration/Roles/DeleteRoleCommandHandlerTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Application/Features/Administration/Roles/DeleteRoleCommandHandlerTests.cs)
- [OrderHub.Tests/Application/Features/Administration/Roles/RoleFailureAndRegistrationTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Application/Features/Administration/Roles/RoleFailureAndRegistrationTests.cs)
- [OrderHub.Tests/Application/Features/Administration/Roles/RoleReadQueryHandlerTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Application/Features/Administration/Roles/RoleReadQueryHandlerTests.cs)
- [OrderHub.Tests/Application/Features/Administration/Roles/UpdateRoleCommandHandlerTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Application/Features/Administration/Roles/UpdateRoleCommandHandlerTests.cs)
- [Roles-Result.md](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/Roles-Result.md)

Modified:

- [IRoleRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Interfaces/Repositories/IRoleRepository.cs)
