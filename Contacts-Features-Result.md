# Contacts Features Result

## Summary

Implemented Suppliers, Deliverymen and Shipping Carriers as three independent vertical slices. Each has one Index route, its own unified Create/Edit dialog, local search, feature-specific columns and deletion confirmation. No shared contact abstraction, editor, state, validation helper, contract, repository or Domain base type was introduced.

- Date: 2026-10-06.
- Branch: `feature/web-app`.
- Reviewed HEAD: `6f0c98f56a24b156a923b6d2142d8a7debd209fc`.
- Changes are unstaged. No stage, commit, push, branch change, reset or rebase was performed.
- No deployment or production database update was performed.
- One focused migration is required for Supplier product-link deletion protection; it has **not been applied**.

## Architecture and review

Reviewed each model, its value objects, CRUD handlers/contracts, repositories/readers, EF mappings, routes/navigation and existing tests separately. Categories, Cities and Product Properties supplied the existing dialog, executor, authorization and presentation conventions. Retired WPF editors were used only to establish field/behavior intent; retired API/WPF projects were not restored or built.

Domain continues to own state and behavior without persistence dependencies. Application owns orchestration, input validation and permission metadata without EF implementation. Infrastructure owns projections, repositories and EF configuration. Web uses `IRequestExecutor` for business requests and the existing `PermissionAccess` service for mutation visibility. Existing cookies, current-user handling, admin bypass and centralized permission enforcement were untouched. Existing architecture tests were not weakened.

The three editors use the existing City contracts and reader through three feature-owned city lookup queries. Each lookup requires that editor's own Write permission, so a writer can complete its form without an unrelated Cities grant. No city editing capability is granted. There is no new common execution or authorization mechanism.

The protected-request inventory expectation changed from 89 to 92 solely for those three typed, protected requests. Existing repositories, scoped context and UnitOfWork remain in use.

## Combined build and validation

| Check | Result |
|---|---|
| Active production solution build, `dotnet build OrderHub.sln --no-restore` | Passed; Domain, Application, Infrastructure and Web; 0 errors, 2 existing warnings |
| Supplier focused filter | 40 passed |
| Deliveryman focused filter | 27 passed |
| ShippingCarrier focused filter | 30 passed |
| Combined Supplier/Deliveryman/ShippingCarrier filter | 85 passed, 0 failed, 0 skipped |
| Architecture filter | 14 passed |
| Full suite before changes | 339 total: 331 passed, 3 failed, 5 skipped |
| Full suite after changes | 417 total: 409 passed, 3 failed, 5 skipped |
| New test cases | 78 added; all pass |
| Migration SQL generation and manual review | Passed; only Supplier product-link FK is changed |
| EF pending-model check | No model changes since the last migration |
| Tracked diff whitespace check | Passed |
| Additional trailing-whitespace scan of added/modified source | Passed |
| Staged diff | Empty |

Focused filters overlap because the permission-independence test class contains all three feature names. Their counts should not be added. Final combined and full runs include the final source changes.

Local evidence: [contacts-build.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-build.log), [contacts-supplier-tests.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-supplier-tests.log), [contacts-deliveryman-tests.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-deliveryman-tests.log), [contacts-shippingcarrier-tests.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-shippingcarrier-tests.log), [contacts-focused-tests.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-focused-tests.log), [contacts-architecture-tests.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-architecture-tests.log), [contacts-baseline-tests.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-baseline-tests.log), [contacts-full-tests.log](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/contacts-full-tests.log). These local diagnostic logs are ignored by Git.

### Pre-existing failures and warnings

Exactly the same three failures were present in the pre-edit baseline and remain in the final suite:

1. `PermissionAuthorizationTests.HTTP_permission_policy_denial_returns_403_without_invoking_endpoint`: expects HTTP 403, receives 302.
2. `PermissionAuthorizationTests.Application_denial_maps_to_generic_HTTP_403`: expects the generic Arabic denial body, receives an empty body.
3. `ProductPropertiesTests.Migration_changes_only_the_order_property_delete_action`: the existing Property migration contains both a Property FK and a User/Role FK, while the test expects one FK operation.

These unrelated implementations/tests/migrations were left unchanged. The five skips are the existing optional PostgreSQL Category and Property integration tests; their database environment was not configured.

Production compilation reports two existing MUD0002 warnings for `AlignItems` on MudGrid in the Administration Roles editor. Test compilation also reports existing BL0005/BL0006 warnings in other Web tests. No warning suppression was added, and no warning was reported for the new feature source.

## Suppliers

1. **Exact Domain model:** Supplier has EntityName, optional Phone and Address relationships, and optional owned BusinessHours. Name-only suppliers already exist through the Products workflow. Full-profile Create/Update still requires a city, street, phone/country and opening/closing times. Name is 2–100 trimmed characters. Phone uses the existing Domain phone rules; country prefix is also constrained to its existing storage length of 4. Street is required and at most 150 characters. Closing time cannot precede opening time; equal times remain valid.
2. **Relationships:** Products use the `product_supplier` join. OrderItem holds a Supplier FK and a separate SupplierName snapshot. Address points to City; Phone is a separate entity. No active Supplier Telegram relationship or direct commercial-document Supplier FK was found.
3. **Existing behavior reviewed:** Existing full-profile handlers/WPF fields, name-only Product-created suppliers, contact mutation semantics, OrderItem/Product delete guards and lack of unique name/phone indexes were reviewed. Duplicate names/contacts remain allowed.
4. **Domain changes:** UpdateName trims before constructing EntityName. Existing behavior methods and constructors remain; no public setters or new base type.
5. **Application changes:** Added Supplier-only validation and protected editor-city query. Create/Update reject invalid fields and missing city/target before mutation, return clear Arabic failures and save once. The misspelled `CrateSupplierCommandHandler` was replaced by `CreateSupplierCommandHandler`. Command payloads use `Input`. SupplierEdit hours are nullable so missing legacy hours are represented honestly.
6. **Infrastructure changes:** Reader uses untracked scalar projections, deterministic Name/Id ordering, null-safe contact/address data and a targeted nullable edit result. Supplier-side Product join deletion is now Restrict.
7. **Web implementation:** `/suppliers`; columns Name, Phone and Address; local Name/Phone/Address search, pager, loading/empty/error/retry and Write-gated actions. Existing NavMenu already points to the correct route.
8. **Unified dialog:** SupplierId null creates; a value loads the targeted profile for edit. Own SupplierEditorState maps read data to commands. City/address/contact fields and a separate hours group use dense controls. Existing name-only profiles open safely with empty contact/hour fields that must be completed before saving.
9. **Delete rules:** Existing guards reject suppliers referenced by Products or OrderItems. Missing records return a clear failure. No Products, Orders, OrderItems or contact records are cascaded from this CRUD deletion.
10. **Authorization:** Suppliers.Read protects list/detail and route. Suppliers.Write protects Create/Update/Delete and its editor-city lookup. Other feature permissions do not grant access.
11. **Persistence/query strategy:** Scalar list projection has no per-row query or aggregate Include. Updates retain existing Phone/Address IDs and initialize absent relationships. One UnitOfWork save uses the existing EF transaction. Failed multi-entity saves leave persisted name, street and phone unchanged.
12. **Tests:** Empty/sorted/null-profile reads, completing a name-only profile, missing target, valid/invalid create and update, duplicate semantics, contact identity, Product/OrderItem guards, direct SQL FK protection, supplier snapshot preservation, unused/missing delete, atomic failure, migration scope, permission independence and rendered dialog/index workflows. Supplier-focused result: 40 passed.
13. **Files added/changed/deleted:** See the exact inventory below; shared review-support files are listed separately.
14. **Deferred risks:** Overnight business hours remain unsupported by the existing Domain rule. No uniqueness policy was invented. The optional contact FKs permit shared references in the schema; existing in-place update semantics are retained, and contact rows are deliberately not automatically deleted. Production PostgreSQL rollout remains a separate manual operation.

### Supplier files

- Modified: [OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierEdit.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/Contracts/SupplierEdit.cs)
- Deleted: `OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CrateSupplierCommandHandler.cs`
- Modified: [OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommand.cs)
- Modified: [OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommand.cs)
- Modified: [OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommandHandler.cs)
- Modified: [OrderHub.Domain/Models/Supplier.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Domain/Models/Supplier.cs)
- Modified: [OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Suppliers/SupplierReader.cs)
- Added: [OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommandHandler.cs)
- Added: [OrderHub.Application/Features/Setup/Suppliers/GetEditorCities/GetSupplierEditorCitiesHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetEditorCities/GetSupplierEditorCitiesHandler.cs)
- Added: [OrderHub.Application/Features/Setup/Suppliers/GetEditorCities/GetSupplierEditorCitiesQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/GetEditorCities/GetSupplierEditorCitiesQuery.cs)
- Added: [OrderHub.Application/Features/Setup/Suppliers/SupplierInputValidation.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Suppliers/SupplierInputValidation.cs)
- Added: [OrderHub.Infrastructure/Migrations/20261006094846_RestrictSupplierProductLinks.Designer.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Migrations/20261006094846_RestrictSupplierProductLinks.Designer.cs)
- Added: [OrderHub.Infrastructure/Migrations/20261006094846_RestrictSupplierProductLinks.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Migrations/20261006094846_RestrictSupplierProductLinks.cs)
- Added: [OrderHub.Tests/Infrastructure/SupplierFeatureTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/SupplierFeatureTests.cs)
- Added: [OrderHub.Tests/Web/SupplierWebTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Web/SupplierWebTests.cs)
- Added: [OrderHub.Web/Components/Pages/Features/Suppliers/Editor/SupplierEditorDialog.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Suppliers/Editor/SupplierEditorDialog.razor)
- Added: [OrderHub.Web/Components/Pages/Features/Suppliers/Editor/SupplierEditorState.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Suppliers/Editor/SupplierEditorState.cs)
- Added: [OrderHub.Web/Components/Pages/Features/Suppliers/Index.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Suppliers/Index.razor)
- Added: [OrderHub.Web/Components/Pages/Features/Suppliers/_Imports.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Suppliers/_Imports.razor)

## Deliverymen

1. **Exact Domain model:** Deliveryman has EntityName, a required City relationship and an optional plain string PhoneNumber. It does not use Supplier/Carrier Phone entities or street/address fields. Name is 2–100 trimmed characters. Optional phone remains arbitrary text within the existing 15-character storage limit; no country code or numeric phone rule was imposed.
2. **Relationships:** Order.DeliverymanId and OrderDeliveryStep.DeliverymanId refer to Deliveryman. Its City FK is required. No active Telegram navigation/link ownership or direct commercial-document FK was found.
3. **Existing behavior reviewed:** Existing Create/Update contracts, city lookup, free-text phone behavior, order/step delete guards, search readers and retired WPF fields were reviewed independently. No unique name or phone index exists; duplicates remain allowed.
4. **Domain changes:** PhoneNumber now has a private setter and ChangePhoneNumber behavior. Constructor and updates use that method; name is trimmed and city rejects null. Phone stays optional and is trimmed, with its storage length enforced.
5. **Application changes:** Deliveryman-only input validation and protected editor-city query. Own handlers validate before mutation, handle missing city/target clearly and catch persistence failures.
6. **Infrastructure changes:** List projection is untracked and ordered by Name then Id. Existing name search, targeted edit, ID read and info-reader contracts remain available.
7. **Web implementation:** `/deliverymen`; Name, City and Phone columns; local search across those fields, pager, state handling and own Write-gated actions. Navigation route was already correct.
8. **Unified dialog:** DeliverymanId selects Create/Edit. Own state contains Name, CityId and optional PhoneNumber. The compact dialog uses ExtraSmall width and requires a selected city. No supplier hours, address or carrier-cost fields were copied into this feature.
9. **Delete rules:** Existing guards reject any Order or DeliveryStep reference. Unused deliverymen can be deleted; missing IDs fail clearly. No orders, delivery steps or cities are deleted.
10. **Authorization:** Deliverymen.Read protects its route/list/detail; Deliverymen.Write protects its mutations and editor-city lookup. Supplier or Carrier grants cannot substitute.
11. **Persistence/query strategy:** Untracked projection reads and a targeted entity update through the existing repository. One save, no row-loop queries or direct Web database access. Historical Order/DeliveryStep IDs remain attached to the same Deliveryman after master edits.
12. **Tests:** Ordering/projections, optional and arbitrary phone text, duplicates, invalid name/city/length on create and update, missing targets, Order and DeliveryStep guards, historical identity preservation, unused delete, own permission boundaries and rendered index/dialog workflows. Deliveryman-focused result: 27 passed.
13. **Files added/changed/deleted:** Listed below; no Deliveryman migration was needed.
14. **Deferred risks:** The model has no deliveryman-name snapshots for these relationships. Existing screens may resolve the current master name by ID; saved history rows are not rewritten. Existing optional phone semantics were intentionally preserved.

### Deliveryman files

- Modified: [OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommandHandler.cs)
- Modified: [OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommandHandler.cs)
- Modified: [OrderHub.Domain/Models/Deliveryman.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Domain/Models/Deliveryman.cs)
- Modified: [OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/Deliverymen/DeliverymanReader.cs)
- Added: [OrderHub.Application/Features/Setup/Deliverymen/DeliverymanInputValidation.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/DeliverymanInputValidation.cs)
- Added: [OrderHub.Application/Features/Setup/Deliverymen/GetEditorCities/GetDeliverymanEditorCitiesHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetEditorCities/GetDeliverymanEditorCitiesHandler.cs)
- Added: [OrderHub.Application/Features/Setup/Deliverymen/GetEditorCities/GetDeliverymanEditorCitiesQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/Deliverymen/GetEditorCities/GetDeliverymanEditorCitiesQuery.cs)
- Added: [OrderHub.Tests/Infrastructure/DeliverymanFeatureTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/DeliverymanFeatureTests.cs)
- Added: [OrderHub.Tests/Web/DeliverymanWebTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Web/DeliverymanWebTests.cs)
- Added: [OrderHub.Web/Components/Pages/Features/Deliverymen/Editor/DeliverymanEditorDialog.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Deliverymen/Editor/DeliverymanEditorDialog.razor)
- Added: [OrderHub.Web/Components/Pages/Features/Deliverymen/Editor/DeliverymanEditorState.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Deliverymen/Editor/DeliverymanEditorState.cs)

## Shipping Carriers

1. **Exact Domain model:** ShippingCarrier has EntityName, Money ShippingCost, optional separate Phone and Address relationships. Full-profile CRUD requires city/address/contact. ShippingCost must be nonnegative and within numeric(18,2) storage range. Name and contact validation use the same existing Domain concepts as before, with Carrier-owned orchestration.
2. **Relationships:** Order.ShippingCarrierId and OrderDeliveryStep.ShippingCarrierId refer to the carrier. Address points to City; Phone is a separate entity. No active Telegram navigation or direct commercial-document carrier FK was found.
3. **Existing behavior reviewed:** Existing required full-profile inputs, cost behavior, in-place contact updates, order/step guards and retired WPF fields were reviewed. There are no unique name/phone indexes; duplicates remain allowed.
4. **Domain changes:** Rename trims before EntityName construction. Existing Money, address and phone behavior was retained; existing compact control flow was expanded for readability.
5. **Application changes:** Added Carrier-owned validation and editor-city query. Create/Update validate all fields and city before mutation; missing carrier and persistence failures produce clear Arabic Results. Payloads use `Input`.
6. **Infrastructure changes:** Carrier-only untracked scalar projection for list/name/ID reads, deterministic ordering and null-safe optional contact data. Existing contracts/repositories remain.
7. **Web implementation:** `/shipping-carriers`; Name, Phone, Address and ShippingCost columns; local Name/Phone/Address search, pager and full list states. Mutations require this feature's Write access.
8. **Unified dialog:** ShippingCarrierId selects Create/Edit. Own state maps name/cost/city/street/country/phone. Small width, dense numeric cost control, responsive phone grouping and a scrollable content area.
9. **Delete rules:** Existing guards reject Order or DeliveryStep references. Missing or persistence-race failures remain recoverable. Unused carrier deletion does not remove historical records, cities or contact entities.
10. **Authorization:** ShippingCarriers.Read protects route/list/detail. ShippingCarriers.Write protects Create/Update/Delete and the editor-city query. Permissions remain independent.
11. **Persistence/query strategy:** Scalar reads, targeted aggregate update, existing Phone/Address IDs preserved, missing optional contact relationships initialized and one atomic save. Name/cost/address cannot remain half-persisted after a simulated phone update failure.
12. **Tests:** Ordered projections, contact/cost reads, duplicate semantics, invalid name/phone/address/city/cost on create and update, missing target, contact identity, atomic failure, Order/DeliveryStep guards and preserved history IDs, unused/missing delete, permission independence and rendered UI flows. Carrier-focused result: 30 passed.
13. **Files added/changed/deleted:** Listed below; no Carrier migration was needed.
14. **Deferred risks:** The existing model references master data by ID; future display of the master name/current fee may reflect its current value. This implementation does not recalculate or rewrite saved order totals, steps or snapshots. Schema-permitted shared contact references retain existing mutation semantics.

### Carrier files

- Modified: [OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommand.cs)
- Modified: [OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommandHandler.cs)
- Modified: [OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommand.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommand.cs)
- Modified: [OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommandHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommandHandler.cs)
- Modified: [OrderHub.Domain/Models/ShippingCarrier.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Domain/Models/ShippingCarrier.cs)
- Modified: [OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierReader.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Features/Setup/ShippingCarriers/ShippingCarrierReader.cs)
- Added: [OrderHub.Application/Features/Setup/ShippingCarriers/GetEditorCities/GetShippingCarrierEditorCitiesHandler.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetEditorCities/GetShippingCarrierEditorCitiesHandler.cs)
- Added: [OrderHub.Application/Features/Setup/ShippingCarriers/GetEditorCities/GetShippingCarrierEditorCitiesQuery.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/GetEditorCities/GetShippingCarrierEditorCitiesQuery.cs)
- Added: [OrderHub.Application/Features/Setup/ShippingCarriers/ShippingCarrierInputValidation.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Application/Features/Setup/ShippingCarriers/ShippingCarrierInputValidation.cs)
- Added: [OrderHub.Tests/Infrastructure/ShippingCarrierFeatureTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/ShippingCarrierFeatureTests.cs)
- Added: [OrderHub.Tests/Web/ShippingCarrierWebTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Web/ShippingCarrierWebTests.cs)
- Added: [OrderHub.Web/Components/Pages/Features/ShippingCarriers/Editor/ShippingCarrierEditorDialog.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/ShippingCarriers/Editor/ShippingCarrierEditorDialog.razor)
- Added: [OrderHub.Web/Components/Pages/Features/ShippingCarriers/Editor/ShippingCarrierEditorState.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/ShippingCarriers/Editor/ShippingCarrierEditorState.cs)
- Added: [OrderHub.Web/Components/Pages/Features/ShippingCarriers/Index.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/ShippingCarriers/Index.razor)
- Added: [OrderHub.Web/Components/Pages/Features/ShippingCarriers/_Imports.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/ShippingCarriers/_Imports.razor)

## Foreign-key and history review

| Relationship | Final delete behavior | Review/result |
|---|---|---|
| product_supplier.suppliers_id → suppliers.id | Restrict | Changed from Cascade; prevents automatic removal of supplier associations |
| product_supplier.products_id → products.id | Cascade | Existing Product-owned join cleanup retained; does not delete Supplier |
| order_items.supplier_id → suppliers.id | Restrict | Existing FK retained; application guard retained |
| orders.deliveryman_id → deliverymen.id | Restrict | Retained; order reference guard |
| order_delivery_steps.deliveryman_id → deliverymen.id | Restrict | Retained; step reference guard |
| orders.shipping_carrier_id → shipping_carriers.id | Restrict | Retained; order reference guard |
| order_delivery_steps.shipping_carrier_id → shipping_carriers.id | Restrict | Retained; step reference guard |
| deliverymen.deliveryman_city_id → cities.id | Restrict | Retained; required city |
| suppliers.address_id → addresses.id | NoAction | Retained; optional reference, no automatic contact deletion |
| suppliers.phone_id → phones.id | NoAction | Retained; optional reference |
| shipping_carriers.address_id → addresses.id | NoAction | Retained; optional reference |
| shipping_carriers.phone_id → phones.id | NoAction | Retained; optional reference |
| addresses.city_id → cities.id | Restrict | Retained; required city |
| orders → order_items/order_delivery_steps via OrderId | Cascade | Existing Order-owned cleanup retained; never triggered by contact CRUD |
| order_items.product_id → products.id | Restrict | Existing history protection retained |

SupplierName is a saved OrderItem snapshot and remains unchanged after Supplier rename. Deliveryman/Carrier references retain IDs; no historical Order/DeliveryStep rows are deleted or rewritten by master updates. Reviewed commercial-document item Product relationships already restrict Product deletion; no direct references to the three masters were found.

There are **no active Telegram relationships** in the current Domain/EF model for these features. Historical Telegram migration files and an unused Telegram options type still exist, but no Telegram workflow, link controls or schema changes were added.

## Migration and rollout

New migration: [OrderHub.Infrastructure/Migrations/20261006094846_RestrictSupplierProductLinks.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Migrations/20261006094846_RestrictSupplierProductLinks.cs).

Up drops and re-adds only `fk_product_supplier_suppliers_suppliers_id` with ON DELETE RESTRICT. Down restores its previous CASCADE behavior. Generated designer and current snapshot were reviewed; the snapshot changes only the Supplier side of that join. The EF pending-model check confirms no remaining drift.

PostgreSQL SQL was generated between `RestrictPropertyDeletionForOrders` and `RestrictSupplierProductLinks` using Infrastructure as both project and startup project. [OrderHub.Infrastructure/obj/contacts-migration.sql](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/obj/contacts-migration.sql) contains a transaction, that FK replacement and the EF history insert only. An initial Web startup invocation was unsupported because Web lacks the design package; using the existing Infrastructure design-time setup succeeded without adding dependencies.

**Not applied to any production database.** SQLite tests create their own isolated databases from the current model and verify direct deletion is rejected. Actual PostgreSQL application was not performed.

Rollout: review the SQL and existing migration history, take the normal database backup, ensure the prerequisite migration chain matches the target environment, and apply through the project's normal controlled migration procedure. FK replacement can require table locks, so schedule it appropriately. Until this migration is installed, application guards reject referenced deletion, but the old database FK can still cascade join rows in a direct deletion/race. Downgrade reintroduces that behavior; it should be a deliberate decision.

## Dialog and UI review

Manually reviewed all three Razor files and state mappings against existing Cities/Properties patterns. Each dialog handles loading, missing target, no available cities, retry, form validation, saving, business errors, unexpected errors, authorization denial, cancellation and successful close. Save guards are set before the first await; repeated submit/cancel is blocked while saving. Escape/backdrop/close-button dismissal is disabled. The Index refreshes only for a successful dialog result or successful confirmed delete.

Rendered-component tests independently cover each feature's Create/Edit state, command choice, validation/no-write, recoverable business failure and retry, unexpected failure/denial, cancellation, repeated submit, deletion confirmation/failure, refresh and read-only action visibility. Separate tests exercise real permission roles without Cities grants and reject cross-feature reads/writes.

Responsive decisions: dense MudTable with Small breakpoint and DataLabels; wrapping names/address/phone cells; compact action buttons; bounded local search width; Small Supplier/Carrier dialogs and ExtraSmall Deliveryman dialog; xs=12 responsive fields with wider-screen contact/time groups; max-height 60vh scroll inside dialog content with actions outside. Global Arabic/RTL conventions remain in use. No UI library, JavaScript or general redesign was introduced.

**Live browser viewport inspection was not performed.** Source and rendered markup were reviewed, but exact laptop/narrow-screen layout and horizontal-overflow behavior still need a live visual check in the authenticated application. No claim of pixel-level visual verification is made.

Formatting was reviewed manually: separate state fields, expanded Domain control-flow blocks, one component attribute per line, named workflow methods, grouped editor content, readable validation/mapping and multiline logging. No new Common component or cross-feature runtime implementation exists.

## Supporting files and scope confirmation

- Modified: [OrderHub.Infrastructure/Configurations/ProductConfiguration.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Configurations/ProductConfiguration.cs)
- Modified: [OrderHub.Infrastructure/Migrations/AppDbContextModelSnapshot.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Infrastructure/Migrations/AppDbContextModelSnapshot.cs)
- Modified: [OrderHub.Tests/Infrastructure/AuthorizationIntegrationTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/AuthorizationIntegrationTests.cs)
- Added: [OrderHub.Tests/Infrastructure/SupplierDeliverymanShippingCarrierPermissionsTests.cs](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Tests/Infrastructure/SupplierDeliverymanShippingCarrierPermissionsTests.cs)
- Added: [OrderHub.Web/Components/Pages/Features/Deliverymen/Index.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Deliverymen/Index.razor)
- Added: [OrderHub.Web/Components/Pages/Features/Deliverymen/_Imports.razor](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/OrderHub.Web/Components/Pages/Features/Deliverymen/_Imports.razor)
- Added: [Contacts-Features-Result.md](C:/Users/ramig/OneDrive/Documents/Work/OrderHub/OrderHub/Contacts-Features-Result.md) (this report).

The ProductConfiguration adjustment is limited to the Supplier-side join FK, and AuthorizationIntegrationTests changes only the protected-request count. Snapshot/generated migration changes are limited to that FK. Categories, Cities, Product Properties, Clients, Orders, authentication, Telegram and deployment implementations were not changed. Existing order/history models and tests remain intact. All three features retain independent runtime implementation and feature-owned files.
