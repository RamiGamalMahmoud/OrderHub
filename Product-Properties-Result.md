# Product Properties implementation result

Completed on 6 October 2026 against revision `f1e9847`, branch `feature/web-app`.

## 1. Summary

Implemented Product Properties management at `/properties` with a compact index table and one shared Create/Update MudBlazor dialog. The editor manages the actual property definition and its List options together. It preserves option identities, validates the complete proposed definition, protects saved order selections, and persists updates atomically.

Deletion requires confirmation and rejects product/order usage. Added a narrowly scoped foreign-key migration to prevent property deletion from cascading into saved order data. No production database migration or deployment was performed.

## 2. Existing code reviewed

- Domain: Property, PropertyOption, PropertyType, Product, ProductProperty, OrderItemProperty, Order/OrderItem, Category, and commercial document models.
- Application: all Properties CRUD/read requests, handlers, contracts, product property inputs, order property read contracts, permission metadata, request executor, and unit of work.
- Infrastructure: Properties reader/repository, Property/Option/ProductProperty/OrderItemProperty configurations, model snapshot, order property readers, and migration conventions.
- Web: Categories formatting/layout, Cities shared dialog/index behavior, existing product/order dialogs, order option selection/validation, permission adapters, navigation, and theme.
- Tests: Properties application/persistence tests, the placeholder domain test file, Cities/Category HTML rendering conventions, architecture tests, and authorization tests.
- Retired WPF Properties editor/index were inspected for behavior only. Obsolete static wrappers and DTO references were not restored.

## 3. Exact discovered Domain model

Property is an aggregate with a name, PropertyType, optional description, and a privately managed collection of PropertyOption entities. Types are Text, Boolean, and List.

Only List properties contain options, and a List must contain at least one option. Property names are unique through the existing database index. Options have database-generated IDs and are unique within a property; the existing domain rule compares normalized values case-insensitively. Existing database lengths are 100 characters for names and option values. Description is optional text.

## 4. Property, Option, Product, and Category relationships

| Relationship | Discovered behavior |
| --- | --- |
| Property → PropertyOption | Aggregate ownership through PropertyId; owned options cascade on deletion of an otherwise unused property |
| Product → ProductProperty → Property | ProductProperty stores the definition ID and IsRequired; Property foreign key is restrictive |
| OrderItem → OrderItemProperty → Property | Saved order properties store PropertyId and a string Value |
| PropertyOption → order selection | No option-ID foreign key; orders save the selected option value as text |
| Category → properties | No direct property/category relationship; products have Categories and their own Property definitions |
| Commercial documents | No independent PropertyOption/Property foreign-key relationship found in the active commercial document models |

Option IDs appear in editor/read metadata, but saved selections use values. Consequently preserving IDs alone is insufficient to make renaming a used option safe.

## 5. Domain changes

- Added `PropertyOptionChange` as a small Domain-owned description of a proposed option change.
- Added `Property.UpdateDefinition` to validate the complete final state before changing aggregate state.
- Enforced required, trimmed names/options and the existing database maximum lengths.
- Reject undefined enum values, empty Lists, options on non-List types, duplicate normalized values, duplicate IDs, and IDs not owned by the target property.
- Preserve IDs for retained options; add only new options; remove only omitted options after application usage checks.
- Expose options through a read-only collection wrapper and restrict PropertyOption renaming to aggregate/internal behavior.
- Keep direct domain operations consistent with the nonempty-List invariant.
- Normalize description consistently during creation and updates.

The domain remains persistence-agnostic and contains no UI state.

## 6. Application changes

Reused the existing `Features/Setup/Properties/Create`, Update, Delete, Get, GetAll, and Contracts structure. Existing request permission metadata and response conventions remain in place: Create returns the new ID, Update completes without a payload, Delete returns Result, and business validation is reported through Arabic DomainException messages.

Create validates the aggregate before checking the normalized name. Update checks missing targets, uniqueness excluding itself, type-change usage, and changed/removed option values used in orders. Domain reconciliation then validates and applies the entire proposed definition.

Renamed the create description parameter to `Description` and update payload to `Input`, removing the typo and generic DTO-style parameter name. No request execution mechanism or generic repository abstraction was added.

## 7. Infrastructure changes

- Added narrow name-conflict, property-usage, order-option-value usage, and option-rename preparation operations to the existing repository contract/implementation.
- Index reads are untracked, ordered by Name then ID, and project option counts in the same database query.
- Edit reads return options ordered by stable ID.
- PostgreSQL Update acquires a row lock before loading the current property/options, held within the application unit-of-work transaction.
- Changed OrderItemProperty's Property relationship from Cascade to Restrict.
- Added `20261006085328_RestrictPropertyDeletionForOrders` and its designer; updated the model snapshot.

The generated migration was reviewed and reduced to the requested order-property foreign key. An unrelated pre-existing Users/Role constraint naming difference suggested by the tooling was excluded; Users/Role source and database behavior were not changed.

## 8. Web implementation

Added `Components/Pages/Features/Properties/Index.razor` and a feature-local Editor folder containing the shared dialog, property state, option state, and small option-row component, plus feature-local imports.

The index shows Name, Type, option count for Lists, and authorized actions. It handles loading, empty, failure/retry, dialogs, confirmation, and successful refresh without an application reload. Local MudTable pagination is available for lists above ten rows. Search was not added: the old Properties list did not provide it, and no need for extra filtering infrastructure was established.

The existing navigation link now targets `/properties`. There are no separate Create/Edit pages or routes.

## 9. Unified dialog behavior

Null PropertyId creates a new definition; a supplied PropertyId loads the existing definition through GetPropertyQuery. Both modes use the same form, validation, fields, options, and save flow.

Fields are Name, Type, optional Description, and options when Type is List. The dialog handles loading, missing records, retry, field validation, cross-option validation, saving, business failures, unexpected failures, Cancel, and success.

Save uses `await _form.ValidateAsync()`. Successful operations close with `DialogResult.Ok(true)` and trigger an index refresh. Failures keep the dialog open with entered data and a readable error. Explicit Cancel is disabled during saving; Escape/backdrop dismissal and the implicit close button are disabled to avoid closing an in-flight save.

Authorization denial follows the existing access-denied navigation convention and cancels the dialog.

## 10. Option editing behavior

Lists display editable option rows with Add and Remove controls. Every row has a stable UI key independent of its persisted ID. Adding creates a temporary row with no database ID; existing rows retain their original IDs.

Validation requires nonempty values of at most 100 characters, prohibits normalized case-insensitive duplicates, and requires at least one List option. UI filtering/validation improves usability; Domain/Application independently enforce the rules.

Switching away from List shows a warning that saving will remove its options. Switching back before saving preserves the unsaved option state. The application prohibits changing the type of a property already used by products/orders.

## 11. Option identity and update strategy

Updates reconcile by ID. Existing rows are renamed in place, omitted unused rows are removed, and only new rows receive new generated IDs. Existing options are never blindly deleted and recreated.

Renaming multiple options can legitimately swap existing values. The unique option index would otherwise cause transient collisions. Infrastructure vacates changed values with one parameterized bulk update to short unique temporary values inside the existing transaction, updates EF's original-value tracking, and then performs the final single SaveChanges. Temporary values are never committed as user data. This supports swaps while preserving IDs and atomicity; PostgreSQL tests verify the behavior.

Raw SQL remains exclusively in Infrastructure. Temporary prefixes are parameterized; option IDs come from tracked persisted entities and are formatted as integers.

## 12. Final delete rules

- Missing property: fail clearly in Arabic.
- Any ProductProperty reference: reject deletion.
- Any OrderItemProperty reference: reject deletion.
- Otherwise: delete the unused property and its owned options.

The index requires confirmation and refreshes after successful deletion. Order values cannot be cascade-deleted by the property foreign key after the included migration is applied. Product references remain restrictive.

## 13. Referenced option and property rules

Saved order selections are matched by both PropertyId and the existing option Value. Renaming or removing a selected value is blocked because it would make the saved order selection unavailable during editing. Unselected options remain editable/removable, subject to the nonempty List rule.

Property name and description remain editable while in use. Changing Type is blocked for product/order usage to preserve the meaning of existing values. Order/commercial data is not rewritten or deleted to accommodate definition changes.

Serialized editor/draft snapshots are not relational option references and are not modified by this feature. Stale editor sessions remain subject to the application's existing metadata revalidation behavior.

## 14. Authorization mapping

| Operation | Existing permission |
| --- | --- |
| `/properties`, list, detail read | Properties.Read |
| Create, Update, Delete | Properties.Write |
| Add/Edit/Delete presentation | Visible only through existing Write permission decision |

Read-only users see the table without mutation actions. Application authorization independently blocks writes. Admin bypass, cookie contents, current-user contracts, and permission services are unchanged. All Web requests execute through IRequestExecutor.

## 15. Persistence and query strategy

Index rows come from one untracked projection with option counts, without loading full tracked aggregates or products using the property. PostgreSQL command interception verifies one database read for the list. The existing PropertyListItem contract is extended with OptionsCount; existing name/type/description data remains available.

The editor uses one targeted detail projection. Update alone loads the tracked aggregate and its options for reconciliation. Usage checks operate on relationships/changed values rather than issuing a query per UI row. No cache or generic paging/search framework was introduced.

## 16. Transaction behavior

Create calls SaveChanges once; EF persists the new aggregate/options atomically. Update begins the existing unit-of-work transaction, locks and loads the aggregate, checks usage, validates and reconciles the final definition, prepares any unique-value renames, saves once, then commits. Failure rolls back using a non-canceled rollback token.

No SaveChanges is executed inside an option loop. A test database trigger deliberately fails option persistence and verifies that the name and option values both remain unchanged. PostgreSQL tests verify the provider-specific row-lock/update path, stable IDs, value swaps, and restrictive history protection.

Concurrent property writes are serialized on the property row in PostgreSQL. No new optimistic stale-editor conflict protocol or order-editor redesign was introduced.

## 17. Compact and responsive UI

- Dense outlined MudTable, readable heading, small labeled action buttons, and wrapped names.
- Standard MudTable small-screen behavior with column DataLabels.
- Type and option count are concise; complete option lists are not displayed in every table row.
- Small responsive dialog with content limited to 60vh and vertical scrolling, keeping actions separate.
- Dense outlined inputs, a short description field, and simple option rows.
- Existing RTL/theme/providers reused.

No Bootstrap, Tailwind, JavaScript, additional UI library, C# breakpoint state, separate mobile page, or custom CSS file was introduced.

## 18. Formatting and readability review

Manually reviewed changed/new Properties Razor and C# files, tests, and the migration. Component attributes use readable multiline formatting; state fields are separate; control flow has braces and early returns; save/load/delete/option workflows use named methods and clear spacing. Dense log calls are formatted vertically.

The option-row component keeps repeated field/remove markup focused. No generic dialog CRUD framework was created. Short table-row lambdas only pass the selected record to named workflow methods. Migration/snapshot review confirmed that the only snapshot behavior change is the order-property delete restriction.

## 19. Tests added and updated

Added 34 cases across:

- Updated `OrderHub.Tests/Domain/PropertyTests.cs`: actual types, required names, normalized/bounded/duplicate values, and validation before aggregate mutation.
- New `OrderHub.Tests/Infrastructure/ProductPropertiesTests.cs`: empty/sorted list and counts, name uniqueness, unchanged/valid rename, invalid creation, identity-preserving option diff, duplicate/foreign/repeated IDs, missing targets, unused type changes/deletion, product and order guards, rollback on database failure, and migration scope.
- New `OrderHub.Tests/Web/ProductPropertiesWebTests.cs`: permission-dependent table actions/counts, Create/Edit initial values, Cancel, successful results, dynamic options/validation, business failures, missing data, access denial, success refresh, empty state, confirmation, request permissions, and the single route.
- New `OrderHub.Tests/Infrastructure/PostgresPropertyTests.cs`: one-query counts, provider update/row locking, option identity/value swaps, rollback after invalid updates, and foreign-key protection of saved order values.

Existing property, product property, order, architecture, and authorization tests were retained. Existing application tests still verify Create's ID response and the established persistence exception convention.

PostgreSQL tests are explicitly opt-in using `ORDERHUB_PROPERTY_TEST_POSTGRES`, pointing to a disposable server with database creation rights. Each test creates/drops its own generated database. Without that variable, the two tests are skipped.

## 20. Focused test results

Command: `dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-restore --filter 'FullyQualifiedName~Property|FullyQualifiedName~Properties' -v minimal`.

Result: **58 passed, 0 failed, 0 skipped**, with the disposable PostgreSQL test server enabled. Log: `properties-tests.log`.

The isolated PostgreSQL 18 server was stopped and its workspace data directory removed after validation. Existing application databases and other PostgreSQL services were not modified.

## 21. Full test results

Command: `dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-build --no-restore -v minimal`.

Result: **334 passed, 2 failed, 3 skipped; 339 total**. Log: `properties-full-tests.log`.

The skipped cases are the existing opt-in PostgreSQL Category tests; their environment variable was not configured. Both new Properties PostgreSQL tests passed in the full run.

## 22. Build and migration validation

Domain, Application, Infrastructure, Web, and Tests built successfully. Final command: `dotnet build OrderHub.Web/OrderHub.Web.csproj --no-restore -v minimal`.

Final incremental build: **0 errors, 0 warnings**. Recompilation also succeeded and emitted only existing warnings described below. Log: `properties-build.log`.

Generated and reviewed the PostgreSQL SQL script between AddRoleBasedPermissions and RestrictPropertyDeletionForOrders. It only drops/re-adds the order-property foreign key with `ON DELETE RESTRICT`, then records the migration. The script is an ignored local validation artifact under Infrastructure/obj. Migration-operation tests confirm only the intended two foreign-key operations.

**Required rollout step:** apply `20261006085328_RestrictPropertyDeletionForOrders` through the normal database deployment process. It has not been applied to a production/existing database during this task.

## 23. New versus pre-existing warnings/failures

Before edits, the full suite on clean revision `f1e9847` produced **300 passed, 2 failed, 3 skipped; 305 total**. Log: `properties-baseline-tests.log`.

The final run has the identical pre-existing failures:

1. `HTTP_permission_policy_denial_returns_403_without_invoking_endpoint`: expects 403, receives the existing 302 access-denied redirect.
2. `Application_denial_maps_to_generic_HTTP_403`: expects an Arabic denial body, receives an empty body in the current test setup.

Their production handlers and tests were not changed. No new test failures remain.

Existing warnings on recompilation are two MUD0002 warnings in the unrelated Roles editor and BL0005/BL0006 warnings in existing product/order tests. No new Properties warnings remain and no warnings were suppressed.

## 24. Files added, changed, and deleted

Added:

- `OrderHub.Domain/Models/PropertyOptionChange.cs`
- `OrderHub.Infrastructure/Migrations/20261006085328_RestrictPropertyDeletionForOrders.cs`
- Its corresponding `.Designer.cs`
- `OrderHub.Web/Components/Pages/Features/Properties/_Imports.razor`
- `OrderHub.Web/Components/Pages/Features/Properties/Index.razor`
- `OrderHub.Web/Components/Pages/Features/Properties/Editor/PropertyEditorDialog.razor`
- `PropertyEditorState.cs`, `PropertyOptionEditorState.cs`, and `PropertyOptionEditorRow.razor` in that Editor folder
- `OrderHub.Tests/Infrastructure/ProductPropertiesTests.cs`
- `OrderHub.Tests/Infrastructure/PostgresPropertyTests.cs`
- `OrderHub.Tests/Web/ProductPropertiesWebTests.cs`
- `Product-Properties-Result.md`

Changed:

- Domain Property and PropertyOption
- Properties repository contract; Create command/handler; Update command/handler; Delete handler; PropertyListItem
- Infrastructure Properties reader/repository
- OrderItemProperty configuration and AppDbContextModelSnapshot
- Existing Domain Property tests
- NavMenu's Properties target route

Deleted: none. Final diff check found no whitespace errors. Changes outside the Properties folders are strictly required navigation, domain, history-protection configuration, snapshot/migration, and feature tests.

## 25. Obsolete code

No active request was superseded: existing CRUD/read requests and narrow persistence contracts were reused. No clearly unused active Property source required deletion. The former unsafe option-update workflow was replaced with aggregate reconciliation; the generic empty delete failure was replaced with explicit usage guards.

Retired WPF/API references were left outside the active architecture. No legacy wrappers, stores, repositories, lookup services, DTOs, or separate editor pages were restored.

## 26. Remaining risks and deferred work

- Apply the included restrictive foreign-key migration during normal rollout; application guards are already implemented, but existing databases need the migration for the stronger database safeguard.
- The two pre-existing HTTP authorization failures remain outside this feature's scope.
- No authenticated live-browser viewport inspection was performed. Automated HTML/form/dialog tests and SQLite/PostgreSQL persistence checks passed.
- Setup lists are loaded locally; very large datasets could justify a future server paging/search design.
- Stale editor conflict detection and revalidation of already-open order/draft editors remain existing application concerns. Saved order records are protected and are not rewritten by this feature.
- Unrelated Role constraint naming drift encountered during migration generation was intentionally excluded from this task.

There is no unresolved Product Properties business-rule blocker.

## 27. Git and scope confirmation

No stage, commit, push, or branch change was performed. Changes remain unstaged in the existing checkout. HEAD remains `f1e9847` on `feature/web-app`.

No Products editor, Categories, Cities, Clients, Suppliers, Deliverymen, Shipping Carriers, authentication, permissions, generic CRUD framework, or deployment source was redesigned.
