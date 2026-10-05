# OrderHub — P1 Persistence Migration Result

**Status: COMPLETE within the requested P1 scope.**

The supported production solution builds. The final focused suite passes all 169 tests. The complete supported suite has **182 passed, 1 failed, 0 skipped, 183 total**. The sole failure is the explicitly accepted, unchanged `Handle_Should_SyncMultiplePermissions` legacy failure. There are no new failing tests.

P1 stops here. No commit was created and no P2 cleanup phase was executed.

## A. Migration summary

Active legacy persistence consumers now use feature-owned read contracts or domain-aware write repositories. Database implementations remain in Infrastructure; use-case handlers, domain mutations and commit decisions belong to Application.

The migration order was:

1. Setup/reference features: Cities, Categories, Clients, Deliverymen, Shipping Carriers, Suppliers and Payment Methods. Cities provide the shared domain state needed by the contact/delivery features.
2. Properties and Products: separate catalog projections from aggregate mutation and move the existing domain operations out of stores.
3. Orders and editor lookups: introduce Application query handlers, purpose-specific readers and explicit projected editor data; adapt the existing Web consumers.

Commands use scoped repositories and the existing scoped `IUnitOfWork`, sharing the operation's `AppDbContext`. Each migrated command retains one commit for its logical write. Queries use read-only projections and propagate cancellation. Order editor collection reads use bounded split projections, followed by merging projected current and historical property data; they do not materialize the full Order/Product aggregate graph.

Previously compliant User, Draft, Order-write and document repository paths were retained rather than mechanically renamed or redesigned.

Migrated wrapper declarations were separated into meaningful command, query, handler and model files under their owning features. New model names describe their purpose, including `ProductCreateInput`, `ProductListItem`, `PropertyDetails`, `OrderListItem`, `OrderEditorData` and `OrderItemEditorData`. No new DTO-suffixed types were introduced.

## B. Features migrated

| Feature/capability | Old dependency | Current dependency | Classification |
|---|---|---|---|
| Cities | Infrastructure MediatR handlers accessing the factory/context directly | Application handlers; `ICityRepository` and `ICityReader` | Domain writes; projected reads |
| Categories | Infrastructure CRUD/query handlers, including reference-info handlers | Application handlers; `ICategoryRepository` and `ICategoryReader` | Domain writes; list/tree/edit/ancestry/info reads |
| Clients | Infrastructure CRUD, edit/list/search/paging/info handlers | Application handlers; `IClientRepository` and `IClientReader`; `ICityRepository` for write-side city state | Domain writes; projected reads |
| Deliverymen | Infrastructure CRUD/list/edit/search/info handlers | Application handlers; feature-owned `IDeliverymanRepository` and `IDeliverymanReader`; `ICityRepository` | Domain writes; projected reads |
| Shipping Carriers | Infrastructure CRUD/list/edit/search/info handlers | Application handlers; `IShippingCarrierRepository` and `IShippingCarrierReader`; `ICityRepository` | Domain writes; projected reads |
| Suppliers | Infrastructure CRUD/list/edit/info handlers | Application handlers; feature-owned `ISupplierRepository` and `ISupplierReader`; `ICityRepository` | Domain writes; projected reads |
| Payment Methods | Infrastructure list handler | Application list handler and `IPaymentMethodReader` | Projected read |
| Properties | Mixed `IPropertyStore` / `PropertyStore` | `IPropertyRepository`, `IPropertyReader`, Application create/update/delete handlers and existing unit of work | Domain writes; separate detail/list reads |
| Products | Mixed `IProductStore` / `ProductStore`, including domain mutation inside Infrastructure | `IProductRepository`, `IProductReader`, Application handlers; `SearchProductsQuery` for Web search | Domain writes; separate list/detail/search/category/info reads |
| Order page/details/editor restoration | Infrastructure MediatR query handlers | Application handlers and `IOrderReader` | Purpose-specific projected reads |
| Single/batch Order item editor metadata | Nested reader contract plus an Infrastructure batch MediatR handler | Standalone `IOrderItemEditorReader`, Application handlers and `OrderItemEditorReader` | Catalog metadata reads |
| Order editor lookup loading | Broad `ILookupService` / `LookupService` used directly by Web | `GetOrderEditorLookupsQuery`, its Application handler and `IOrderEditorLookupReader` | Four cohesive editor lookup reads |

The Product search dialog and Order details step now invoke Application queries through the existing `IRequestExecutor`. Web does not resolve persistence readers or repositories directly. UI layout, forms, filtering of inactive payment selections, retry behavior and editor state preservation were retained.

## C. Application persistence contracts

All new contracts have feature ownership and expose concrete operations/results rather than EF Core APIs or generic CRUD machinery.

| Contract | Purpose |
|---|---|
| `Features.Setup.Cities.Contracts.ICityRepository` | Find/add/remove domain City state; test existing address and deliveryman usage before deletion. |
| `Features.Setup.Cities.Contracts.ICityReader` | City list, edit and reference-info projections. |
| `Features.Setup.Categories.Contracts.ICategoryRepository` | Tracked Category state, duplicate-name/parent check and product/child dependency checks. |
| `Features.Setup.Categories.Contracts.ICategoryReader` | Category list, edit, tree, ancestry and reference-info projections. |
| `Features.Setup.Clients.Contracts.IClientRepository` | Tracked Client/address/phone state and Order dependency checks. |
| `Features.Setup.Clients.Contracts.IClientReader` | Client list, database-paged list, edit, search, single-item and info results. |
| `Features.Setup.Deliverymen.Contracts.IDeliverymanRepository` | Domain Deliveryman state and Order/delivery-chain dependency checks. |
| `Features.Setup.Deliverymen.Contracts.IDeliverymanReader` | Deliveryman list, edit, search, single-item and info results. |
| `Features.Setup.ShippingCarriers.Contracts.IShippingCarrierRepository` | Tracked carrier/address/phone state and Order/delivery-chain dependency checks. |
| `Features.Setup.ShippingCarriers.Contracts.IShippingCarrierReader` | Carrier list, edit, search, single-item and info results. |
| `Features.Setup.Suppliers.Contracts.ISupplierRepository` | Tracked supplier/address/phone state and Order-item/Product dependency checks. |
| `Features.Setup.Suppliers.Contracts.ISupplierReader` | Supplier list, edit and info projections. |
| `Features.Setup.PaymentMethods.Contracts.IPaymentMethodReader` | Payment method list projection, preserving active status and display data. |
| `Features.Setup.Properties.Contracts.IPropertyRepository` | Duplicate-name checks, tracked Property/options loading and domain entity add/remove. |
| `Features.Setup.Properties.Contracts.IPropertyReader` | Separate Property detail/options and list projections. |
| `Features.Products.Contracts.IProductRepository` | Tracked Product loading, required Category/Supplier domain state, Order usage checks and add/remove. |
| `Features.Products.Contracts.IProductReader` | Product list, detail, name/category lookup and reference-info projections. |
| `Features.Orders.Contracts.IOrderReader` | Order page, details and editor restoration data. |
| `Features.Orders.Contracts.IOrderEditorLookupReader` | Clients, payment methods, deliverymen and carriers needed by the editor. |
| `Features.Orders.GetItemEditor.IOrderItemEditorReader` | Extracted from the previous nested contract; single and batch catalog editor metadata now share this reader. |

The namespaces above are relative to `OrderHub.Application`. The extracted item-editor contract is a moved/extended capability; its single-item purpose already existed before P1.

No contract exposes `IQueryable`, `DbContext`, `DbSet`, EF entries, Include operations, SQL, Npgsql parameters or expression-tree query builders. Existing genuinely shared lookup models remain shared; new feature results stay near their owning use cases/contracts.

## D. Infrastructure implementations

| Implementation namespace | Implementations and responsibilities |
|---|---|
| `Features.Setup.Cities` | `CityRepository`: scoped domain persistence and usage checks. `CityReader`: list/edit/info projections. |
| `Features.Setup.Categories` | `CategoryRepository`: domain persistence, duplicates and dependency checks. `CategoryReader`: projected lists, tree assembly, edit/info reads and retained recursive ancestry SQL. |
| `Features.Setup.Clients` | `ClientRepository`: tracked write graph and usage checks. `ClientReader`: projected edit/list/search/info reads and database paging. |
| `Features.Setup.Deliverymen` | `DeliverymanRepository`: write state and usage checks. `DeliverymanReader`: projected selection/edit/list/info data. |
| `Features.Setup.ShippingCarriers` | `ShippingCarrierRepository`: tracked write graph and usage checks. `ShippingCarrierReader`: projected selection/edit/list/info data. |
| `Features.Setup.Suppliers` | `SupplierRepository`: tracked write graph and usage checks. `SupplierReader`: projected list/edit/info data. |
| `Features.Setup.PaymentMethods` | `PaymentMethodReader`: read-only payment method list. |
| `Features.Setup.Properties` | `PropertyRepository`: tracked Property/options state without business mutation or commits. `PropertyReader`: projected detail/list results. |
| `Features.Products` | `ProductRepository`: required tracked aggregate/reference state without business mutation or commits. `ProductReader`: projected list/detail/search/category/info results; existing recursive category SQL remains Infrastructure-owned. |
| `Features.Orders` | `OrderReader`: page/details and projected restoration reads. `OrderItemEditorReader`: single/batch product metadata. `OrderEditorLookupReader`: the four editor lookup projections. |

The namespaces above are relative to `OrderHub.Infrastructure`. Reads create and dispose contexts using the unchanged production factory. Write repositories use the existing operation-scoped context and leave commits to Application via the existing unit of work.

Read methods use `AsNoTracking` where applicable. Ancestry scalar SQL does not materialize tracked entities. Paging retains database filtering, sorting, count, skip and take. Tree assembly is deliberately performed after loading a projected category set, as before; it does not introduce per-node database requests.

## E. Removed legacy dependencies

Removed after identifying supported consumers and completing their replacements:

- `IProductStore` and `ProductStore`.
- `IPropertyStore` and `PropertyStore`.
- `ILookupService` and `LookupService`.
- Their obsolete `AddStores` / `AddReadServices` registration helpers. The surviving item-editor reader registration moved to the Orders registration group.
- Infrastructure MediatR CRUD handlers for Cities, Categories, Clients, Deliverymen, Shipping Carriers and Suppliers.
- Infrastructure setup/payment/reference-info query handlers.
- Infrastructure Order page/details/edit/batch-editor query handlers.
- Migrated command/query/model static wrappers and DTO declarations, replaced by feature-owned declarations. These declarations were moved/replaced, not discarded behavior.
- The old aggregate `MapOrder` helper: retained restoration tests now exercise the actual database projection and editor restoration rather than a production helper retained solely for tests.

No supported behavior test was deleted to obtain a passing result. Product and Property store tests were moved/reworked as persistence/use-case tests while preserving their core assertions. Historical restoration tests now retain their coverage through real SQLite reads.

## F. Remaining earlier persistence contracts and consumers

These retained contracts already express domain-aware writes or specific reads. Their earlier folder/namespace names do not mean their active consumers still use a generic or mixed store.

| Retained contract | Supported consumers | Reason retained |
|---|---|---|
| `Interfaces.Stores.IOrderRepository` | Create/Update/Delete Order, payment-method changes, five status transitions and Create Invoice | Commands need domain Order state, numbering and aggregate behavior. |
| `Interfaces.Repositories.IUserRepository` | Database Login; Create Admin; Create/Update/Activate/Deactivate User | Existing domain-aware user persistence; authentication and permissions were not redesigned. |
| `Interfaces.Repositories.IInvoiceRepository` | Create Invoice | Domain invoice creation and existing-invoice handling. |
| `Interfaces.Repositories.IDocumentSequenceRepository` | Invoice, Quotation and Proforma Invoice creation | Existing atomic document number reservation and transaction semantics. |
| `Interfaces.Stores.IQuotationRepository` | Create Quotation and Create Order draft-document linking | Domain document persistence/linking. |
| `Interfaces.Stores.IProformaInvoiceRepository` | Create Proforma Invoice and Create Order draft-document linking | Domain document persistence/linking. |
| `Features.OrderDrafts.Contracts.IDraftRepository` | Save/Delete Draft and Create Order draft completion | Already scoped, actor-owned persistence through Application. |
| `Features.OrderDrafts.Contracts.IDraftReadService` | Get Draft and List Draft | Already purpose-specific reads with ownership filtering. |
| `Features.Administration.Users.GetUser.IUserDetailsService` | Get User | Already a purpose-specific read contract. |
| `Features.Administration.Users.GetUsers.IUserListingService` | Get Users | Already a purpose-specific listing contract. |
| `Interfaces.IUnitOfWork` | Existing write features and migrated setup/catalog commands | Existing scoped commit/transaction boundary, unchanged. |

All namespaces in this table are relative to `OrderHub.Application`.

Other legacy persistence code deliberately remains for P2:

- Global `Interfaces.Repositories.ISupplierRepository` and `IDeliverymanRepository`, their old Infrastructure implementations under `Reopsitories`, and their existing registrations. Search found no active supported feature consumer. These are different fully qualified contracts from the new feature-owned interfaces; they do not compete for the same DI service type.
- The existing unused `RepositoryBase<TModel>` used by those old implementations. No new generic repository framework was created.
- Unregistered `ReadServices.ProductReadService`, which returns a query after disposing its context. It has no active supported consumer and was not adopted by the migrated paths.
- Infrastructure MediatR assembly scanning. Search and an architecture test confirm that no supported Infrastructure `IRequestHandler` remains, but removal of the now-unnecessary scan is deferred to P2.

## G. Dependency injection changes

New registration groups are `AddSetupPersistence`, `AddCatalogPersistence` and `AddOrderReads`, called by the existing Infrastructure composition root.

- All 20 feature persistence contracts listed in section C have one deliberate **scoped** registration.
- Scoped write repositories resolve the existing scoped `AppDbContext`; `IUnitOfWork` resolves that same context.
- Read implementations depend on the existing singleton context factory and dispose each read context. They do not store a scoped context in a singleton.
- Removed singleton Product/Property store and broad lookup-service registrations.
- Moved the previous transient nested item-editor reader registration to a scoped standalone contract, with single and batch reads sharing one implementation.
- Existing User, Draft, document and domain Order repository registrations/lifetimes remain unchanged.

`IRequestExecutor` remains registered exactly once as **scoped `AuthenticatedRequestExecutor`** in Web. Its implementation, identity capture, operation-scope creation and Current User initialization are unchanged. No Infrastructure executor or competing executor was introduced.

Architecture tests verify reader registration uniqueness/lifetime, handler ownership and the executor registration. Runtime tests resolve migrated paths through the production Application/Infrastructure registration methods, replacing only the factory with the existing isolated test factory.

## H. Business behavior and failure semantics

**No intentional business behavior changes.**

Preserved domain constructors/methods, validation order, duplicate checks, missing-entity results, deletion guards and failure messages. Product property/supplier synchronization and Property option synchronization now run in Application using the same domain operations.

Preserved Order source, VAT, saved product/supplier names, historical property values and metadata, current property requirements, inactive delivery state handling, delivery-chain ordering and editor draft/session behavior. Existing Order/document numbering, status changes, pricing and document transactions were retained.

Property Create/Update previously surfaced provider commit exceptions. Their handlers preserve the original exception instance/type/diagnostics when the existing unit of work wraps it in `PersistenceException`; this does not require an Application EF Core dependency. Focused tests protect that behavior. Existing commands that returned business failures for commit errors retain their prior results/messages.

Directly encountered read implementation details were adapted without changing matching rules:

- Client/carrier phone search uses the same country-code/separator/national-number text as `FullNumber`, expressed using mapped scalar fields so EF can execute the predicate in the database.
- Order details totals project the same unit-price × quantity calculation used by the existing domain subtotal/total getters, avoiding full aggregate materialization.
- Category info reads retain the legacy own-name `FullPath` result from fresh untracked categories without loaded ancestors. The existing `SubCategories.Count < 0` info flag remains unchanged; it was not silently corrected.

No authorization policy, permission behavior, authentication flow, cookie/circuit validation, Current User architecture, ownership rule, messaging integration or visual UI redesign was implemented.

## I. Database changes

| Change | Result |
|---|---|
| Schema migrations | None |
| Table/column changes | None |
| Relationships, indexes, constraints or delete behavior changes | None |
| Production data migrations/cleanup | None |
| Production seed redesign | None |
| Production context/factory changes | None |

The P0 SQLite fixture, schema creation, disposal and isolation implementation remains unchanged. Existing historical restoration scenarios intentionally contain unavailable references; those tests disable foreign-key enforcement only on their own isolated SQLite connection to persist those scenarios. No schema or shared fixture policy was changed. Query counting uses a test-only wrapper over an existing fixture connection.

## J. Validation results

The pre-change full-suite baseline was **168 passed, 1 failed, 0 skipped, 169 total**. The same permission test was already failing before any P1 edit.

| Final validation | Result |
|---|---|
| Supported production solution build | **Passed — 0 warnings, 0 errors** |
| Focused Web/Infrastructure/Property/Orders/Drafts suite | **169 passed, 0 failed, 0 skipped** |
| Complete supported test suite | **182 passed, 1 failed, 0 skipped, 183 total** |
| New P1 failures | **0** |
| Whitespace/error check | `git diff --check` passed |

Final commands:

```powershell
dotnet build OrderHub.sln --no-restore --verbosity quiet
dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-restore --verbosity quiet --filter "FullyQualifiedName~OrderHub.Tests.Web|FullyQualifiedName~OrderHub.Tests.Infrastructure|FullyQualifiedName~OrderHub.Tests.Application.PropertyTests|FullyQualifiedName~OrderHub.Tests.Application.Features.OrderDrafts|FullyQualifiedName~OrderHub.Tests.Application.Features.Orders" --logger "trx;LogFileName=P1-focused.trx"
dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-build --no-restore --verbosity quiet --logger "trx;LogFileName=P1-final.trx"
git diff --check
```

Focused tests were also run after the logical migration groups. Added coverage includes:

- Setup create/update/delete round trips, duplicate checks, projected reads, relationship deletion guards and cancellation.
- Application/Domain assembly boundaries, Infrastructure handler removal, DI uniqueness/lifetime and production registration resolution.
- Product/Property mutation and projection behavior through Application handlers, with retained existing assertions.
- Property duplicate rejection and original commit-failure propagation.
- Actual Order editor projection with current/historical metadata and fixed query count when item count grows from one to five.
- Database paging, Product/category/name lookup, phone search, Order details totals, editor metadata and shared lookup use cases.
- Existing editor recovery, restoration, session/draft lifecycle and concurrent identity-isolation tests.

No useful failing behavior test was removed. The suite grew by 14 test cases. Existing Blazor component-test warnings (`BL0005`/`BL0006`) remain in test compilation; they were not suppressed in the final validation.

### Accepted pre-existing failure

`OrderHub.Tests.Application.Features.Administration.Users.Update.UpdateUserCommandHandlerTests.Handle_Should_SyncMultiplePermissions`

Multiple unsaved `UserPermission` instances have database-generated ID `0`; existing ID-based equality can cause removal of the wrong permission. The assertion, synchronization implementation and permission model are unchanged. The P1 instructions explicitly accept this failure and reserve its resolution for later Authorization/Permission work. It is **not a P1 regression or blocker**.

### Validation limits

Tests use SQLite, not a live PostgreSQL server. The existing Npgsql-specific Category ancestry query retains its SQL and parameter approach, with cancellation propagation; it was inspected but not executed against PostgreSQL. The Product recursive category lookup was exercised on SQLite. No production database or data was contacted or changed. This report does not claim PostgreSQL integration or production performance benchmarking.

TRX artifacts are in the ignored test-results directory:

- [Focused results](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/TestResults/P1-focused.trx)
- [Complete suite results](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/TestResults/P1-final.trx)

## K. Architecture verification

| Requirement | Verification |
|---|---|
| Application does not depend on EF Core persistence implementation details | Confirmed by source searches and assembly-reference tests; new contracts contain concrete operations/results. |
| Domain remains persistence-agnostic | Confirmed; no Domain source changed and assembly checks exclude persistence/Web dependencies. |
| Web did not become a persistence layer | Confirmed; migrated Web reads call Application queries via `IRequestExecutor`, with no reader/repository or EF access. |
| Infrastructure owns EF Core/database access | Confirmed; reader and repository implementations remain there. |
| No generic repository framework introduced | Confirmed; existing unused generic code is deferred to P2. |
| No competing executor introduced | Confirmed; one scoped Web registration remains. |
| Current User architecture not redesigned | Confirmed; identity/executor source is unchanged and isolation tests pass. |
| Authentication not redesigned | Confirmed; login, inactive-new-login validation, cookies and session behavior are unchanged. |
| Authorization not implemented | Confirmed; no policy/role/permission-cache work. |
| Permission model not redesigned | Confirmed; the accepted legacy failure remains unchanged. |
| Cancellation propagated | Confirmed for new asynchronous persistence paths and covered by focused tests. |
| No schema/data migration | Confirmed; configuration, migration and production factory/context files are unchanged. |

Post-migration searches found no supported references to `IProductStore`, `IPropertyStore`, `ILookupService` or the removed Order query wrappers. Infrastructure has no remaining supported MediatR use-case handler. Web has no direct migrated reader/repository dependency. Unsupported historical projects were not restored, migrated or deleted.

## L. Deferred P2 candidates

1. Remove the unused old global Supplier/Deliveryman repository interfaces, implementations and registrations, then their exclusively associated `RepositoryBase<TModel>`.
2. Remove unused `ReadServices.ProductReadService` after the same supported-consumer verification. Do not adopt its disposed-context query API.
3. Remove the now-unnecessary Infrastructure MediatR assembly scan once P2 performs the final repository-wide registration audit.
4. Review residual unused legacy wrappers/models, including dormant Order query placeholders and global DTO declarations. The pre-existing unimplemented `GetOrder` placeholder has no current Web caller and was not turned into a new feature.
5. Consider ownership/file organization of the retained, already appropriate Order/document/User contracts without mechanical renaming or business redesign.
6. Review inherited read presentation issues such as Category info flags/ancestor paths separately from this behavior-preserving migration.

The known permission synchronization defect belongs to the later Authorization/Permission redesign, not P2 persistence cleanup.

## Working tree and file inventory

The working tree was clean at the start of P1. No commit or staging operation was performed. The following appendix records the final visible migration files and Git diff statistics; new files remain untracked for review.

### Git status --short --untracked-files=all

```text
 D OrderHub.Application/Commands/CategoryCommands.cs
 D OrderHub.Application/Commands/CityCommands.cs
 D OrderHub.Application/Commands/ClienCommands.cs
 D OrderHub.Application/Commands/DeliverymanCommands.cs
 D OrderHub.Application/Commands/ShippingCarriersCommands.cs
 D OrderHub.Application/Commands/SupplierCommands.cs
 M OrderHub.Application/DTOs/AddressDtos.cs
 D OrderHub.Application/DTOs/CategoryDtos.cs
 D OrderHub.Application/DTOs/CityDtos.cs
 D OrderHub.Application/DTOs/ClientDtos.cs
 M OrderHub.Application/DTOs/CommonDtos.cs
 D OrderHub.Application/DTOs/DeliverymanDtos.cs
 D OrderHub.Application/DTOs/PaymentMothodsDtos.cs
 D OrderHub.Application/DTOs/ShippingCarriersDtos.cs
 D OrderHub.Application/DTOs/SupplierDtos.cs
 D OrderHub.Application/Features/Orders/GetOrderItemEditor.cs
 D OrderHub.Application/Features/Orders/Queries/GetOrderDetials.cs
 D OrderHub.Application/Features/Orders/Queries/GetOrderEditQuery.cs
 D OrderHub.Application/Features/Orders/Queries/GetOrderItemsEditor.cs
 D OrderHub.Application/Features/Orders/Queries/GetOrderPaged.cs
 D OrderHub.Application/Features/Products/Contracts/IProductStore.cs
 D OrderHub.Application/Features/Products/Create/CreateProduct.cs
 M OrderHub.Application/Features/Products/Delete/DeleteProductCommand.cs
 D OrderHub.Application/Features/Products/Get/GetProduct.cs
 D OrderHub.Application/Features/Products/List/ListProducts.cs
 D OrderHub.Application/Features/Products/Update/UpdateProduct.cs
 M OrderHub.Application/Features/Setup/Properties/Create/CreatePropertyCommand.cs
 D OrderHub.Application/Features/Setup/Properties/Create/PropertyCreateDto.cs
 D OrderHub.Application/Features/Setup/Properties/Create/PropertyOptionCreateDto.cs
 D OrderHub.Application/Features/Setup/Properties/Delete/DeleteProperty.cs
 M OrderHub.Application/Features/Setup/Properties/Get/GetPropertyQuery.cs
 D OrderHub.Application/Features/Setup/Properties/Get/PropertyDetailsDto.cs
 D OrderHub.Application/Features/Setup/Properties/Get/PropertyOptionDto.cs
 M OrderHub.Application/Features/Setup/Properties/GetAll/GetPropertiesQuery.cs
 D OrderHub.Application/Features/Setup/Properties/GetAll/PropertyListDto.cs
 D OrderHub.Application/Features/Setup/Properties/Update/PropertyOptionUpdateDto.cs
 D OrderHub.Application/Features/Setup/Properties/Update/PropertyUpdateDto.cs
 M OrderHub.Application/Features/Setup/Properties/Update/UpdatePropertyCommand.cs
 D OrderHub.Application/Interfaces/ILookupService.cs
 D OrderHub.Application/Interfaces/Stores/IPropertyStore.cs
 D OrderHub.Application/Queries/CategoryQueries.cs
 D OrderHub.Application/Queries/CityQueries.cs
 D OrderHub.Application/Queries/ClientQueries.cs
 D OrderHub.Application/Queries/CommonQueries.cs
 D OrderHub.Application/Queries/DeliverymanQueries.cs
 D OrderHub.Application/Queries/PaymentMothodQueries.cs
 D OrderHub.Application/Queries/ShippingCarriersQueries.cs
 D OrderHub.Application/Queries/SupplierQueries.cs
 D OrderHub.Infrastructure/CommandHandlers/Categories/CreateCategoryCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Categories/DeleteCategoryCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Categories/UpdateCategoryCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Cities/CreateCityCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Cities/DeleteCityCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Cities/UpdateCityCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Clientss/CreateClientCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Clientss/DeleteClientCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Clientss/UpdateClientCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Deliverymen/CreateDeliverymanCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Deliverymen/DeleteDeliverymanCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Deliverymen/UpdateDeliverymanCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/PaymentMethods/GetPaymentMethodListQueryHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/ShippingCarriers/CreateShippingCarrierCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/ShippingCarriers/DeleteShippingCarrierCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/ShippingCarriers/UpdateShippingCarrierCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Suppliers/CrateSupplierCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Suppliers/DeleteSupplierCommandHandler.cs
 D OrderHub.Infrastructure/CommandHandlers/Suppliers/UpdateSupplierCommandHandler.cs
 M OrderHub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
 M OrderHub.Infrastructure/Features/Orders/OrderItemEditorReader.cs
 D OrderHub.Infrastructure/Features/Orders/Queries/GetOrderDetialsQeuryHandler.cs
 D OrderHub.Infrastructure/Features/Orders/Queries/GetOrderEditQueryHandler.cs
 D OrderHub.Infrastructure/Features/Orders/Queries/GetOrderItemsEditorQeryHandler.cs
 D OrderHub.Infrastructure/Features/Orders/Queries/GetOrderPagedQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Categories/GetCategoryForEditQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Categories/GetCategoryListQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Categories/GetCategoryParentIdsQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Categories/GetCategoryTreeQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Cities/GetAllCitiesQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Cities/GetCityForEditQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Clients/GetAllClientsPagedQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Clients/GetAllClientsQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Clients/GetClientEditQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Clients/GetClientsByNameQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetAllDeliverymenInfoQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetAllShippingCarriersInfoQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetCategoriesInfoQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetCitiesInfoQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetClientsInfoQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetProductsInfoQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetRootCategoriesQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetSubCategoriesQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/CommonQueryHandlers/GetSuppliersInfoQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Deliverymen/GetAllDeliverymenListQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Deliverymen/GetDeliverymanForEditQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Deliverymen/GetDeliverymenByNameQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/ShippingCarriers/GetShippingCarrierForEditQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/ShippingCarriers/GetShippingCarriersByNameQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/ShippingCarriers/GetShippingCarriersQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Suppliers/GetAllSuppliersQueryHandler.cs
 D OrderHub.Infrastructure/QueryHandlers/Suppliers/GetSupplierForEditQueryHandler.cs
 D OrderHub.Infrastructure/ReadServices/LookupService.cs
 D OrderHub.Infrastructure/ReadServices/ServiceCollectionExtensions.cs
 D OrderHub.Infrastructure/Stores/ProductStore.cs
 D OrderHub.Infrastructure/Stores/PropertyStore.cs
 D OrderHub.Infrastructure/Stores/ServiceCollectionExtensions.cs
 M OrderHub.Tests/Application/PropertyTests.cs
 D OrderHub.Tests/Infrastructure/Stores/ProductStoreTests.cs
 D OrderHub.Tests/Infrastructure/Stores/PropertyStoreTests.cs
 M OrderHub.Tests/Web/DraftCompletionLifecycleTests.cs
 M OrderHub.Tests/Web/DraftRestorationTests.cs
 M OrderHub.Tests/Web/OrderCreateFlowTests.cs
 M OrderHub.Tests/Web/OrderDetailsTests.cs
 M OrderHub.Tests/Web/OrderEditorSessionTests.cs
 M OrderHub.Tests/Web/OrderUpdateFlowTests.cs
 M OrderHub.Tests/Web/PersistedOrderRestorationTests.cs
 M OrderHub.Tests/Web/ProductAdditionRecoveryTests.cs
 M OrderHub.Web/Components/Pages/Features/Orders/Editor/OrderDetails/DetailsStep.razor
 M OrderHub.Web/Components/Pages/Features/Orders/Editor/OrderDetails/DetailsStep.razor.cs
 M OrderHub.Web/Components/Pages/Features/Orders/Editor/OrderEditor.razor
 M OrderHub.Web/Components/Pages/Features/Orders/Editor/OrderEditor.razor.cs
 M OrderHub.Web/Components/Pages/Features/Orders/Editor/Products/AddProductDialog.razor.cs
 M OrderHub.Web/Components/Pages/Features/Orders/Editor/Products/ProductsStep.razor.cs
 M OrderHub.Web/Components/Pages/Features/Orders/Index/Orders.razor
 M OrderHub.Web/Components/Pages/Features/Orders/Index/Orders.razor.cs
 M OrderHub.Web/Components/Pages/Features/Orders/OrderDetials.razor
?? OrderHub.Application/Features/Orders/Contracts/IOrderEditorLookupReader.cs
?? OrderHub.Application/Features/Orders/Contracts/IOrderReader.cs
?? OrderHub.Application/Features/Orders/GetDetails/GetOrderDetailsQuery.cs
?? OrderHub.Application/Features/Orders/GetDetails/GetOrderDetailsQueryHandler.cs
?? OrderHub.Application/Features/Orders/GetDetails/OrderDetails.cs
?? OrderHub.Application/Features/Orders/GetDetails/OrderDetailsClient.cs
?? OrderHub.Application/Features/Orders/GetDetails/OrderDetailsDelivery.cs
?? OrderHub.Application/Features/Orders/GetDetails/OrderDetailsHeader.cs
?? OrderHub.Application/Features/Orders/GetDetails/OrderDetailsLine.cs
?? OrderHub.Application/Features/Orders/GetDetails/OrderDetailsPayment.cs
?? OrderHub.Application/Features/Orders/GetEditorLookups/GetOrderEditorLookupsHandler.cs
?? OrderHub.Application/Features/Orders/GetEditorLookups/GetOrderEditorLookupsQuery.cs
?? OrderHub.Application/Features/Orders/GetEditorLookups/OrderEditorLookups.cs
?? OrderHub.Application/Features/Orders/GetForEdit/GetOrderEditQueryHandler.cs
?? OrderHub.Application/Features/Orders/GetForEdit/GetOrderForEditQuery.cs
?? OrderHub.Application/Features/Orders/GetForEdit/OrderEditorData.cs
?? OrderHub.Application/Features/Orders/GetForEdit/OrderEditorDeliveryStep.cs
?? OrderHub.Application/Features/Orders/GetForEdit/OrderEditorItem.cs
?? OrderHub.Application/Features/Orders/GetForEdit/OrderEditorOption.cs
?? OrderHub.Application/Features/Orders/GetForEdit/OrderEditorProperty.cs
?? OrderHub.Application/Features/Orders/GetForEdit/OrderEditorSupplier.cs
?? OrderHub.Application/Features/Orders/GetItemEditor/GetOrderItemEditorHandler.cs
?? OrderHub.Application/Features/Orders/GetItemEditor/GetOrderItemEditorQuery.cs
?? OrderHub.Application/Features/Orders/GetItemEditor/IOrderItemEditorReader.cs
?? OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorData.cs
?? OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorProperty.cs
?? OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorPropertyOption.cs
?? OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorSupplier.cs
?? OrderHub.Application/Features/Orders/GetItemsEditor/GetOrderItemsEditorHandler.cs
?? OrderHub.Application/Features/Orders/GetItemsEditor/GetOrderItemsEditorQuery.cs
?? OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogItem.cs
?? OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogProperty.cs
?? OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogPropertyOption.cs
?? OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogSupplier.cs
?? OrderHub.Application/Features/Orders/GetPage/GetOrderPagedQueryHandler.cs
?? OrderHub.Application/Features/Orders/GetPage/GetOrdersPageQuery.cs
?? OrderHub.Application/Features/Orders/GetPage/OrderListItem.cs
?? OrderHub.Application/Features/Orders/GetPage/OrderPaymentMethodItem.cs
?? OrderHub.Application/Features/Products/Contracts/IProductReader.cs
?? OrderHub.Application/Features/Products/Contracts/IProductRepository.cs
?? OrderHub.Application/Features/Products/Contracts/ProductInfo.cs
?? OrderHub.Application/Features/Products/Create/CreateProductCommand.cs
?? OrderHub.Application/Features/Products/Create/CreateProductHandler.cs
?? OrderHub.Application/Features/Products/Create/ProductCreateInput.cs
?? OrderHub.Application/Features/Products/Create/ProductPropertyInput.cs
?? OrderHub.Application/Features/Products/Delete/DeleteProductHandler.cs
?? OrderHub.Application/Features/Products/Get/GetProductHandler.cs
?? OrderHub.Application/Features/Products/Get/GetProductQuery.cs
?? OrderHub.Application/Features/Products/Get/ProductDetails.cs
?? OrderHub.Application/Features/Products/Get/ProductPropertySelection.cs
?? OrderHub.Application/Features/Products/GetProductsInfo/GetProductsInfoQuery.cs
?? OrderHub.Application/Features/Products/GetProductsInfo/GetProductsInfoQueryHandler.cs
?? OrderHub.Application/Features/Products/List/ListProductsHandler.cs
?? OrderHub.Application/Features/Products/List/ListProductsQuery.cs
?? OrderHub.Application/Features/Products/List/ProductListItem.cs
?? OrderHub.Application/Features/Products/List/ProductSupplierItem.cs
?? OrderHub.Application/Features/Products/Search/SearchProductsHandler.cs
?? OrderHub.Application/Features/Products/Search/SearchProductsQuery.cs
?? OrderHub.Application/Features/Products/Update/ProductPropertyUpdate.cs
?? OrderHub.Application/Features/Products/Update/ProductUpdateInput.cs
?? OrderHub.Application/Features/Products/Update/UpdateProductCommand.cs
?? OrderHub.Application/Features/Products/Update/UpdateProductHandler.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/CategoryCreateInput.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/CategoryEdit.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/CategoryInfo.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/CategoryListItem.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/CategoryTree.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/CategoryUpdateInput.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/ICategoryReader.cs
?? OrderHub.Application/Features/Setup/Categories/Contracts/ICategoryRepository.cs
?? OrderHub.Application/Features/Setup/Categories/CreateCategory/CreateCategoryCommand.cs
?? OrderHub.Application/Features/Setup/Categories/CreateCategory/CreateCategoryCommandHandler.cs
?? OrderHub.Application/Features/Setup/Categories/DeleteCategory/DeleteCategoryCommand.cs
?? OrderHub.Application/Features/Setup/Categories/DeleteCategory/DeleteCategoryCommandHandler.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoriesInfo/GetCategoriesInfoQuery.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoriesInfo/GetCategoriesInfoQueryHandler.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryForEdit/GetCategoryForEditQuery.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryForEdit/GetCategoryForEditQueryHandler.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryList/GetCategoryListQuery.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryList/GetCategoryListQueryHandler.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryParentIds/GetCategoryParentIdsQuery.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryParentIds/GetCategoryParentIdsQueryHandler.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryTree/GetCategoryTreeQuery.cs
?? OrderHub.Application/Features/Setup/Categories/GetCategoryTree/GetCategoryTreeQueryHandler.cs
?? OrderHub.Application/Features/Setup/Categories/GetRootCategories/GetRootCategoriesQuery.cs
?? OrderHub.Application/Features/Setup/Categories/GetRootCategories/GetRootCategoriesQueryHandler.cs
?? OrderHub.Application/Features/Setup/Categories/GetSubCategories/GetSubCategoriesQuery.cs
?? OrderHub.Application/Features/Setup/Categories/GetSubCategories/GetSubCategoriesQueryHandler.cs
?? OrderHub.Application/Features/Setup/Categories/UpdateCategory/UpdateCategoryCommand.cs
?? OrderHub.Application/Features/Setup/Categories/UpdateCategory/UpdateCategoryCommandHandler.cs
?? OrderHub.Application/Features/Setup/Cities/Contracts/CityCreateInput.cs
?? OrderHub.Application/Features/Setup/Cities/Contracts/CityInfo.cs
?? OrderHub.Application/Features/Setup/Cities/Contracts/CityListItem.cs
?? OrderHub.Application/Features/Setup/Cities/Contracts/CityUpdateInput.cs
?? OrderHub.Application/Features/Setup/Cities/Contracts/ICityReader.cs
?? OrderHub.Application/Features/Setup/Cities/Contracts/ICityRepository.cs
?? OrderHub.Application/Features/Setup/Cities/CreateCity/CreateCityCommand.cs
?? OrderHub.Application/Features/Setup/Cities/CreateCity/CreateCityCommandHandler.cs
?? OrderHub.Application/Features/Setup/Cities/DeleteCity/DeleteCityCommand.cs
?? OrderHub.Application/Features/Setup/Cities/DeleteCity/DeleteCityCommandHandler.cs
?? OrderHub.Application/Features/Setup/Cities/GetAllCities/GetAllCitiesQuery.cs
?? OrderHub.Application/Features/Setup/Cities/GetAllCities/GetAllCitiesQueryHandler.cs
?? OrderHub.Application/Features/Setup/Cities/GetCitiesInfo/GetCitiesInfoQuery.cs
?? OrderHub.Application/Features/Setup/Cities/GetCitiesInfo/GetCitiesInfoQueryHandler.cs
?? OrderHub.Application/Features/Setup/Cities/GetCityForEdit/GetCityForEditQuery.cs
?? OrderHub.Application/Features/Setup/Cities/GetCityForEdit/GetCityForEditQueryHandler.cs
?? OrderHub.Application/Features/Setup/Cities/UpdateCity/UpdateCityCommand.cs
?? OrderHub.Application/Features/Setup/Cities/UpdateCity/UpdateCityCommandHandler.cs
?? OrderHub.Application/Features/Setup/Clients/Contracts/ClientInfo.cs
?? OrderHub.Application/Features/Setup/Clients/Contracts/ClientInput.cs
?? OrderHub.Application/Features/Setup/Clients/Contracts/ClientListItem.cs
?? OrderHub.Application/Features/Setup/Clients/Contracts/IClientReader.cs
?? OrderHub.Application/Features/Setup/Clients/Contracts/IClientRepository.cs
?? OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommand.cs
?? OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommandHandler.cs
?? OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommand.cs
?? OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommandHandler.cs
?? OrderHub.Application/Features/Setup/Clients/GetAllClients/GetAllClientsQuery.cs
?? OrderHub.Application/Features/Setup/Clients/GetAllClients/GetAllClientsQueryHandler.cs
?? OrderHub.Application/Features/Setup/Clients/GetAllClientsPaged/GetAllClientsPagedQuery.cs
?? OrderHub.Application/Features/Setup/Clients/GetAllClientsPaged/GetAllClientsPagedQueryHandler.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientById/GetClientByIdQuery.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientById/GetClientByIdQueryHandler.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientEdit/GetClientEditQuery.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientEdit/GetClientEditQueryHandler.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientsByName/GetClientsByNameQuery.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientsByName/GetClientsByNameQueryHandler.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientsInfo/GetClientsInfoQuery.cs
?? OrderHub.Application/Features/Setup/Clients/GetClientsInfo/GetClientsInfoQueryHandler.cs
?? OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommand.cs
?? OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommandHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanInfo.cs
?? OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanInput.cs
?? OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanListItem.cs
?? OrderHub.Application/Features/Setup/Deliverymen/Contracts/IDeliverymanReader.cs
?? OrderHub.Application/Features/Setup/Deliverymen/Contracts/IDeliverymanRepository.cs
?? OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommand.cs
?? OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommandHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommand.cs
?? OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommandHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenInfo/GetAllDeliverymenInfoQuery.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenInfo/GetAllDeliverymenInfoQueryHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenList/GetAllDeliverymenListQuery.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenList/GetAllDeliverymenListQueryHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanById/GetDeliverymanByIdQuery.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanById/GetDeliverymanByIdQueryHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanForEdit/GetDeliverymanForEditQuery.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanForEdit/GetDeliverymanForEditQueryHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymenByName/GetDeliverymenByNameQuery.cs
?? OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymenByName/GetDeliverymenByNameQueryHandler.cs
?? OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommand.cs
?? OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommandHandler.cs
?? OrderHub.Application/Features/Setup/PaymentMethods/Contracts/IPaymentMethodReader.cs
?? OrderHub.Application/Features/Setup/PaymentMethods/Contracts/PaymentMethodListItem.cs
?? OrderHub.Application/Features/Setup/PaymentMethods/GetPaymentMethodList/GetPaymentMethodListQuery.cs
?? OrderHub.Application/Features/Setup/PaymentMethods/GetPaymentMethodList/GetPaymentMethodListQueryHandler.cs
?? OrderHub.Application/Features/Setup/Properties/Contracts/IPropertyReader.cs
?? OrderHub.Application/Features/Setup/Properties/Contracts/IPropertyRepository.cs
?? OrderHub.Application/Features/Setup/Properties/Create/CreatePropertyCommandHandler.cs
?? OrderHub.Application/Features/Setup/Properties/Create/PropertyCreateInput.cs
?? OrderHub.Application/Features/Setup/Properties/Create/PropertyOptionInput.cs
?? OrderHub.Application/Features/Setup/Properties/Delete/DeletePropertyCommand.cs
?? OrderHub.Application/Features/Setup/Properties/Delete/DeletePropertyHandler.cs
?? OrderHub.Application/Features/Setup/Properties/Get/GetPropertyQueryHandler.cs
?? OrderHub.Application/Features/Setup/Properties/Get/PropertyDetails.cs
?? OrderHub.Application/Features/Setup/Properties/Get/PropertyOptionItem.cs
?? OrderHub.Application/Features/Setup/Properties/GetAll/GetPropertiesQueryHandler.cs
?? OrderHub.Application/Features/Setup/Properties/GetAll/PropertyListItem.cs
?? OrderHub.Application/Features/Setup/Properties/Update/PropertyOptionUpdate.cs
?? OrderHub.Application/Features/Setup/Properties/Update/PropertyUpdateInput.cs
?? OrderHub.Application/Features/Setup/Properties/Update/UpdatePropertyCommandHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/IShippingCarrierReader.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/IShippingCarrierRepository.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierCreateInput.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierEdit.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierInfo.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierListItem.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierUpdateInput.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommand.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommandHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommand.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommandHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetAllShippingCarriersInfo/GetAllShippingCarriersInfoQuery.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetAllShippingCarriersInfo/GetAllShippingCarriersInfoQueryHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierById/GetShippingCarrierByIdQuery.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierById/GetShippingCarrierByIdQueryHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierForEdit/GetShippingCarrierForEditQuery.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierForEdit/GetShippingCarrierForEditQueryHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriers/GetShippingCarriersQuery.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriers/GetShippingCarriersQueryHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriersByName/GetShippingCarriersByNameQuery.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriersByName/GetShippingCarriersByNameQueryHandler.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommand.cs
?? OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommandHandler.cs
?? OrderHub.Application/Features/Setup/Suppliers/Contracts/ISupplierReader.cs
?? OrderHub.Application/Features/Setup/Suppliers/Contracts/ISupplierRepository.cs
?? OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierCreateInput.cs
?? OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierEdit.cs
?? OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierInfo.cs
?? OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierListItem.cs
?? OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierUpdateInput.cs
?? OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CrateSupplierCommandHandler.cs
?? OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommand.cs
?? OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommand.cs
?? OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommandHandler.cs
?? OrderHub.Application/Features/Setup/Suppliers/GetAllSuppliers/GetAllSuppliersQuery.cs
?? OrderHub.Application/Features/Setup/Suppliers/GetAllSuppliers/GetAllSuppliersQueryHandler.cs
?? OrderHub.Application/Features/Setup/Suppliers/GetSupplierForEdit/GetSupplierForEditQuery.cs
?? OrderHub.Application/Features/Setup/Suppliers/GetSupplierForEdit/GetSupplierForEditQueryHandler.cs
?? OrderHub.Application/Features/Setup/Suppliers/GetSuppliersInfo/GetSuppliersInfoQuery.cs
?? OrderHub.Application/Features/Setup/Suppliers/GetSuppliersInfo/GetSuppliersInfoQueryHandler.cs
?? OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommand.cs
?? OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommandHandler.cs
?? OrderHub.Infrastructure/Features/Orders/OrderEditorLookupReader.cs
?? OrderHub.Infrastructure/Features/Orders/OrderEditorRead.cs
?? OrderHub.Infrastructure/Features/Orders/OrderQueryFilters.cs
?? OrderHub.Infrastructure/Features/Orders/OrderReader.cs
?? OrderHub.Infrastructure/Features/Orders/ServiceCollectionExtensions.cs
?? OrderHub.Infrastructure/Features/Products/ProductReader.cs
?? OrderHub.Infrastructure/Features/Products/ProductRepository.cs
?? OrderHub.Infrastructure/Features/Products/ServiceCollectionExtensions.cs
?? OrderHub.Infrastructure/Features/Setup/Categories/CategoryReader.cs
?? OrderHub.Infrastructure/Features/Setup/Categories/CategoryRepository.cs
?? OrderHub.Infrastructure/Features/Setup/Cities/CityReader.cs
?? OrderHub.Infrastructure/Features/Setup/Cities/CityRepository.cs
?? OrderHub.Infrastructure/Features/Setup/Clients/ClientReader.cs
?? OrderHub.Infrastructure/Features/Setup/Clients/ClientRepository.cs
?? OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanReader.cs
?? OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanRepository.cs
?? OrderHub.Infrastructure/Features/Setup/PaymentMethods/PaymentMethodReader.cs
?? OrderHub.Infrastructure/Features/Setup/Properties/PropertyReader.cs
?? OrderHub.Infrastructure/Features/Setup/Properties/PropertyRepository.cs
?? OrderHub.Infrastructure/Features/Setup/ServiceCollectionExtensions.cs
?? OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierReader.cs
?? OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierRepository.cs
?? OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierReader.cs
?? OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierRepository.cs
?? OrderHub.Tests/Infrastructure/PersistenceArchitectureTests.cs
?? OrderHub.Tests/Infrastructure/ReadProjectionTests.cs
?? OrderHub.Tests/Infrastructure/SetupPersistenceTests.cs
?? OrderHub.Tests/Infrastructure/Stores/ProductPersistenceTests.cs
?? OrderHub.Tests/Infrastructure/Stores/PropertyPersistenceTests.cs
?? P1-Result.md
```

### Git diff --stat

This standard statistic covers tracked changes only; Git does not include untracked additions in it.

```text
 OrderHub.Application/Commands/CategoryCommands.cs  |  12 -
 OrderHub.Application/Commands/CityCommands.cs      |  12 -
 OrderHub.Application/Commands/ClienCommands.cs     |  12 -
 .../Commands/DeliverymanCommands.cs                |  12 -
 .../Commands/ShippingCarriersCommands.cs           |  12 -
 OrderHub.Application/Commands/SupplierCommands.cs  |  12 -
 OrderHub.Application/DTOs/AddressDtos.cs           |   4 +-
 OrderHub.Application/DTOs/CategoryDtos.cs          |  15 --
 OrderHub.Application/DTOs/CityDtos.cs              |   8 -
 OrderHub.Application/DTOs/ClientDtos.cs            |   7 -
 OrderHub.Application/DTOs/CommonDtos.cs            |  13 +-
 OrderHub.Application/DTOs/DeliverymanDtos.cs       |  15 --
 OrderHub.Application/DTOs/PaymentMothodsDtos.cs    |   6 -
 OrderHub.Application/DTOs/ShippingCarriersDtos.cs  |   9 -
 OrderHub.Application/DTOs/SupplierDtos.cs          |  44 ----
 .../Features/Orders/GetOrderItemEditor.cs          |  50 ----
 .../Features/Orders/Queries/GetOrderDetials.cs     |  47 ----
 .../Features/Orders/Queries/GetOrderEditQuery.cs   |  51 ----
 .../Features/Orders/Queries/GetOrderItemsEditor.cs |  34 ---
 .../Features/Orders/Queries/GetOrderPaged.cs       |  34 ---
 .../Features/Products/Contracts/IProductStore.cs   |  21 --
 .../Features/Products/Create/CreateProduct.cs      |  40 ---
 .../Products/Delete/DeleteProductCommand.cs        |  10 +-
 .../Features/Products/Get/GetProduct.cs            |  37 ---
 .../Features/Products/List/ListProducts.cs         |  39 ---
 .../Features/Products/Update/UpdateProduct.cs      |  40 ---
 .../Properties/Create/CreatePropertyCommand.cs     |  24 +-
 .../Setup/Properties/Create/PropertyCreateDto.cs   |   6 -
 .../Properties/Create/PropertyOptionCreateDto.cs   |   3 -
 .../Setup/Properties/Delete/DeleteProperty.cs      |  27 ---
 .../Setup/Properties/Get/GetPropertyQuery.cs       |  21 +-
 .../Setup/Properties/Get/PropertyDetailsDto.cs     |  11 -
 .../Setup/Properties/Get/PropertyOptionDto.cs      |   5 -
 .../Setup/Properties/GetAll/GetPropertiesQuery.cs  |  15 +-
 .../Setup/Properties/GetAll/PropertyListDto.cs     |   5 -
 .../Properties/Update/PropertyOptionUpdateDto.cs   |   5 -
 .../Setup/Properties/Update/PropertyUpdateDto.cs   |   6 -
 .../Properties/Update/UpdatePropertyCommand.cs     |  14 +-
 OrderHub.Application/Interfaces/ILookupService.cs  |  16 --
 .../Interfaces/Stores/IPropertyStore.cs            |  30 ---
 OrderHub.Application/Queries/CategoryQueries.cs    |  13 -
 OrderHub.Application/Queries/CityQueries.cs        |  11 -
 OrderHub.Application/Queries/ClientQueries.cs      |  15 --
 OrderHub.Application/Queries/CommonQueries.cs      |  18 --
 OrderHub.Application/Queries/DeliverymanQueries.cs |  13 -
 .../Queries/PaymentMothodQueries.cs                |  11 -
 .../Queries/ShippingCarriersQueries.cs             |  13 -
 OrderHub.Application/Queries/SupplierQueries.cs    |  11 -
 .../Categories/CreateCategoryCommandHandler.cs     |  52 ----
 .../Categories/DeleteCategoryCommandHandler.cs     |  45 ----
 .../Categories/UpdateCategoryCommandHandler.cs     |  45 ----
 .../Cities/CreateCityCommandHandler.cs             |  30 ---
 .../Cities/DeleteCityCommandHandler.cs             |  47 ----
 .../Cities/UpdateCityCommandHandler.cs             |  37 ---
 .../Clientss/CreateClientCommandHandler.cs         |  40 ---
 .../Clientss/DeleteClientCommandHandler.cs         |  41 ----
 .../Clientss/UpdateClientCommandHandler.cs         |  46 ----
 .../Deliverymen/CreateDeliverymanCommandHandler.cs |  37 ---
 .../Deliverymen/DeleteDeliverymanCommandHandler.cs |  47 ----
 .../Deliverymen/UpdateDeliverymanCommandHandler.cs |  41 ----
 .../GetPaymentMethodListQueryHandler.cs            |  23 --
 .../CreateShippingCarrierCommandHandler.cs         |  52 ----
 .../DeleteShippingCarrierCommandHandler.cs         |  44 ----
 .../UpdateShippingCarrierCommandHandler.cs         |  48 ----
 .../Suppliers/CrateSupplierCommandHandler.cs       |  52 ----
 .../Suppliers/DeleteSupplierCommandHandler.cs      |  47 ----
 .../Suppliers/UpdateSupplierCommandHandler.cs      |  49 ----
 .../ServiceCollectionExtensions.cs                 |  10 +-
 .../Features/Orders/OrderItemEditorReader.cs       |  45 +++-
 .../Orders/Queries/GetOrderDetialsQeuryHandler.cs  |  56 -----
 .../Orders/Queries/GetOrderEditQueryHandler.cs     | 126 ----------
 .../Queries/GetOrderItemsEditorQeryHandler.cs      |  40 ---
 .../Orders/Queries/GetOrderPagedQueryHandler.cs    |  75 ------
 .../Categories/GetCategoryForEditQueryHandler.cs   |  24 --
 .../Categories/GetCategoryListQueryHandler.cs      |  35 ---
 .../Categories/GetCategoryParentIdsQueryHandler.cs |  34 ---
 .../Categories/GetCategoryTreeQueryHandler.cs      |  66 -----
 .../Cities/GetAllCitiesQueryHandler.cs             |  31 ---
 .../Cities/GetCityForEditQueryHandler.cs           |  25 --
 .../Clients/GetAllClientsPagedQueryHandler.cs      |  34 ---
 .../Clients/GetAllClientsQueryHandler.cs           |  32 ---
 .../Clients/GetClientEditQueryHandler.cs           |  36 ---
 .../Clients/GetClientsByNameQueryHandler.cs        |  59 -----
 .../GetAllDeliverymenInfoQueryHandler.cs           |  22 --
 .../GetAllShippingCarriersInfoQueryHandler.cs      |  21 --
 .../GetCategoriesInfoQueryHandler.cs               |  25 --
 .../GetCitiesInfoQueryHandler.cs                   |  25 --
 .../GetClientsInfoQueryHandler.cs                  |  24 --
 .../GetProductsInfoQueryHandler.cs                 |  24 --
 .../GetRootCategoriesQueryHandler.cs               |  24 --
 .../GetSubCategoriesQueryHandler.cs                |  24 --
 .../GetSuppliersInfoQueryHandler.cs                |  24 --
 .../GetAllDeliverymenListQueryHandler.cs           |  28 ---
 .../GetDeliverymanForEditQueryHandler.cs           |  27 ---
 .../GetDeliverymenByNameQueryHandler.cs            |  57 -----
 .../GetShippingCarrierForEditQueryHandler.cs       |  32 ---
 .../GetShippingCarriersByNameQueryHandler.cs       |  60 -----
 .../GetShippingCarriersQueryHandler.cs             |  31 ---
 .../Suppliers/GetAllSuppliersQueryHandler.cs       |  31 ---
 .../Suppliers/GetSupplierForEditQueryHandler.cs    |  38 ---
 .../ReadServices/LookupService.cs                  |  78 ------
 .../ReadServices/ServiceCollectionExtensions.cs    |  14 --
 OrderHub.Infrastructure/Stores/ProductStore.cs     | 269 ---------------------
 OrderHub.Infrastructure/Stores/PropertyStore.cs    | 130 ----------
 .../Stores/ServiceCollectionExtensions.cs          |  18 --
 OrderHub.Tests/Application/PropertyTests.cs        |  81 ++++---
 .../Infrastructure/Stores/ProductStoreTests.cs     | 148 ------------
 .../Infrastructure/Stores/PropertyStoreTests.cs    |  72 ------
 .../Web/DraftCompletionLifecycleTests.cs           |  16 +-
 OrderHub.Tests/Web/DraftRestorationTests.cs        |  26 +-
 OrderHub.Tests/Web/OrderCreateFlowTests.cs         |  45 +++-
 OrderHub.Tests/Web/OrderDetailsTests.cs            |  16 +-
 OrderHub.Tests/Web/OrderEditorSessionTests.cs      |  16 +-
 OrderHub.Tests/Web/OrderUpdateFlowTests.cs         |  26 +-
 .../Web/PersistedOrderRestorationTests.cs          |  37 ++-
 OrderHub.Tests/Web/ProductAdditionRecoveryTests.cs |  16 +-
 .../Orders/Editor/OrderDetails/DetailsStep.razor   |   7 +-
 .../Editor/OrderDetails/DetailsStep.razor.cs       |  10 +-
 .../Pages/Features/Orders/Editor/OrderEditor.razor |   2 +-
 .../Features/Orders/Editor/OrderEditor.razor.cs    |  45 ++--
 .../Editor/Products/AddProductDialog.razor.cs      |   8 +-
 .../Orders/Editor/Products/ProductsStep.razor.cs   |  39 +--
 .../Pages/Features/Orders/Index/Orders.razor       |   5 +-
 .../Pages/Features/Orders/Index/Orders.razor.cs    |  12 +-
 .../Pages/Features/Orders/OrderDetials.razor       |  11 +-
 125 files changed, 329 insertions(+), 3775 deletions(-)
```

### Added source/test files

There are **242 new C# source/test files**, totaling **4879 lines**, in addition to the tracked changes above. This Markdown report is also a new, untracked artifact. Generated TRX files remain ignored. Temporary editing scripts were removed.

- [OrderHub.Application/Features/Orders/Contracts/IOrderEditorLookupReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/Contracts/IOrderEditorLookupReader.cs)
- [OrderHub.Application/Features/Orders/Contracts/IOrderReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/Contracts/IOrderReader.cs)
- [OrderHub.Application/Features/Orders/GetDetails/GetOrderDetailsQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/GetOrderDetailsQuery.cs)
- [OrderHub.Application/Features/Orders/GetDetails/GetOrderDetailsQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/GetOrderDetailsQueryHandler.cs)
- [OrderHub.Application/Features/Orders/GetDetails/OrderDetails.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/OrderDetails.cs)
- [OrderHub.Application/Features/Orders/GetDetails/OrderDetailsClient.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/OrderDetailsClient.cs)
- [OrderHub.Application/Features/Orders/GetDetails/OrderDetailsDelivery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/OrderDetailsDelivery.cs)
- [OrderHub.Application/Features/Orders/GetDetails/OrderDetailsHeader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/OrderDetailsHeader.cs)
- [OrderHub.Application/Features/Orders/GetDetails/OrderDetailsLine.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/OrderDetailsLine.cs)
- [OrderHub.Application/Features/Orders/GetDetails/OrderDetailsPayment.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetDetails/OrderDetailsPayment.cs)
- [OrderHub.Application/Features/Orders/GetEditorLookups/GetOrderEditorLookupsHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetEditorLookups/GetOrderEditorLookupsHandler.cs)
- [OrderHub.Application/Features/Orders/GetEditorLookups/GetOrderEditorLookupsQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetEditorLookups/GetOrderEditorLookupsQuery.cs)
- [OrderHub.Application/Features/Orders/GetEditorLookups/OrderEditorLookups.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetEditorLookups/OrderEditorLookups.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/GetOrderEditQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/GetOrderEditQueryHandler.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/GetOrderForEditQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/GetOrderForEditQuery.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/OrderEditorData.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/OrderEditorData.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/OrderEditorDeliveryStep.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/OrderEditorDeliveryStep.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/OrderEditorItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/OrderEditorItem.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/OrderEditorOption.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/OrderEditorOption.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/OrderEditorProperty.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/OrderEditorProperty.cs)
- [OrderHub.Application/Features/Orders/GetForEdit/OrderEditorSupplier.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetForEdit/OrderEditorSupplier.cs)
- [OrderHub.Application/Features/Orders/GetItemEditor/GetOrderItemEditorHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemEditor/GetOrderItemEditorHandler.cs)
- [OrderHub.Application/Features/Orders/GetItemEditor/GetOrderItemEditorQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemEditor/GetOrderItemEditorQuery.cs)
- [OrderHub.Application/Features/Orders/GetItemEditor/IOrderItemEditorReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemEditor/IOrderItemEditorReader.cs)
- [OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorData.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorData.cs)
- [OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorProperty.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorProperty.cs)
- [OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorPropertyOption.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorPropertyOption.cs)
- [OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorSupplier.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemEditor/OrderItemEditorSupplier.cs)
- [OrderHub.Application/Features/Orders/GetItemsEditor/GetOrderItemsEditorHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemsEditor/GetOrderItemsEditorHandler.cs)
- [OrderHub.Application/Features/Orders/GetItemsEditor/GetOrderItemsEditorQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemsEditor/GetOrderItemsEditorQuery.cs)
- [OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogItem.cs)
- [OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogProperty.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogProperty.cs)
- [OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogPropertyOption.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogPropertyOption.cs)
- [OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogSupplier.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetItemsEditor/OrderCatalogSupplier.cs)
- [OrderHub.Application/Features/Orders/GetPage/GetOrderPagedQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetPage/GetOrderPagedQueryHandler.cs)
- [OrderHub.Application/Features/Orders/GetPage/GetOrdersPageQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetPage/GetOrdersPageQuery.cs)
- [OrderHub.Application/Features/Orders/GetPage/OrderListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetPage/OrderListItem.cs)
- [OrderHub.Application/Features/Orders/GetPage/OrderPaymentMethodItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Orders/GetPage/OrderPaymentMethodItem.cs)
- [OrderHub.Application/Features/Products/Contracts/IProductReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Contracts/IProductReader.cs)
- [OrderHub.Application/Features/Products/Contracts/IProductRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Contracts/IProductRepository.cs)
- [OrderHub.Application/Features/Products/Contracts/ProductInfo.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Contracts/ProductInfo.cs)
- [OrderHub.Application/Features/Products/Create/CreateProductCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Create/CreateProductCommand.cs)
- [OrderHub.Application/Features/Products/Create/CreateProductHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Create/CreateProductHandler.cs)
- [OrderHub.Application/Features/Products/Create/ProductCreateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Create/ProductCreateInput.cs)
- [OrderHub.Application/Features/Products/Create/ProductPropertyInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Create/ProductPropertyInput.cs)
- [OrderHub.Application/Features/Products/Delete/DeleteProductHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Delete/DeleteProductHandler.cs)
- [OrderHub.Application/Features/Products/Get/GetProductHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Get/GetProductHandler.cs)
- [OrderHub.Application/Features/Products/Get/GetProductQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Get/GetProductQuery.cs)
- [OrderHub.Application/Features/Products/Get/ProductDetails.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Get/ProductDetails.cs)
- [OrderHub.Application/Features/Products/Get/ProductPropertySelection.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Get/ProductPropertySelection.cs)
- [OrderHub.Application/Features/Products/GetProductsInfo/GetProductsInfoQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/GetProductsInfo/GetProductsInfoQuery.cs)
- [OrderHub.Application/Features/Products/GetProductsInfo/GetProductsInfoQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/GetProductsInfo/GetProductsInfoQueryHandler.cs)
- [OrderHub.Application/Features/Products/List/ListProductsHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/List/ListProductsHandler.cs)
- [OrderHub.Application/Features/Products/List/ListProductsQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/List/ListProductsQuery.cs)
- [OrderHub.Application/Features/Products/List/ProductListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/List/ProductListItem.cs)
- [OrderHub.Application/Features/Products/List/ProductSupplierItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/List/ProductSupplierItem.cs)
- [OrderHub.Application/Features/Products/Search/SearchProductsHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Search/SearchProductsHandler.cs)
- [OrderHub.Application/Features/Products/Search/SearchProductsQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Search/SearchProductsQuery.cs)
- [OrderHub.Application/Features/Products/Update/ProductPropertyUpdate.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Update/ProductPropertyUpdate.cs)
- [OrderHub.Application/Features/Products/Update/ProductUpdateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Update/ProductUpdateInput.cs)
- [OrderHub.Application/Features/Products/Update/UpdateProductCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Update/UpdateProductCommand.cs)
- [OrderHub.Application/Features/Products/Update/UpdateProductHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Products/Update/UpdateProductHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/CategoryCreateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/CategoryCreateInput.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/CategoryEdit.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/CategoryEdit.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/CategoryInfo.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/CategoryInfo.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/CategoryListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/CategoryListItem.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/CategoryTree.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/CategoryTree.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/CategoryUpdateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/CategoryUpdateInput.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/ICategoryReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/ICategoryReader.cs)
- [OrderHub.Application/Features/Setup/Categories/Contracts/ICategoryRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/Contracts/ICategoryRepository.cs)
- [OrderHub.Application/Features/Setup/Categories/CreateCategory/CreateCategoryCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/CreateCategory/CreateCategoryCommand.cs)
- [OrderHub.Application/Features/Setup/Categories/CreateCategory/CreateCategoryCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/CreateCategory/CreateCategoryCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/DeleteCategory/DeleteCategoryCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/DeleteCategory/DeleteCategoryCommand.cs)
- [OrderHub.Application/Features/Setup/Categories/DeleteCategory/DeleteCategoryCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/DeleteCategory/DeleteCategoryCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoriesInfo/GetCategoriesInfoQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoriesInfo/GetCategoriesInfoQuery.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoriesInfo/GetCategoriesInfoQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoriesInfo/GetCategoriesInfoQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryForEdit/GetCategoryForEditQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryForEdit/GetCategoryForEditQuery.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryForEdit/GetCategoryForEditQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryForEdit/GetCategoryForEditQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryList/GetCategoryListQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryList/GetCategoryListQuery.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryList/GetCategoryListQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryList/GetCategoryListQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryParentIds/GetCategoryParentIdsQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryParentIds/GetCategoryParentIdsQuery.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryParentIds/GetCategoryParentIdsQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryParentIds/GetCategoryParentIdsQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryTree/GetCategoryTreeQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryTree/GetCategoryTreeQuery.cs)
- [OrderHub.Application/Features/Setup/Categories/GetCategoryTree/GetCategoryTreeQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetCategoryTree/GetCategoryTreeQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/GetRootCategories/GetRootCategoriesQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetRootCategories/GetRootCategoriesQuery.cs)
- [OrderHub.Application/Features/Setup/Categories/GetRootCategories/GetRootCategoriesQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetRootCategories/GetRootCategoriesQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/GetSubCategories/GetSubCategoriesQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetSubCategories/GetSubCategoriesQuery.cs)
- [OrderHub.Application/Features/Setup/Categories/GetSubCategories/GetSubCategoriesQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/GetSubCategories/GetSubCategoriesQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Categories/UpdateCategory/UpdateCategoryCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/UpdateCategory/UpdateCategoryCommand.cs)
- [OrderHub.Application/Features/Setup/Categories/UpdateCategory/UpdateCategoryCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Categories/UpdateCategory/UpdateCategoryCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Cities/Contracts/CityCreateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/Contracts/CityCreateInput.cs)
- [OrderHub.Application/Features/Setup/Cities/Contracts/CityInfo.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/Contracts/CityInfo.cs)
- [OrderHub.Application/Features/Setup/Cities/Contracts/CityListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/Contracts/CityListItem.cs)
- [OrderHub.Application/Features/Setup/Cities/Contracts/CityUpdateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/Contracts/CityUpdateInput.cs)
- [OrderHub.Application/Features/Setup/Cities/Contracts/ICityReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/Contracts/ICityReader.cs)
- [OrderHub.Application/Features/Setup/Cities/Contracts/ICityRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/Contracts/ICityRepository.cs)
- [OrderHub.Application/Features/Setup/Cities/CreateCity/CreateCityCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/CreateCity/CreateCityCommand.cs)
- [OrderHub.Application/Features/Setup/Cities/CreateCity/CreateCityCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/CreateCity/CreateCityCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Cities/DeleteCity/DeleteCityCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/DeleteCity/DeleteCityCommand.cs)
- [OrderHub.Application/Features/Setup/Cities/DeleteCity/DeleteCityCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/DeleteCity/DeleteCityCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Cities/GetAllCities/GetAllCitiesQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/GetAllCities/GetAllCitiesQuery.cs)
- [OrderHub.Application/Features/Setup/Cities/GetAllCities/GetAllCitiesQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/GetAllCities/GetAllCitiesQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Cities/GetCitiesInfo/GetCitiesInfoQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/GetCitiesInfo/GetCitiesInfoQuery.cs)
- [OrderHub.Application/Features/Setup/Cities/GetCitiesInfo/GetCitiesInfoQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/GetCitiesInfo/GetCitiesInfoQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Cities/GetCityForEdit/GetCityForEditQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/GetCityForEdit/GetCityForEditQuery.cs)
- [OrderHub.Application/Features/Setup/Cities/GetCityForEdit/GetCityForEditQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/GetCityForEdit/GetCityForEditQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Cities/UpdateCity/UpdateCityCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/UpdateCity/UpdateCityCommand.cs)
- [OrderHub.Application/Features/Setup/Cities/UpdateCity/UpdateCityCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Cities/UpdateCity/UpdateCityCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/Contracts/ClientInfo.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/Contracts/ClientInfo.cs)
- [OrderHub.Application/Features/Setup/Clients/Contracts/ClientInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/Contracts/ClientInput.cs)
- [OrderHub.Application/Features/Setup/Clients/Contracts/ClientListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/Contracts/ClientListItem.cs)
- [OrderHub.Application/Features/Setup/Clients/Contracts/IClientReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/Contracts/IClientReader.cs)
- [OrderHub.Application/Features/Setup/Clients/Contracts/IClientRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/Contracts/IClientRepository.cs)
- [OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommand.cs)
- [OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommand.cs)
- [OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/GetAllClients/GetAllClientsQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetAllClients/GetAllClientsQuery.cs)
- [OrderHub.Application/Features/Setup/Clients/GetAllClients/GetAllClientsQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetAllClients/GetAllClientsQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/GetAllClientsPaged/GetAllClientsPagedQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetAllClientsPaged/GetAllClientsPagedQuery.cs)
- [OrderHub.Application/Features/Setup/Clients/GetAllClientsPaged/GetAllClientsPagedQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetAllClientsPaged/GetAllClientsPagedQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientById/GetClientByIdQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientById/GetClientByIdQuery.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientById/GetClientByIdQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientById/GetClientByIdQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientEdit/GetClientEditQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientEdit/GetClientEditQuery.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientEdit/GetClientEditQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientEdit/GetClientEditQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientsByName/GetClientsByNameQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientsByName/GetClientsByNameQuery.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientsByName/GetClientsByNameQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientsByName/GetClientsByNameQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientsInfo/GetClientsInfoQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientsInfo/GetClientsInfoQuery.cs)
- [OrderHub.Application/Features/Setup/Clients/GetClientsInfo/GetClientsInfoQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/GetClientsInfo/GetClientsInfoQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommand.cs)
- [OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanInfo.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanInfo.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanInput.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/Contracts/DeliverymanListItem.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/Contracts/IDeliverymanReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/Contracts/IDeliverymanReader.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/Contracts/IDeliverymanRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/Contracts/IDeliverymanRepository.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommand.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommand.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenInfo/GetAllDeliverymenInfoQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenInfo/GetAllDeliverymenInfoQuery.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenInfo/GetAllDeliverymenInfoQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenInfo/GetAllDeliverymenInfoQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenList/GetAllDeliverymenListQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenList/GetAllDeliverymenListQuery.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenList/GetAllDeliverymenListQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenList/GetAllDeliverymenListQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanById/GetDeliverymanByIdQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanById/GetDeliverymanByIdQuery.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanById/GetDeliverymanByIdQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanById/GetDeliverymanByIdQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanForEdit/GetDeliverymanForEditQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanForEdit/GetDeliverymanForEditQuery.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanForEdit/GetDeliverymanForEditQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanForEdit/GetDeliverymanForEditQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymenByName/GetDeliverymenByNameQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymenByName/GetDeliverymenByNameQuery.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymenByName/GetDeliverymenByNameQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymenByName/GetDeliverymenByNameQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommand.cs)
- [OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommandHandler.cs)
- [OrderHub.Application/Features/Setup/PaymentMethods/Contracts/IPaymentMethodReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/PaymentMethods/Contracts/IPaymentMethodReader.cs)
- [OrderHub.Application/Features/Setup/PaymentMethods/Contracts/PaymentMethodListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/PaymentMethods/Contracts/PaymentMethodListItem.cs)
- [OrderHub.Application/Features/Setup/PaymentMethods/GetPaymentMethodList/GetPaymentMethodListQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/PaymentMethods/GetPaymentMethodList/GetPaymentMethodListQuery.cs)
- [OrderHub.Application/Features/Setup/PaymentMethods/GetPaymentMethodList/GetPaymentMethodListQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/PaymentMethods/GetPaymentMethodList/GetPaymentMethodListQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Properties/Contracts/IPropertyReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Contracts/IPropertyReader.cs)
- [OrderHub.Application/Features/Setup/Properties/Contracts/IPropertyRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Contracts/IPropertyRepository.cs)
- [OrderHub.Application/Features/Setup/Properties/Create/CreatePropertyCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Create/CreatePropertyCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Properties/Create/PropertyCreateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Create/PropertyCreateInput.cs)
- [OrderHub.Application/Features/Setup/Properties/Create/PropertyOptionInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Create/PropertyOptionInput.cs)
- [OrderHub.Application/Features/Setup/Properties/Delete/DeletePropertyCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Delete/DeletePropertyCommand.cs)
- [OrderHub.Application/Features/Setup/Properties/Delete/DeletePropertyHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Delete/DeletePropertyHandler.cs)
- [OrderHub.Application/Features/Setup/Properties/Get/GetPropertyQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Get/GetPropertyQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Properties/Get/PropertyDetails.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Get/PropertyDetails.cs)
- [OrderHub.Application/Features/Setup/Properties/Get/PropertyOptionItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Get/PropertyOptionItem.cs)
- [OrderHub.Application/Features/Setup/Properties/GetAll/GetPropertiesQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/GetAll/GetPropertiesQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Properties/GetAll/PropertyListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/GetAll/PropertyListItem.cs)
- [OrderHub.Application/Features/Setup/Properties/Update/PropertyOptionUpdate.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Update/PropertyOptionUpdate.cs)
- [OrderHub.Application/Features/Setup/Properties/Update/PropertyUpdateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Update/PropertyUpdateInput.cs)
- [OrderHub.Application/Features/Setup/Properties/Update/UpdatePropertyCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Properties/Update/UpdatePropertyCommandHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/IShippingCarrierReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/IShippingCarrierReader.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/IShippingCarrierRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/IShippingCarrierRepository.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierCreateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierCreateInput.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierEdit.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierEdit.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierInfo.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierInfo.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierListItem.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierUpdateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/Contracts/ShippingCarrierUpdateInput.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommand.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommandHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommand.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommandHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetAllShippingCarriersInfo/GetAllShippingCarriersInfoQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetAllShippingCarriersInfo/GetAllShippingCarriersInfoQuery.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetAllShippingCarriersInfo/GetAllShippingCarriersInfoQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetAllShippingCarriersInfo/GetAllShippingCarriersInfoQueryHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierById/GetShippingCarrierByIdQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierById/GetShippingCarrierByIdQuery.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierById/GetShippingCarrierByIdQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierById/GetShippingCarrierByIdQueryHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierForEdit/GetShippingCarrierForEditQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierForEdit/GetShippingCarrierForEditQuery.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierForEdit/GetShippingCarrierForEditQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierForEdit/GetShippingCarrierForEditQueryHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriers/GetShippingCarriersQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriers/GetShippingCarriersQuery.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriers/GetShippingCarriersQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriers/GetShippingCarriersQueryHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriersByName/GetShippingCarriersByNameQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriersByName/GetShippingCarriersByNameQuery.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriersByName/GetShippingCarriersByNameQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriersByName/GetShippingCarriersByNameQueryHandler.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommand.cs)
- [OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Suppliers/Contracts/ISupplierReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/ISupplierReader.cs)
- [OrderHub.Application/Features/Setup/Suppliers/Contracts/ISupplierRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/ISupplierRepository.cs)
- [OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierCreateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierCreateInput.cs)
- [OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierEdit.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierEdit.cs)
- [OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierInfo.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierInfo.cs)
- [OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierListItem.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierListItem.cs)
- [OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierUpdateInput.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierUpdateInput.cs)
- [OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CrateSupplierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CrateSupplierCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommand.cs)
- [OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommand.cs)
- [OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommandHandler.cs)
- [OrderHub.Application/Features/Setup/Suppliers/GetAllSuppliers/GetAllSuppliersQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetAllSuppliers/GetAllSuppliersQuery.cs)
- [OrderHub.Application/Features/Setup/Suppliers/GetAllSuppliers/GetAllSuppliersQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetAllSuppliers/GetAllSuppliersQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Suppliers/GetSupplierForEdit/GetSupplierForEditQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetSupplierForEdit/GetSupplierForEditQuery.cs)
- [OrderHub.Application/Features/Setup/Suppliers/GetSupplierForEdit/GetSupplierForEditQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetSupplierForEdit/GetSupplierForEditQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Suppliers/GetSuppliersInfo/GetSuppliersInfoQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetSuppliersInfo/GetSuppliersInfoQuery.cs)
- [OrderHub.Application/Features/Setup/Suppliers/GetSuppliersInfo/GetSuppliersInfoQueryHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetSuppliersInfo/GetSuppliersInfoQueryHandler.cs)
- [OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommand.cs)
- [OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommandHandler.cs)
- [OrderHub.Infrastructure/Features/Orders/OrderEditorLookupReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Orders/OrderEditorLookupReader.cs)
- [OrderHub.Infrastructure/Features/Orders/OrderEditorRead.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Orders/OrderEditorRead.cs)
- [OrderHub.Infrastructure/Features/Orders/OrderQueryFilters.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Orders/OrderQueryFilters.cs)
- [OrderHub.Infrastructure/Features/Orders/OrderReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Orders/OrderReader.cs)
- [OrderHub.Infrastructure/Features/Orders/ServiceCollectionExtensions.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Orders/ServiceCollectionExtensions.cs)
- [OrderHub.Infrastructure/Features/Products/ProductReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Products/ProductReader.cs)
- [OrderHub.Infrastructure/Features/Products/ProductRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Products/ProductRepository.cs)
- [OrderHub.Infrastructure/Features/Products/ServiceCollectionExtensions.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Products/ServiceCollectionExtensions.cs)
- [OrderHub.Infrastructure/Features/Setup/Categories/CategoryReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Categories/CategoryReader.cs)
- [OrderHub.Infrastructure/Features/Setup/Categories/CategoryRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Categories/CategoryRepository.cs)
- [OrderHub.Infrastructure/Features/Setup/Cities/CityReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Cities/CityReader.cs)
- [OrderHub.Infrastructure/Features/Setup/Cities/CityRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Cities/CityRepository.cs)
- [OrderHub.Infrastructure/Features/Setup/Clients/ClientReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Clients/ClientReader.cs)
- [OrderHub.Infrastructure/Features/Setup/Clients/ClientRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Clients/ClientRepository.cs)
- [OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanReader.cs)
- [OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanRepository.cs)
- [OrderHub.Infrastructure/Features/Setup/PaymentMethods/PaymentMethodReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/PaymentMethods/PaymentMethodReader.cs)
- [OrderHub.Infrastructure/Features/Setup/Properties/PropertyReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Properties/PropertyReader.cs)
- [OrderHub.Infrastructure/Features/Setup/Properties/PropertyRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Properties/PropertyRepository.cs)
- [OrderHub.Infrastructure/Features/Setup/ServiceCollectionExtensions.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/ServiceCollectionExtensions.cs)
- [OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierReader.cs)
- [OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierRepository.cs)
- [OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierReader.cs)
- [OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierRepository.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierRepository.cs)
- [OrderHub.Tests/Infrastructure/PersistenceArchitectureTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/PersistenceArchitectureTests.cs)
- [OrderHub.Tests/Infrastructure/ReadProjectionTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/ReadProjectionTests.cs)
- [OrderHub.Tests/Infrastructure/SetupPersistenceTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/SetupPersistenceTests.cs)
- [OrderHub.Tests/Infrastructure/Stores/ProductPersistenceTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/Stores/ProductPersistenceTests.cs)
- [OrderHub.Tests/Infrastructure/Stores/PropertyPersistenceTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/Stores/PropertyPersistenceTests.cs)
