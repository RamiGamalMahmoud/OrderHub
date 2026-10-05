# OrderHub — P2 Persistence Cleanup & Verification

**P2 status: COMPLETE.**

The supported production solution builds with zero warnings/errors. Final focused validation has **171 passed, 0 failed, 0 skipped**. The complete supported suite has **184 passed, 1 failed, 0 skipped, 185 total**. The sole failure is the accepted, unchanged `Handle_Should_SyncMultiplePermissions` failure. **New P2 failures: 0.**

No commit or staging operation was performed. No later migration phase was started.

## A. Baseline

- The working tree and index were clean before P2.
- HEAD was `d226d0de6dc1c260a95b898176c892fc342250b2`, `refactor(persistence): move legacy repositories out of stores folder`.
- The preceding P1 migration commit was `b07a64b`, `refactor(persistence): migrate features to application-owned persistence`.
- The solution contains the four supported production projects: Domain, Application, Infrastructure and Web. Tests reference those supported projects. Project references were not changed in P2.
- Initial production build: **passed, 0 warnings, 0 errors**.
- Initial complete test attempt: **could not compile**, so no baseline test results were produced at that point. Four test files still contained seven imports of the removed `Application.Interfaces.Stores` / `Infrastructure.Stores` namespaces, with a resulting unresolved `IProformaInvoiceRepository` reference. This existed before P2 production changes and followed the already-committed repository relocation.

The obsolete imports were repaired in `OrderWriteArchitectureTests`, `DraftCompletionLifecycleTests`, `OrderCreateFlowTests` and `OrderUpdateFlowTests`. Only imports changed; no assertions or behavior were changed. The additional baseline issue was documented before production cleanup proceeded.

After that import-only repair, the full baseline was **182 passed, 1 failed, 0 skipped, 183 total**. The failure was the accepted permission synchronization test. This repaired baseline was established before deleting production persistence components.

The prior P1 report was treated as a candidate list, not as deletion authority. For example, `ProductReadService` was already deleted in HEAD `d226d0d`; P2 verified its absence rather than claiming or recreating its deletion.

## B. Removed legacy persistence

All reference searches covered supported Domain/Application/Infrastructure/Web source and Tests, including fully qualified names, namespace imports and serialized/reflection references where relevant. DI registrations and assembly discovery were inspected separately. Unsupported historical projects were not treated as consumers.

| Removed component | Previous purpose | Why safe; zero-consumer evidence |
|---|---|---|
| Global `Interfaces.Repositories.ISupplierRepository` | Earlier Supplier write/load port | References were limited to its own declaration, old implementation and registration. Active handlers import the distinct feature-owned Setup/Suppliers contract. No tests used the old port. |
| `Infrastructure.Reopsitories.SupplierRepository` | Old global Supplier implementation | Only its old registration selected it. Active Supplier behavior is covered by the P1 Setup persistence tests; the active feature implementation remains unchanged. |
| Global `Interfaces.Repositories.IDeliverymanRepository` | Earlier Deliveryman write/load port | Only its declaration, old implementation and registration referred to this fully qualified contract. Active handlers use Setup/Deliverymen's contract. No implementation-specific test depended on the old one. |
| `Infrastructure.Reopsitories.DeliverymanRepository` | Old global Deliveryman implementation | No supported caller beyond its obsolete registration; the feature-owned replacement remains unchanged and its behavior tests pass. |
| `RepositoryBase<TModel>` | Base class for the two old implementations | After those implementations were removed, the only remaining source matches were its declaration/constructor. No registration, reflection discovery or test required it. No replacement generic framework was added. |
| `DTOs.AddressDtos` / nested `AddressListDto` | Global address selection wrapper | Definition-only matches, no active query/result contract, serializer reference, DI entry or test. Current feature-owned address data remains in active Client/carrier/supplier results. |
| `DTOs.CommonDtos` / nested `AddressInfoDto`, `DeliveryMethodInfoDto` | Residual global lookup wrappers | No supported consumers or discovery registrations. Active reference-info/editor lookup paths use their current contracts/models. |
| `DTOs.OrderStatusesDtos` | Empty global wrapper | Empty declaration with no consumer or runtime role. Active enum/status models remain unchanged. |
| `DTOs.ClientCredentials` | Orphan JSON credential data model | No constructor, deserializer, endpoint, interface or test consumed it. It is unrelated to the supported Database Login command/session path, which was untouched. |
| `DTOs.Documents.DeliveryNoteData` / `DeliveryNoteDataItem` | Unused document rendering payload | Only mutual declarations existed. The PDF factory supports Invoice, Quotation and Proforma payloads, with no Delivery Note renderer/caller. Active shared document models were retained. |
| `Features.Orders.Get.GetOrder` / nested Query, result and Handler | Unimplemented Order query placeholder | Only its declaration and automatic Application MediatR discovery existed; no supported dispatch/caller. Active details/editor queries supersede the read role. No missing feature was implemented. |
| `Features.Orders.Queries.GetOrdersSummary` / nested `Qeury` | Dormant query placeholder | No handler, request contract or caller; definition-only matches. Active Order page/details queries remain. |
| `PropertyCreateInput` | Unused Property creation model | Definition-only match. The active `CreatePropertyCommand` carries the required input directly and is covered by existing tests. |
| `IInvoiceService` | Orphan invoice report-generation port | No implementation, registration or supported caller. Active invoice generation uses the existing Application document use case, PDF factory and file port. |
| `Common.Lookups.AddressLookup` | Old broad-lookup address model | No outside consumer; its only association was with the obsolete City lookup model. |
| `Common.Lookups.CityLookup` | Old broad city lookup result | Referenced only by the obsolete Address lookup declaration and its own declaration. Current City feature results remain. |
| `Common.Lookups.CategoryLookup` | Old broad category lookup result | Referenced only by the obsolete Product lookup declaration and its own declaration. Current category feature results remain. |
| `Common.Lookups.SupplierLookup` | Old broad supplier selection result | Definition-only match. Current Supplier feature results and Product supplier lookup models remain. |
| `Common.Lookups.ProductLookup` | Old generic product/category lookup result | Definition-only match; active product lookup contracts use `ProductLookupItem`. |
| `Common.CacheKeys` | Keys/helper names for the earlier query-cache paths | No supported class, constant, static-import, helper-call or cache registration consumed it. No new cache was introduced. |

These are **20 deleted files**, including closed clusters of mutually referencing dead models. No behavior test was deleted.

`ProductReadService` required no P2 deletion: current source, tracked-file search and commit history confirm it was already absent. Its disposed-context `IQueryable` API was not adopted by any current reader.

## C. Retained persistence

The six global repositories now reside under `Application.Interfaces.Repositories` and `Infrastructure.Reopsitories`, following the commit made before P2. Their behavior and organization were not changed again.

| Retained abstraction | Active purpose/consumers |
|---|---|
| `IOrderRepository` | Domain Order creation, update, deletion, numbering, payment-method changes, status transitions and invoice creation. Commands require aggregate behavior. |
| `IInvoiceRepository` | Invoice creation and existing-invoice handling, including document generation. |
| `IDocumentSequenceRepository` | Atomic number reservation for Invoice, Quotation and Proforma creation, within existing transaction semantics. |
| `IQuotationRepository` | Quotation persistence and draft-document linking during Order creation. |
| `IProformaInvoiceRepository` | Proforma persistence and draft-document linking during Order creation. |
| `IUserRepository` | Login and User/Admin commands. User/Permission persistence was deliberately left stable. |
| `IDraftRepository` | Actor-owned draft Save/Delete and completion during Order creation. |
| `IDraftReadService` | Ownership-filtered Get/List Draft queries. |
| `IUserDetailsService`, `IUserListingService` | Purpose-specific User read operations; not generic repository/store APIs. |
| P1 feature readers and repositories | Cohesive feature-owned projection/domain persistence capabilities, with active Application handlers. |
| `IUnitOfWork` | Existing operation-scoped commit/transaction boundary shared with write repositories. |

Active Invoice/Quotation/Proforma PDF payloads and `DocumentCustomer` remain genuinely shared between Application document use cases and Infrastructure rendering. Active command wrappers in document generation and Order deletion were retained rather than cosmetically split/renamed. Existing shared enum, paging and editor lookup models also remain where consumed.

## D. Dependency injection cleanup

Removed registrations:

- Scoped old global `IDeliverymanRepository → Reopsitories.DeliverymanRepository`.
- Scoped old global `ISupplierRepository → Reopsitories.SupplierRepository`.
- Infrastructure MediatR assembly registration/scanning.

Removed the `System.Reflection` import that existed solely for Infrastructure MediatR scanning.

No registration helper was deleted unnecessarily. `AddRepositories` still registers six active domain repositories. `AddPersistence`, `AddSetupPersistence`, `AddCatalogPersistence`, `AddOrderReads`, `AddDrafts` and `AddServices` remain meaningful. `AddStores` and `AddReadServices` were already absent before P2.

All remaining database persistence contracts were audited. The architecture test enumerates the Application's reader/repository/unit-of-work and specialized Draft/User read contracts, requires exactly one registration for every contract, checks the intended lifetime, and resolves the actual Infrastructure implementation through production composition with the isolated test factory. This covers **31 database persistence contracts**.

Preserved lifetimes:

- P1 readers/repositories and retained domain repositories: scoped.
- Draft repository and unit of work: scoped.
- Draft read service: singleton, holding only the factory.
- User detail/listing services and their shared concrete read service: transient, holding only the factory.
- Context factory: singleton; operation context: scoped.
- Existing file/document/database-maintenance services: their existing singleton lifetimes.
- Web `IRequestExecutor`: scoped `AuthenticatedRequestExecutor`.

**Duplicate persistence registrations: none.** No service relies on registration order to select competing implementations. No lifetime was changed, and no singleton holding a scoped context was introduced.

## E. Infrastructure MediatR audit

Supported Infrastructure has no `IRequestHandler<,>`, `IRequestHandler<>`, `INotificationHandler<>`, stream handler or pipeline implementation. Source searches and reflection-based architecture checks verify this independently of folder names.

Infrastructure assembly scanning was removed because it had no handler to discover. A new regression test verifies that `AddInfrastructure` alone registers neither `IMediator` nor MediatR use-case handlers.

Application's existing assembly scan and MediatR use cases remain unchanged. EF Core's separate configuration assembly discovery in `AppDbContext` remains unchanged; it serves model configuration, not use-case dispatch.

## F. Dead-code audit

| Area | Finding/result |
|---|---|
| Stores | No supported production `IProductStore`, `ProductStore`, `IPropertyStore`, `PropertyStore`, `ILookupService` or `LookupService` remains. No production Store abstraction/framework remains in the migrated paths. |
| Repositories | Deleted only the old global Supplier/Deliveryman ports/implementations and their base. Retained active domain and feature repositories. Identical short names were distinguished by full namespace ownership. |
| ReadServices | `ProductReadService` was already removed upstream. Current read implementations are feature-owned; the User read implementation is actively consumed. |
| Global DTO/wrappers | Removed unused global wrappers/models and closed obsolete lookup clusters. Kept active shared document payloads. No mass DTO rename or namespace normalization. |
| Dormant query placeholders | Removed unimplemented `GetOrder` and handlerless `GetOrdersSummary`; active Order reads remain. |
| DI helpers | Removed stale scan/import and two registrations. All remaining helpers register meaningful current services. |
| Dead namespace references | Repaired four test files' old Stores imports; removed two unused Razor imports of the now-empty legacy Orders Queries namespace. These are the only Web edits. |
| Text matches containing “Store” | Test namespaces/fixture helper names, `Storefront` icons, ordinary comments and `_productStore` variables typed as `IProductReader` are not legacy persistence abstractions. They were not cosmetically renamed. |

No obsolete implementation-specific tests required deletion or migration. Existing Setup tests already protect active Supplier/Deliveryman behavior; Order/document/draft tests and their assertions were preserved.

## G. Architecture verification

| Requirement | Result |
|---|---|
| Domain persistence-agnostic | Confirmed by source/project inspection and assembly-reference checks. No Domain file changed. |
| Application EF-independent | Confirmed. No EF/Npgsql/Infrastructure/Web/ASP.NET dependency was introduced, and contracts do not expose context/query implementation APIs. |
| Infrastructure owns implementation | Confirmed; active EF readers, repositories, factories, unit of work and model configuration remain there. |
| Web has no direct database persistence dependency | Confirmed by source searches and a new test inspecting constructor, field and property dependencies, including generic/array dependencies. Composition-root Infrastructure registration/options references remain legitimate. |
| No generic repository framework introduced | Confirmed; the obsolete base was deleted without replacement. |
| No active legacy Store architecture | Confirmed by source/type searches and regression tests. |
| One executor implementation/registration | Confirmed: scoped Web `AuthenticatedRequestExecutor`, unchanged. |
| Current User/authentication/authorization stable | Confirmed. No identity, login/cookie/session validation, authorization or Permission source changed. |
| Ownership and unit-of-work behavior stable | Confirmed. Draft, active feature persistence and transaction implementations are unchanged; existing tests pass. |

Final supported-production searches have zero matches for removed store/lookup abstractions, `RepositoryBase`, `ProductReadService`, the dead model/wrapper names and Infrastructure MediatR handler/scanning declarations. Only the feature-owned Supplier/Deliveryman contracts remain; the old global contracts are absent.

## H. Database changes

**None.**

No EF migrations, schema/table/column/index/constraint/relationship changes, seed changes, production data migration or cleanup occurred. Production context/factory/configuration/migration files are unchanged. Tests retain the existing isolated SQLite setup. No production database or maintenance operation was executed.

## I. Behavior changes

**No intentional business behavior changes.**

No validation, calculation, Order/document/draft behavior, delete rule, status transition, error message, file behavior, Telegram/WhatsApp integration, authentication or authorization behavior changed. No UI visual/layout/navigation change was made. Razor changes remove only unused imports.

The known UserPermission synchronization defect, equality and persistence remain unchanged. P2 did not add Roles, permission levels/templates/policies/caches, session revalidation or a new persistence architecture.

## J. Validation

Logical cleanup groups were validated independently:

| Group | Production build | Focused result |
|---|---|---|
| Old global Supplier/Deliveryman repositories | Passed | 18 passed, 0 failed |
| Now-unused repository base | Passed | 9 passed, 0 failed |
| Infrastructure MediatR scan | Passed | 32 passed, 0 failed |
| Dead wrappers/models and related imports | Passed after removal of two stale namespace imports | 169 passed, 0 failed |
| Expanded architecture/DI/Web boundary checks | Passed | 27 passed, 0 failed |
| Remaining dead lookup/cache declarations | Passed | 171 passed, 0 failed |

Final results:

| Validation | Result |
|---|---|
| Supported production build | **PASS — 0 warnings, 0 errors** |
| Focused persistence/architecture/Web/Property tests | **171 passed, 0 failed, 0 skipped** |
| Complete supported suite | **184 passed, 1 failed, 0 skipped; 185 total** |
| Known pre-existing failure | `Handle_Should_SyncMultiplePermissions` only |
| New P2 failures | **0** |
| `git diff --check` | **PASS** |

The suite grew from 183 to 185 test cases; no test was removed. Existing Blazor component-test analyzer warnings remain; final validation did not suppress them.

The known failure concerns multiple unsaved `UserPermission` entities sharing ID `0` while existing synchronization uses ID-based equality. It was present in the repaired baseline, was not caused by P2, and is expressly accepted for this phase.

Final commands:

```powershell
dotnet build OrderHub.sln --no-restore --verbosity quiet
dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-restore --verbosity quiet --filter "FullyQualifiedName~OrderHub.Tests.Infrastructure|FullyQualifiedName~OrderHub.Tests.Web|FullyQualifiedName~PropertyTests" --logger "trx;LogFileName=P2-focused.trx"
dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-build --no-restore --verbosity quiet --logger "trx;LogFileName=P2-final.trx"
git diff --check
```

Results are kept only in the ignored test-results directory:

- [Repaired baseline](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/TestResults/P2-repaired-baseline.trx)
- [Focused validation](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/TestResults/P2-focused.trx)
- [Complete validation](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/TestResults/P2-final.trx)

Validation uses SQLite and registration/reflection/source checks, not a live PostgreSQL integration environment. P2 changes no read/write query implementation, schema or provider behavior.

## K. Final persistence inventory

### Feature-owned readers and read ports

| Contract | Infrastructure implementation | Lifetime | Application consumers | Registration group |
|---|---|---|---|---|
| Setup/Cities `ICityReader` | `CityReader` | Scoped | City list/edit/info queries | `AddSetupPersistence` |
| Setup/Categories `ICategoryReader` | `CategoryReader` | Scoped | Category list/tree/edit/ancestry/info queries | `AddSetupPersistence` |
| Setup/Clients `IClientReader` | `ClientReader` | Scoped | Client list/paged/edit/search/single/info queries | `AddSetupPersistence` |
| Setup/Deliverymen `IDeliverymanReader` | `DeliverymanReader` | Scoped | Deliveryman list/edit/search/single/info queries | `AddSetupPersistence` |
| Setup/ShippingCarriers `IShippingCarrierReader` | `ShippingCarrierReader` | Scoped | Carrier list/edit/search/single/info queries | `AddSetupPersistence` |
| Setup/Suppliers `ISupplierReader` | `SupplierReader` | Scoped | Supplier list/edit/info queries | `AddSetupPersistence` |
| Setup/PaymentMethods `IPaymentMethodReader` | `PaymentMethodReader` | Scoped | Payment method list | `AddSetupPersistence` |
| Setup/Properties `IPropertyReader` | `PropertyReader` | Scoped | Property detail/list | `AddCatalogPersistence` |
| Products `IProductReader` | `ProductReader` | Scoped | Product detail/list/search/info and category lookup capabilities | `AddCatalogPersistence` |
| Orders `IOrderReader` | `OrderReader` | Scoped | Order page/details/editor restoration | `AddOrderReads` |
| Orders `IOrderItemEditorReader` | `OrderItemEditorReader` | Scoped | Single/batch item-editor metadata | `AddOrderReads` |
| Orders `IOrderEditorLookupReader` | `OrderEditorLookupReader` | Scoped | Application editor-lookup query | `AddOrderReads` |
| OrderDrafts `IDraftReadService` | `DraftReadService` | Singleton, factory-only | Get/List Draft | `AddDrafts` |
| Administration `IUserDetailsService` | `UserReadService` | Transient, factory-only | Get User | Infrastructure composition root |
| Administration `IUserListingService` | `UserReadService` | Transient, factory-only | Get Users | Infrastructure composition root |

### Feature-owned repositories

| Contract ownership | Implementation | Lifetime | Consumers | Registration group |
|---|---|---|---|---|
| Setup/Cities `ICityRepository` | `CityRepository` | Scoped | City commands and city state needed by Client/Supplier/Carrier/Deliveryman commands | `AddSetupPersistence` |
| Setup/Categories `ICategoryRepository` | `CategoryRepository` | Scoped | Category commands | `AddSetupPersistence` |
| Setup/Clients `IClientRepository` | `ClientRepository` | Scoped | Client commands | `AddSetupPersistence` |
| Setup/Deliverymen `IDeliverymanRepository` | Feature-owned `DeliverymanRepository` | Scoped | Deliveryman commands | `AddSetupPersistence` |
| Setup/ShippingCarriers `IShippingCarrierRepository` | `ShippingCarrierRepository` | Scoped | Carrier commands | `AddSetupPersistence` |
| Setup/Suppliers `ISupplierRepository` | Feature-owned `SupplierRepository` | Scoped | Supplier commands | `AddSetupPersistence` |
| Setup/Properties `IPropertyRepository` | `PropertyRepository` | Scoped | Property commands | `AddCatalogPersistence` |
| Products `IProductRepository` | `ProductRepository` | Scoped | Product commands | `AddCatalogPersistence` |
| OrderDrafts `IDraftRepository` | `DraftRepository` | Scoped | Save/Delete Draft and Create Order draft completion | `AddDrafts` |

### Retained domain repositories

All six contracts are under `OrderHub.Application.Interfaces.Repositories`; implementations are under `OrderHub.Infrastructure.Reopsitories`. Each has one scoped registration in `AddRepositories`:

| Contract | Implementation | Consumers |
|---|---|---|
| `IOrderRepository` | `OrderRepository` | Order create/update/delete/status/payment commands; Invoice creation |
| `IUserRepository` | `UserRepository` | Login and User/Admin commands |
| `IInvoiceRepository` | `InvoiceRepository` | Invoice creation/existing invoice handling |
| `IDocumentSequenceRepository` | `DocumentSequenceRepository` | Invoice/Quotation/Proforma number reservation |
| `IQuotationRepository` | `QuotationRepository` | Quotation creation and Order draft linking |
| `IProformaInvoiceRepository` | `ProformaInvoiceRepository` | Proforma creation and Order draft linking |

### Unit of work and context infrastructure

- `IUnitOfWork → UnitOfWork`: scoped, registered in `AddPersistence`; consumed by write use cases.
- `AppDbContext`: scoped operation context, created by the factory; shared by repositories and unit of work.
- `AppDbContextFactory`: singleton; operation creation and isolated read contexts.
- Existing design-time context factory and EF configuration assembly discovery: retained for EF tooling/model construction, with no runtime redesign.

### Specialized persistence/integration services

Existing `AddServices` registrations remain:

| Contract | Implementation | Lifetime | Role/consumers |
|---|---|---|---|
| `ILocalFileAccessService` | `LocalFileAccessService` | Singleton | Document save operations and four file-download controllers |
| `IApplicationDirectoriesService` | `ApplicationDirectoriesService` | Singleton | File-access and log directory resolution |
| `IPdfDocumentFactory` | `PdfDocumentFactory` | Singleton | Active Invoice/Quotation/Proforma rendering |
| `IDatabaseService` | `DatabaseService` | Singleton, factory-only | Specialized connectivity/migration/maintenance facade; no current feature caller found, explicitly classified in section L |
| `IAppLogger` | `FileAppLogger` | Singleton | Existing file-logging integration, explicitly classified in section L |

Authentication hashing/authorization services are not database persistence abstractions and their registrations remain unchanged.

## L. Remaining cleanup and explicit classifications

- **Database maintenance facade:** `IDatabaseService`/`DatabaseService` has no current feature caller, but is an existing registered, specialized operational capability with a clear connectivity/migration/maintenance purpose. It has no P1 feature replacement and is not mixed CRUD Store infrastructure. It was retained under the allowance for a clear architectural purpose, rather than speculatively removing administrative capability. Its maintenance methods were not executed or modified. Whether to expose or retire that operational facade is a separate product/operations decision.
- **File logger:** no current Application feature caller of `IAppLogger` was found. It remains an existing configured file-logging integration, outside database persistence cleanup; removing/redesigning file behavior is expressly out of scope. No new logging layer was created.
- **Active document/Order wrappers and shared payloads:** retained because they have real supported consumers. Cosmetic file splitting, DTO terminology normalization and namespace/folder spelling changes were not performed.
- **Store-like variable/test names:** retained because they are not Store contracts or implementations. Tests may still use the `Infrastructure.Stores` namespace for historical organization; production code has no such legacy persistence layer.
- **Known Category info presentation/permission defects:** not fixed. They concern business behavior or the later Authorization phase, not obsolete persistence removal.
- **Non-persistence helpers/packages:** not subjected to unrelated dead-code or package cleanup. No project/package reference change was required for this phase.

No unexplained active dependency on the obsolete Store architecture remains. No unresolved P2 regression or stop condition remains.

## Git and changed files

The following snapshot records the complete visible P2 changes. The report itself is a new untracked artifact. Generated test results are ignored, not staged or committed.

### Final Git status

```text
 D OrderHub.Application/Common/CacheKeys.cs
 D OrderHub.Application/Common/Lookups/AddressLookup.cs
 D OrderHub.Application/Common/Lookups/CategoryLookup.cs
 D OrderHub.Application/Common/Lookups/CityLookup.cs
 D OrderHub.Application/Common/Lookups/ProductLookup.cs
 D OrderHub.Application/Common/Lookups/SupplierLookup.cs
 D OrderHub.Application/DTOs/AddressDtos.cs
 D OrderHub.Application/DTOs/ClientCredentials.cs
 D OrderHub.Application/DTOs/CommonDtos.cs
 D OrderHub.Application/DTOs/Documents/DeliveryNoteData.cs
 D OrderHub.Application/DTOs/OrderStatusesDtos.cs
 D OrderHub.Application/Features/Orders/Get/GetOrder.cs
 D OrderHub.Application/Features/Orders/Queries/GetOrdersSummary.cs
 D OrderHub.Application/Features/Setup/Properties/Create/PropertyCreateInput.cs
 D OrderHub.Application/Interfaces/Repositories/IDeliverymanRepository.cs
 D OrderHub.Application/Interfaces/Repositories/ISupplierRepository.cs
 D OrderHub.Application/Interfaces/Services/IInvoiceService.cs
 M OrderHub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
 D OrderHub.Infrastructure/Reopsitories/DeliverymanRepository.cs
 D OrderHub.Infrastructure/Reopsitories/RepositoryBase.cs
 M OrderHub.Infrastructure/Reopsitories/ServiceCollectionExtensions.cs
 D OrderHub.Infrastructure/Reopsitories/SupplierRepository.cs
 M OrderHub.Tests/Infrastructure/OrderWriteArchitectureTests.cs
 M OrderHub.Tests/Infrastructure/PersistenceArchitectureTests.cs
 M OrderHub.Tests/Web/DraftCompletionLifecycleTests.cs
 M OrderHub.Tests/Web/OrderCreateFlowTests.cs
 M OrderHub.Tests/Web/OrderUpdateFlowTests.cs
 M OrderHub.Web/Components/Pages/Features/Orders/Index/Orders.razor
 M OrderHub.Web/Components/Pages/Features/Orders/OrderDetials.razor
?? P2-Result.md
```

### Git diff --stat

The tracked diff excludes this new Markdown report.

```text
 OrderHub.Application/Common/CacheKeys.cs           | 21 -----
 .../Common/Lookups/AddressLookup.cs                |  3 -
 .../Common/Lookups/CategoryLookup.cs               |  3 -
 OrderHub.Application/Common/Lookups/CityLookup.cs  |  3 -
 .../Common/Lookups/ProductLookup.cs                |  3 -
 .../Common/Lookups/SupplierLookup.cs               |  3 -
 OrderHub.Application/DTOs/AddressDtos.cs           |  9 ---
 OrderHub.Application/DTOs/ClientCredentials.cs     | 11 ---
 OrderHub.Application/DTOs/CommonDtos.cs            |  9 ---
 .../DTOs/Documents/DeliveryNoteData.cs             | 24 ------
 OrderHub.Application/DTOs/OrderStatusesDtos.cs     |  5 --
 .../Features/Orders/Get/GetOrder.cs                | 18 -----
 .../Features/Orders/Queries/GetOrdersSummary.cs    |  6 --
 .../Setup/Properties/Create/PropertyCreateInput.cs |  6 --
 .../Repositories/IDeliverymanRepository.cs         |  9 ---
 .../Interfaces/Repositories/ISupplierRepository.cs |  9 ---
 .../Interfaces/Services/IInvoiceService.cs         |  8 --
 .../ServiceCollectionExtensions.cs                 |  3 -
 .../Reopsitories/DeliverymanRepository.cs          | 21 -----
 .../Reopsitories/RepositoryBase.cs                 | 12 ---
 .../Reopsitories/ServiceCollectionExtensions.cs    |  2 -
 .../Reopsitories/SupplierRepository.cs             | 21 -----
 .../Infrastructure/OrderWriteArchitectureTests.cs  |  4 +-
 .../Infrastructure/PersistenceArchitectureTests.cs | 89 +++++++++++++++++++---
 .../Web/DraftCompletionLifecycleTests.cs           |  4 +-
 OrderHub.Tests/Web/OrderCreateFlowTests.cs         |  4 +-
 OrderHub.Tests/Web/OrderUpdateFlowTests.cs         |  2 +-
 .../Pages/Features/Orders/Index/Orders.razor       |  1 -
 .../Pages/Features/Orders/OrderDetials.razor       |  1 -
 29 files changed, 86 insertions(+), 228 deletions(-)
```

### Protected source verification

Git verification found no changes to Domain, active Infrastructure feature implementations, the production context/factory/unit of work, schema/configuration/migrations, User persistence, Current User, Authentication, Administration/Permissions, Draft use cases, Web identity/executor services or controllers. The index is clean.
