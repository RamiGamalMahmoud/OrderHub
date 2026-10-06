# Categories implementation result

Completed on 6 October 2026 against repository revision `f8e7692`, branch `feature/web-app`.

## 1. Summary

Implemented the OrderHub Web Categories browser and unified Create/Update editor using the existing MudBlazor, RTL, request execution, persistence, and role authorization architecture. The browser displays one hierarchy level at a time, includes navigable breadcrumbs, and shows direct child counts and recursive product totals. Category writes validate names and parents, prevent cycles, and preserve conservative deletion rules.

No schema migration or deployment change is required.

## 2. Existing code reviewed

- Domain: `Category`, `Product`, `ModelBase`, `EntityName`, and `Money`.
- Application: all existing Category commands, queries, contracts, handlers, typed permission metadata, `IRequestExecutor`, and `IUnitOfWork`.
- Infrastructure: Category reader/repository, Category and Product EF configurations, context/factory, unit of work, and production service registration.
- Web: Roles index and shared editor conventions, `MainLayout`, `NavMenu`, `PermissionView`, `PermissionAccess`, route authorization, error boundary, and authenticated request executor.
- Tests: setup persistence, architecture boundaries, application authorization, role-backed authorization integration, and Web permission tests.
- Retired WPF: Categories index/editor models and views were inspected for behavior only. Their obsolete DTO/query references were not restored. The retired API/WPF projects are not implementation or build targets.

The active hierarchy has a nullable `ParentCategoryId`; products have a required category relationship. Both child-category and product foreign keys use `DeleteBehavior.Restrict`. Existing deletion handlers already prohibit deleting categories with direct children or direct products.

## 3. Domain changes

- Made `ParentCategoryId` privately settable so callers cannot bypass category movement behavior.
- `MoveTo` updates both the parent navigation and foreign key, maintains loaded child collections, and supports moving to root.
- `RemoveSubCategory` now uses the same movement behavior.
- Category names are trimmed before `EntityName` validates the existing 2–100 character constraint.
- Fixed Category-specific equality checks for unsaved entities: unrelated new categories with ID zero are no longer treated as the same category. Shared `ModelBase` behavior was not changed.
- Replaced recursive descendant collection with iterative traversal to avoid a call-stack depth limit.

Domain remains persistence-agnostic. Database ancestry validation supplements domain checks when ancestor navigations are not loaded.

## 4. Application changes

Preserved the current feature-oriented `Features/Setup/Categories` placement and the existing separate CreateCategory, UpdateCategory, DeleteCategory, and editor-read use cases.

Added the `GetCategories` use case with separate files for its query, handler, response, card item, and breadcrumb segment. `GetCategoriesResponse` includes parent existence, direct children, and the ordered hierarchy path.

Create and Update validate normalized names, parent existence, and sibling name conflicts, returning clear Arabic business failures. Update validates self-parenting and descendant-parent cycles before changing the entity. It runs inside the existing unit-of-work transaction and acquires a feature-owned hierarchy lock before reading or validating the move.

Retained `GetCategoryForEditQuery` for editor data and `GetCategoriesInfoQuery` for hierarchical parent options. Reduced the reader abstraction to the three reads used by the current feature. Added narrow repository operations for cycle validation and hierarchy locking; no generic persistence abstraction was introduced.

## 5. Infrastructure changes

- Rebuilt the Category reader around untracked projections and the internal `CategoryHierarchy`/`CategoryHierarchyRow` types.
- Browser reads execute one database query projecting category ID, name, parent ID, and direct product count. The hierarchy processing supplies all card counts and breadcrumbs without further database calls.
- Parent-option reads project hierarchy data without calculating product counts, then return full paths and accurate child-presence flags.
- Added a parameterized recursive ancestry CTE in the repository. `UNION` ensures termination even when encountering an existing corrupt cycle.
- PostgreSQL updates acquire `LOCK TABLE categories IN SHARE ROW EXCLUSIVE MODE` within the unit-of-work transaction. This serializes hierarchy writes while permitting normal reads, preventing concurrent opposite moves from both validating an obsolete hierarchy.
- SQLite tests use their existing transactional provider without the PostgreSQL-specific table lock. PostgreSQL locking and concurrency behavior were tested separately against a disposable local PostgreSQL server.

Application and Web contain no EF, SQL, DbContext, DbSet, or IQueryable dependencies for Categories.

## 6. Web changes

Added:

- `Components/Pages/Features/Categories/Index.razor` for root and nested browsing.
- `Components/Pages/Features/Categories/Components/CategoryCard.razor` as the reusable card.
- `Components/Pages/Features/Categories/Editor/Editor.razor` as the shared Create/Update form.
- `CategoryEditorState.cs` for editable values, parent filtering, and explicit return destinations.
- A feature-local `_Imports.razor`.

Cards show the icon, bold name, direct subcategory count, recursive product total, and a child-navigation action. Edit/Delete appear only with Write permission. Leaf cards have a disabled child-navigation action with no active child link.

The browser handles loading, empty root/nested levels, missing parents, and failed loads with retry. Deletion uses the existing MudBlazor confirmation dialog convention and refreshes the current level through the request executor without a full application reload.

The compact editor has Name and Parent Category fields, full hierarchy paths in the parent selector, Save/Cancel actions, and a visible `الرجوع للأقسام` button, including while loading or showing a failure. Self and all descendants are excluded from edit-mode parent choices. Application validation remains authoritative.

All use cases execute through `IRequestExecutor`. Existing permission services and route policies are reused; no authentication cookie, current-user, or authorization-service changes were made.

## 7. Final routes

| Route | Behavior |
| --- | --- |
| `/categories` | Root categories only |
| `/categories/{ParentId:int}` | Direct children of the supplied parent |
| `/categories/create` | Create a root category by default |
| `/categories/create?parentId=10` | Create with parent 10 preselected |
| `/categories/{CategoryId:int}/edit` | Edit the specified category |

## 8. Navigation behavior

Breadcrumbs start with `الأقسام` and continue through ancestors to the current category. Previous segments link to their corresponding hierarchy levels; the current segment is disabled.

Create preselects the browsed parent. Back and Cancel return to that original level. Editing returns to the original parent level where the edited card was displayed. Saving returns to the selected parent level, so a moved category is visible at its new location. A null parent resolves to `/categories`. Browser history is not used.

## 9. Direct children count

The projected hierarchy is indexed by parent ID. `DirectChildrenCount` is the number of rows whose parent ID equals the displayed category ID. Grandchildren are excluded. `HasChildren` is derived from this count.

## 10. Recursive product count

Each projected row contains its direct product count from the configured Product-to-Category relationship. An iterative queue starts at leaves and adds each completed category's total to its parent. A parent becomes ready after every direct child has been processed. Consequently each card includes products on the category and all descendants at every depth, exactly once.

The aggregation uses O(number of categories) memory and processing after the query. There is no per-card database call, product-entity materialization, caching, or fixed depth. Tests verify the 2 + 5 + 7 + 4 = 18 example, a 10,000-level hierarchy, and one database read per browser operation on PostgreSQL. A corrupt cycle fails clearly rather than hanging or displaying misleading totals.

## 11. Cycle prevention

The domain rejects self and loaded-descendant assignments. Application Update explicitly rejects matching category/parent IDs and checks the proposed parent's complete database ancestry using the repository CTE. A proposed descendant necessarily includes the edited category in its ancestry and is rejected.

The PostgreSQL hierarchy lock is acquired before validation and held through save/commit or rollback. The concurrency test submits opposite moves simultaneously and verifies exactly one succeeds, the other fails, and the resulting hierarchy remains valid.

## 12. Final delete rules

Preserved existing business behavior:

- Missing category: fail clearly.
- Any direct product: reject deletion.
- Any direct child category: reject deletion.
- Unused leaf/root category: allow deletion.

Descendant products are protected because reaching any descendant requires at least one direct child, which already blocks deletion. No destructive cascade was added. The restrictive database relationships remain a final guard against concurrent usage changes.

## 13. Authorization mapping

| Operation | Required permission |
| --- | --- |
| Browser, breadcrumbs, editor read, parent options | `Categories.Read` |
| Create, Update, Delete | `Categories.Write` |
| Browser route | `Categories.Read` |
| Editor routes | `Categories.Write` |
| Add/Edit/Delete UI | Visible only after existing Write access check |

Admin bypass and Write-includes-Read behavior continue through the existing authorization service. Tests verify every Category request's metadata, route policies, rendered read-only UI, and real application-pipeline denial of all three write commands without side effects.

## 14. Responsive design

Cards use MudGrid `xs=12`, `sm=6`, `md=4`, `lg=3`: approximately one, two, three, and four columns as available width increases. Spacing is compact; counts and the page header wrap. Cards fill their grid cell, names can wrap, and icon/actions follow the existing RTL provider.

The shared editor is limited to 640px and naturally fits narrower widths. Dense outlined inputs and standard MudBlazor components preserve readability. No custom breakpoint state, JavaScript, Bootstrap, Tailwind, duplicate viewport pages, or new CSS file was introduced.

HTML rendering tests verify presentation and action visibility. An authenticated live-browser viewport inspection was not performed.

## 15. Tests added and updated

Added six test files:

- `Domain/CategoryTests.cs`: unsaved identity, loaded cycle rejection, normalized names, child membership, valid moves, and removal to root.
- `Infrastructure/CategoryHierarchyTests.cs`: 10,000-level traversal and corrupt-cycle handling.
- `Infrastructure/CategoryPersistenceTests.cs`: root/direct-child reads, multiple-depth counts, empty and missing levels, breadcrumbs, hierarchical options, root/child creation, missing parents, invalid names, rename/reparent/root moves, unchanged edits, missing targets, sibling duplicates, self/direct/deep cycles, delete guards, cancellation, and role-backed write denial.
- `Infrastructure/PostgresCategoryTests.cs`: one-query browsing/counts/paths, PostgreSQL ancestry validation, and concurrent opposite moves.
- `Web/CategoryEditorTests.cs`: return/save destinations, descendant filtering, request metadata, and route policies.
- `Web/CategoryRenderingTests.cs`: read-only and writable browser output, empty state permissions, and disabled leaf navigation.

Updated existing setup and architecture tests to use the new browser instead of removed reads. Updated the authorization inventory from 93 to 89 protected requests: one browser query added and five superseded queries removed. Existing broad authorization behavior tests were retained.

PostgreSQL tests are opt-in through `ORDERHUB_CATEGORY_TEST_POSTGRES`; the supplied connection must refer to a disposable test server with database creation rights. Each test creates and drops its own generated database. Without the environment variable, those three tests are explicitly skipped.

## 16. Focused results

Command: `dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-restore --filter 'FullyQualifiedName~Category' -v minimal`.

Result: **32 passed, 0 failed, 0 skipped**, including all three PostgreSQL tests. Log: `category-tests.log`.

The isolated PostgreSQL 18 server was created solely for validation, listened on localhost, and was stopped and removed afterward. Existing application databases and PostgreSQL services were not modified.

## 17. Full-suite results

Command: `dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-build --no-restore -v minimal`, with the disposable PostgreSQL test connection configured.

Result: **271 passed, 2 failed, 0 skipped; 273 total**. Log: `full-tests.log`.

The two failures are pre-existing:

1. `PermissionAuthorizationTests.HTTP_permission_policy_denial_returns_403_without_invoking_endpoint`: expects 403; the current unchanged handler redirects to `/access-denied` with 302.
2. `PermissionAuthorizationTests.Application_denial_maps_to_generic_HTTP_403`: expects the Arabic denial message in the response body; the current unchanged middleware returns an empty body for that test setup.

Both failures were reproduced against an untouched `git archive HEAD` copy of revision `f8e7692`, using the original production code and tests. Log: `baseline-tests.log`. The temporary baseline source copy/archive were removed. Authentication/HTTP authorization behavior was intentionally left outside this feature's scope.

## 18. Build results and warnings

Command: `dotnet build OrderHub.Web/OrderHub.Web.csproj --no-restore -v minimal`.

Result: **successful, 0 errors, 2 existing warnings**. Domain, Application, Infrastructure, and Web built successfully; the test project also built during focused validation. Log: `category-build.log`.

Both Web warnings are existing `MUD0002` notices for `AlignItems` on MudGrid in the unrelated Roles editor. Existing test sources emit `BL0005`/`BL0006` notices. No warnings remain in the new/changed Categories code or tests, and no warnings were suppressed.

Final diff checks found no whitespace errors. Architecture tests confirm project boundaries and the absence of direct Web persistence dependencies. Changes outside Categories are limited to existing test expectations affected by the removed Category reads.

## 19. Superseded files removed

Removed from `OrderHub.Application/Features/Setup/Categories` after confirming no active Application, Infrastructure, or Web callers remained:

- `Contracts/CategoryListItem.cs`
- `Contracts/CategoryTree.cs`
- `GetCategoryList/GetCategoryListQuery.cs`
- `GetCategoryList/GetCategoryListQueryHandler.cs`
- `GetCategoryParentIds/GetCategoryParentIdsQuery.cs`
- `GetCategoryParentIds/GetCategoryParentIdsQueryHandler.cs`
- `GetCategoryTree/GetCategoryTreeQuery.cs`
- `GetCategoryTree/GetCategoryTreeQueryHandler.cs`
- `GetRootCategories/GetRootCategoriesQuery.cs`
- `GetRootCategories/GetRootCategoriesQueryHandler.cs`
- `GetSubCategories/GetSubCategoriesQuery.cs`
- `GetSubCategories/GetSubCategoriesQueryHandler.cs`

Their old reader methods, recursive tree builder, and nested flat-tree record were also removed. No legacy static wrappers, DTOs, stores, or generic repositories were restored.

## 20. Remaining limits and deferred work

- The two confirmed pre-existing HTTP authorization test failures remain; fixing that unrelated behavior requires a separate task.
- Browser reads intentionally project the complete category taxonomy and aggregate in memory. This avoids N+1 behavior and is sensible for the current feature. Extremely large taxonomies could warrant a subtree-specific PostgreSQL read implementation later; no caching was added.
- Parent options contain full paths. Their size grows with the taxonomy and path lengths; a remote-search selector could be evaluated for a substantially larger dataset.
- PostgreSQL serializes category updates during validation/save. This prioritizes hierarchy correctness; ordinary reads remain available.
- Live browser viewport/interaction checks remain unperformed; automated HTML rendering, editor-state, provider integration, concurrency, and build checks passed.
- Retired API/WPF source remains untouched and is not supported as an architectural target.

There is no unresolved Categories business-rule blocker.

## 21. Git and scope confirmation

No commit, staging, push, or branch change was performed. All implementation changes remain unstaged in the existing checkout. Repository HEAD remains `f8e7692` on `feature/web-app`.

No Products, Clients, Suppliers, Deliverymen, Shipping Carriers, authentication, permission caching, deployment, or general application redesign was performed.
