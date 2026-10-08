# Clients implementation report

## Inspection recorded before implementation
- Branch: `feature/web-app`; revision: `ded33cdc1e8dde8a25e4c876f68b3d345bee8405`. Initial working tree clean. No AGENTS.md found in the repository.
- Client owns an EntityName, optional Location, one required Address and one required Phone. Address/Phone are separate many-to-one records, not exclusive EF owned entities; no uniqueness constraints on name, phone or contact foreign keys. Duplicate names/numbers are allowed.
- EntityName requires 2–100 characters. Address requires nonblank street and City; database street limit 150. PhoneNumber requires 6–14 digits and `+` followed by 1–4 digits. Current database country-code width is 4 total characters, so persisted inputs must respect that width. Location is optional varchar(255), free text, not necessarily a URL.
- Only Order directly references Client in the current model; FK uses Restrict in configuration and current migration snapshot. Orders have ClientId and live client projections, not client name/phone/address snapshots. Existing OrderReader explicitly resolves current contact details. Editing Client intentionally updates that live directory presentation; order facts/items/delivery history are never mutated by Clients.
- Commercial documents store CustomerName/CustomerPhone/CustomerAddress snapshots. Quotations link to draft reference or Order; invoices/proformas link via documents/orders. No document ClientId exists. Client changes must leave these snapshots untouched.
- Persisted order drafts serialize Draft with Data.ClientId and ClientName into jsonb, without a Client FK. Delete must check these references as well as Orders.
- Legacy client_recipients/outbox tables were dropped by existing RemoveLegacyMessagingInfrastructure migration. Current Telegram API reads Order projections and contains no Client mutation rules. Do not revive messaging infrastructure or edit migration history.
- Historical WPF editor requires name, city, street, phone and country code; location optional in its view model. Its 7-digit UI minimum contradicts Domain's 6-digit minimum; Domain is authoritative. Legacy WPF architecture is stale and remains untouched.
- Existing Clients CQRS requests/contracts/readers/repository will be completed, preserving request shapes used by other features. Current edit read throws NullReferenceException for missing clients; replace with null for clear editor handling.
- Preserve exclusive contact IDs. Shared contacts require detachment before editing so another party's contact data cannot be rewritten. Client deletion retains contact rows because ownership is not exclusive.
- No EF schema change is required: historical FK already Restrict. No migration will be added/applied.

## Implemented behavior and files

### Domain
`OrderHub.Domain/Models/Client.cs` now trims name, street, country code and national number. Contact inputs are validated before existing contact objects are mutated, preventing invalid street changes from partially changing City and preventing failed Phone creation from leaving a null Number. Existing EntityName, Address and PhoneNumber rules remain authoritative. Location now has a private setter and an explicit normalization method. Replacement methods allow detaching shared contacts without persistence types in Domain. No unrelated Domain entity changed.

### Application use cases
- Reused existing `GetAllClientsQuery`, `GetAllClientsPagedQuery`, `GetClientsByNameQuery`, `GetClientByIdQuery`, `GetClientsInfoQuery`, `GetClientEditQuery`, `CreateClientCommand`, `UpdateClientCommand`, and `DeleteClientCommand`.
- Added `GetEditorCities/GetClientEditorCitiesQuery.cs` and its handler. They expose existing minimal CityInfo (Id/Name), following the established setup lookup pattern. No new cross-feature contract or abstraction was extracted.
- Added feature-specific `ClientInputValidation.cs`: input nullability, EntityName validation, nonblank street/150 limit, positive and existing city, PhoneNumber rules/current country-code storage width, optional Location/255 limit. Invalid requests return Arabic business errors before staging changes.
- Create/update report persistence failures in Arabic. Update and delete report missing Client in Arabic. Edit read returns null for a missing target so the dialog can show a clear error.
- Expanded `Contracts/IClientRepository.cs` with Client-specific reference and contact-update operations. No use case/request shape was removed; no old global persistence layer was restored. No new type has a Dto suffix.

### Infrastructure and mutation safety
`OrderHub.Infrastructure/Features/Setup/Clients/ClientReader.cs` keeps untracked projected reads and orders list, paging, search and information results by Name then Id. A single projection retrieves list/edit data, without per-row queries.

`ClientRepository.cs` checks Orders and persisted drafts before deleting. PostgreSQL uses parameterized jsonb extraction at `Data.ClientId`; the SQLite test provider uses parameterized json_extract at the equivalent path. Checks scan all owners' drafts solely to return a boolean; draft content is not exposed.

Updates load the tracked Client with Address/City and Phone. Changed contacts are checked for sharing by other Clients, Suppliers and ShippingCarriers. Exclusive contacts keep their IDs; a changed shared contact gets a replacement row for this Client, leaving the other party's row intact. Unchanged shared contacts keep their IDs. Deletion removes only the Client and deliberately retains Address/Phone rows, because the mapping does not establish exclusive ownership.

Each mutation invokes one SaveChanges through the existing unit of work; EF supplies its atomic save transaction. A database-trigger test verifies failed contact updates roll back the complete profile. No extra broad transaction or generic CRUD infrastructure was introduced.

### Historical records and relationship graph
```text
Client -> required Address -> required City
Client -> required Phone
Order -> Client (required, Restrict)
Order -> OrderItems / OrderDeliverySteps
Invoice -> Order (Restrict), plus customer snapshots
Quotation / ProformaInvoice -> optional Order (Restrict), or draft reference
DraftRecord.Data -> Draft.Data.ClientId + ClientName (JSON, no Client FK)
```
Orders intentionally display the live Client directory details as they already did before this feature. Updating Client does not issue updates to Orders, items, delivery history, document snapshots or draft JSON. Tests verify the order ClientId, item name snapshot, delivery row and customer snapshots in invoice, quotation and proforma remain intact. This task does not invent order snapshots or rewrite historical records.

Client deletion is blocked with `لا يمكن حذف العميل لأنه مرتبط بطلبات أو مسودات طلبات.` when referenced. Direct SQL deletion of a Client with Orders also fails under the existing Restrict FK, preserving the complete history. Documents without Orders already carry independent customer snapshots and no ClientId; matching by a non-unique name/phone would invent an unsafe association, so no such check was added.

### Web UI
New independent files:
- `OrderHub.Web/Components/Pages/Features/Clients/Index.razor`
- `OrderHub.Web/Components/Pages/Features/Clients/_Imports.razor`
- `OrderHub.Web/Components/Pages/Features/Clients/Editor/ClientEditorDialog.razor`
- `OrderHub.Web/Components/Pages/Features/Clients/Editor/ClientEditorState.cs`

The `/clients` page follows the established Arabic/RTL MudTable pattern with compact name, phone and address columns; Location remains editable/searchable without cluttering the list. Search covers name, phone, city/street and Location. Loading, retryable errors, empty results, paging, write-only actions and delete confirmation are implemented. Success refreshes the page's data through IRequestExecutor without an application reload.

One unified create/edit MudDialog owns Client-specific state, loads edit data/lookups through IRequestExecutor, validates through MudForm.ValidateAsync, rechecks required city/street/location, guards double submission, and prevents cancel, escape, backdrop and close-button dismissal while saving. It closes with success only after persistence succeeds. City/phone fields use responsive MudGrid and the dialog uses natural sizing, without an artificial height/scroll container. No new CSS, Bootstrap, Tailwind or global DataGrid conversion was added. Razor attributes/workflow methods and Arabic strings were manually reviewed; HTML-rendering tests validate the interactions. Physical viewport/browser rendering at 1366×768 was not exercised in this run.

### Authorization
All nine pre-existing Client requests retain typed Clients permissions. The new editor-city request requires Clients.Write; it never executes a Cities.Read request. List/edit/info requests require Clients.Read; create/update/delete require Clients.Write. The page requires Clients.Read and shows writer actions only after the existing permission service authorizes Clients.Write. Tests exercise actual permission pipelines with Read/Write users holding no Cities permission.

The shared metadata inventory test in `OrderHub.Tests/Infrastructure/AuthorizationIntegrationTests.cs` received only a one-line count change from 92 to 93 for the one new permission-decorated request. Its assertions/exemptions remain intact. The unrelated failing authorization tests were not changed. Authentication/current-user/admin behavior was not modified.

### Migration review
No migration was added, edited or applied. No EF configuration or snapshot/designer file changed. Inspection confirmed the existing `fk_orders_clients_client_id` uses Restrict in both OrderConfiguration and the current model snapshot; required Client contact FKs and Address→City also use Restrict. Current-model reference/FK tests and direct SQL rejection verify history cannot cascade from Client deletion. Existing legacy messaging deletion migrations remain untouched. Since no schema change is needed, no Up/Down or migration SQL is generated.

## Verification

Build/test commands used `--no-restore -p:OutputPath=bin/ClientsVerification/`. The standard output directory was locked by the running Web app, so a separate ignored output folder was used without stopping that app. An attempted isolated restore was blocked by access to the user's NuGet.Config; the existing restored assets worked with the separate output path.

| Check | Result |
|---|---|
| `dotnet build OrderHub.sln` with verification output | Passed; final incremental build 0 warnings, 0 errors |
| Focused Clients filter | 42 passed, 0 failed, 0 skipped (41 new cases + 1 existing draft restoration case) |
| Architecture + shared request-metadata inventory | 17 passed, 0 failed, 0 skipped |
| Full suite before changes | 417 total: 409 passed, 3 failed, 5 skipped |
| Full suite after changes | 458 total: 450 passed, 3 failed, 5 skipped |
| New failure comparison | None; exact failing-test names match baseline |
| `git diff --check` | Passed |

Earlier non-incremental builds emitted existing Blazor/MudBlazor analyzer warnings in unrelated components/tests; there were no Client compilation errors in the final verification.

Added test cases:
- `OrderHub.Tests/Domain/ClientTests.cs`: 9 cases, construction/normalization/invariant rejection and unchanged state after invalid updates.
- `OrderHub.Tests/Infrastructure/ClientFeatureTests.cs`: 13 cases, list/edit/paging/search/lookups, deterministic ordering, missing records, duplicate behavior, invalid mutations, exclusive IDs, shared-contact isolation, atomic rollback, safe deletion and untouched order/document/draft history.
- `OrderHub.Tests/Infrastructure/ClientPermissionsTests.cs`: 2 cases, Read/Write permissions and independence from Cities.Read.
- `OrderHub.Tests/Infrastructure/ClientArchitectureTests.cs`: 2 cases, all Client metadata, EF-independent Application, persistence-free Web injection and independent editor state.
- `OrderHub.Tests/Web/ClientWebTests.cs`: 15 cases, permissions, search including Location/phone, empty state, create/edit, validation/retry, missing target/no cities, success-only refresh, deletion confirmation, route/policy, recoverable exceptions and submission/cancel guards.

Pre-existing failures (unchanged):
- `OrderHub.Tests.Infrastructure.ProductPropertiesTests.Migration_changes_only_the_order_property_delete_action`
- `OrderHub.Tests.Web.PermissionAuthorizationTests.Application_denial_maps_to_generic_HTTP_403`
- `OrderHub.Tests.Web.PermissionAuthorizationTests.HTTP_permission_policy_denial_returns_403_without_invoking_endpoint`

The two HTTP permission tests fail on their existing response expectations; the migration test expects a single FK addition while the historical migration already has multiple additions. None was fixed opportunistically.

Existing optional PostgreSQL skips (unchanged):
- `OrderHub.Tests.Infrastructure.PostgresCategoryTests.PostgreSQL_ancestry_query_rejects_deep_cycle_and_allows_other_branch`
- `OrderHub.Tests.Infrastructure.PostgresCategoryTests.PostgreSQL_browser_uses_one_query_for_recursive_counts_and_breadcrumbs`
- `OrderHub.Tests.Infrastructure.PostgresCategoryTests.PostgreSQL_concurrent_opposite_moves_cannot_create_a_cycle`
- `OrderHub.Tests.Infrastructure.PostgresPropertyTests.PostgreSQL_counts_in_one_read_and_updates_option_identity_atomically`
- `OrderHub.Tests.Infrastructure.PostgresPropertyTests.PostgreSQL_restricts_order_references_instead_of_cascading_history`

Raw build logs and TRX evidence are retained under ignored `TestResults/Clients/`, including `baseline/clients-baseline.trx`, `focused/clients-focused.trx`, `architecture/clients-architecture.trx`, and `full/clients-full.trx`.

## Deferred concerns and explicit limits
- Orders currently display live Client contact details. If immutable order-time customer snapshots are desired, that requires a separate domain/schema/product decision; this feature preserves established behavior.
- PhoneNumber permits a country code with four digits after `+`, whereas the database permits four total characters. This feature respects the current storage constraint; widening the shared phone schema is deferred.
- Persisted drafts have no relational Client FK. Existing references are protected by the narrow JSON check, but a simultaneous draft save after the delete check can still retain a now-missing ClientId. Guaranteeing integrity across concurrent draft writes requires a coordinated draft persistence change; no cross-feature/schema redesign is inferred here. Orders remain protected by the database even under concurrent writes.
- No disposable PostgreSQL test server was configured; the PostgreSQL JSON reference-check branch was reviewed but not exercised against a live PostgreSQL server. SQLite exercises the complete mutation/reference behavior and direct FK enforcement.
- Historical document snapshots do not have Client identity. A deleted Client with no Orders/drafts can have independently stored, name-matching snapshot documents; those snapshots remain intact and are not guessed to be references.
- Contact rows are intentionally retained after Client deletion. Any cleanup policy must establish actual exclusive ownership first.

## Final Git state and scope
The branch remains `feature/web-app` at the inspected revision. Existing modified production files belong only to Clients in Domain/Application/Infrastructure; new Web/Application/test files are Client-specific. The only edit outside the Client file paths is the necessary one-line authorization request inventory count. No unrelated feature implementation, dependency, authentication code, EF migration, configuration or snapshot changed.

Final status (before writing this final report content; report is the same already-untracked file):
```text
 M OrderHub.Application/Features/Setup/Clients/Contracts/IClientRepository.cs
 M OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommandHandler.cs
 M OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommandHandler.cs
 M OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommandHandler.cs
 M OrderHub.Domain/Models/Client.cs
 M OrderHub.Infrastructure/Features/Setup/Clients/ClientReader.cs
 M OrderHub.Infrastructure/Features/Setup/Clients/ClientRepository.cs
 M OrderHub.Tests/Infrastructure/AuthorizationIntegrationTests.cs
?? OrderHub.Application/Features/Setup/Clients/ClientInputValidation.cs
?? OrderHub.Application/Features/Setup/Clients/GetEditorCities/
?? OrderHub.Tests/Domain/ClientTests.cs
?? OrderHub.Tests/Infrastructure/ClientArchitectureTests.cs
?? OrderHub.Tests/Infrastructure/ClientFeatureTests.cs
?? OrderHub.Tests/Infrastructure/ClientPermissionsTests.cs
?? OrderHub.Tests/Web/ClientWebTests.cs
?? OrderHub.Web/Components/Pages/Features/Clients/
?? reports/

```
The index is empty (`git diff --cached --stat` produced no changes). Nothing was staged, committed, pushed, reset, rebased or switched to another branch. Build/test artifacts are ignored.

Suggested Conventional Commit message only:
```text
feat(clients): add independent client editor and safe contact management
```

