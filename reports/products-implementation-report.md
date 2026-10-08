# Products implementation report

## Inspection before source changes
- Branch `feature/web-app`, revision `9efdbc77b8ed34902a078933ca34f1d888bc1dc5`; initial working tree clean (prior Clients work already belongs to this revision). No repository AGENTS.md found.
- Product contains EntityName (2–100), required trimmed uppercase Code (database varchar(10)), nonnegative Money (numeric(18,2)), required Category, optional existing Suppliers, and ProductProperty assignments. No Product name/code unique index exists.
- ProductProperty stores its own Id, ProductId, PropertyId and IsRequired only. No selected option/default/text/Boolean value is persisted on Product. Property definitions expose Text/Boolean/List and stable PropertyOption Id/Value. OrderItemProperty holds actual chosen values as strings and PropertyId. UI must not invent Product defaults or option selections; show type semantics/current options and edit assignment/required only.
- Product→Category uses Restrict. ProductSupplier is a composite-key join; product side Cascade, supplier side Restrict. ProductProperty product side Cascade, property side Restrict. Surviving assignment IDs must be retained on update.
- OrderItem→Product and invoice/quotation/proforma item→Product use Restrict. They snapshot product name, prices, quantities/totals and supplier name (Orders). OrderItemProperty preserves Value; its Property definition/name remains live. Order editor intentionally projects current category, supplier availability and property definitions. Product edits must not modify saved item/document facts.
- Draft JSON envelope contains Data.Items[].ProductId with name/price/property snapshots and no relational Product FK. Delete must check drafts as well as OrderItems and all document item tables.
- Current persisted model has no inventory/stock/Salla product mapping. Legacy integration/WPF references are not a reason to add those models. WPF requires name/code/price/category and supports property assignment/requirement; its formatted numeric-code regex is not Domain's invariant.
- Existing CQRS create/update lack comprehensive validation; update clears suppliers and mutates assignments before category validation. Rework only this Product vertical slice to validate before mutation and reconcile retained rows.
- Existing CategoryHierarchy is internal to Infrastructure and already supports arbitrary-depth Browse/Path/Options, cycle detection and deterministic order. Product-specific readers can reuse it in the same assembly without widening visibility or rebuilding traversal in Web.
- All historical Product foreign keys already Restrict in configurations/current snapshot. No schema change or migration is required.
- Baseline: 458 tests, 450 passed, 3 failed, 5 optional PostgreSQL skips. Preserve exact baseline failing tests.

## Product relationships and historical facts

```text
Product -> Category (required; Restrict; any existing hierarchy level)
Product <-> existing Suppliers (composite-key product_supplier join)
Product -> ProductProperty [Id, PropertyId, IsRequired] (owned assignment; Cascade)
ProductProperty -> Property -> PropertyOption definitions
OrderItem -> Product (Restrict), snapshot name/price/quantity/supplier name
OrderItem -> OrderItemProperty [PropertyId, Value], live Property definition/name
InvoiceItem / QuotationItem / ProformaInvoiceItem -> Product (Restrict), line snapshots
DraftRecord.Data.Items[] -> ProductId + draft line/property snapshots (JSON; no Product FK)
```

Changing current Product name, code, price, category, suppliers or property assignments does not issue writes to OrderItems, OrderItemProperties, Orders, delivery records or document lines. Tests verify persisted name, price, quantity, subtotal, supplier name, chosen list value, delivery row and each document type's line snapshots remain unchanged. Product-property removal deletes the Product's assignment only; historical values reference Property directly, not ProductProperty.

The Order editor intentionally projects the current category name, supplier choices and property definitions/options. Those live projections can reflect catalog changes; saved business facts remain unchanged. No new snapshots or history redesign was introduced.

## Actual Product-property semantics and instruction conflict

ProductProperty has no text/default/Boolean value, selected option, or option FK. It stores only assignment identity and IsRequired. Consequently the editor supports adding/removing available properties and editing required/optional configuration. Text/Boolean/List are distinguished with Arabic type labels and explanations; List shows current option values. Actual text inputs, Boolean choices and list selections remain in the Order workflow, where those values are persisted. Editable Product-level value/default controls would promise unsupported persistence, so they were not invented. Invalid Product option/value mutation tests are not applicable because the Product command contract accepts no option/value fields; tests instead verify type-aware definitions/options and unchanged historical chosen values.

Surviving ProductProperty rows keep their IDs when requirement flags change. New assignments are added, omitted assignments removed, and duplicates rejected before mutation. Property and PropertyOption definitions/IDs are never modified by this feature.

## Domain and mutation behavior

`Product.cs` now trims names consistently with the current Domain direction and exposes UpdateDetails, which validates the proposed basic data before assigning any field. Code keeps its existing trim/uppercase behavior; Money remains nonnegative and zero price is supported. Property assignments reject nonpositive PropertyId. Existing single-field APIs remain available.

Application validates names through EntityName, nonblank code/current 10-character storage limit, nonnegative price/current numeric(18,2) range and precision, selected category existence, supplier/property existence, null input and duplicate/invalid relationship IDs. Empty supplier/property lists are valid. It does not enforce the old WPF code-format regex or invent unique Product-name/code rules.

Create/update gather and validate all requested references before changing the Product graph. Update reconciles supplier composite-key relationships and property assignments instead of clearing/recreating retained rows. No Supplier/Category/Property entity is recreated. One unit-of-work SaveChanges commits each complete mutation; EF's save transaction is sufficient, so no broad manual transaction was added. A forced database-update failure proves basic data and relationship changes roll back together. Cancellation is no longer swallowed by the old broad exception catch; persistence failures become Arabic business errors.

## Category hierarchy reuse

ProductReader reuses the existing internal Infrastructure CategoryHierarchy with narrow projected CategoryHierarchyRow data and zero unused product counts. Visibility and the Categories implementation are unchanged. Product-specific GetProductEditorCategoriesQuery is authorized by Products.Write and calls Browse through the Product reader, returning the existing GetCategoriesResponse path/children models. Editor initialization uses the existing Path capability for the selected category. Web never reconstructs the hierarchy or recursively traverses category records.

The full-page editor maintains independent browser and selected category state. Entering a category only updates the breadcrumb/visible children; اختيار هذا القسم explicitly selects the current category. Root categories, parents with children, intermediate categories and leaves are equally valid. Breadcrumb buttons browse ancestors; selected value shows its full path. Tests cover 32 levels in persistence and a 40-segment server-supplied path in Web. A deleted selected category fails cleanly at save.

## Application and Infrastructure use cases

Existing Product Create/Update/Delete and Get/List/Search/Info request shapes remain available for current callers. Added:
- Index/GetProductIndexQuery: Products.Read, database search/paging with compact counts.
- Editor/GetProductEditorQuery: Products.Write, complete write-editor data including selected Product state, supplier/property definitions/options and selected category path. Missing Product returns null; create returns available lookups with no existing Product.
- Editor/GetProductEditorCategoriesQuery: Products.Write, current hierarchy browse/path response.

IProductReader and IProductRepository remain Product-specific persistence contracts. The reader gained separate index/editor implementations, while the existing detail projection now orders selected supplier/property IDs deterministically. Editor reads use a fixed number of untracked projections, not one request per category/supplier/property. The index issues count/page queries with aggregate supplier/property counts, filters by name/code/category/supplier name, orders by name then Id, and caps page size at 50.

Delete checks OrderItems, invoice items, quotation items, proforma items and all owners' persisted JSON drafts. PostgreSQL uses parameterized jsonb-array extraction; SQLite tests use the equivalent parameterized JSON query. Only existence is returned; other users' draft contents are not exposed. Blocked deletion returns `لا يمكن حذف المنتج لأنه مرتبط بطلبات أو مستندات أو مسودات.` Database Restrict foreign keys remain the final history boundary; direct SQL deletion is tested for all relational history types. Unused Product deletion removes only genuinely Product-owned supplier joins/property assignments, leaving suppliers, categories, definitions and options intact.

## Web UI

New `Products/Index.razor` provides `/products`, compact responsive MudTable columns (name/code/category/price/supplier count/property count), database search, pagination, loading/empty/error/retry states, writer-only add/edit/delete actions and delete confirmation. Search uses its button or Enter. Add navigates to `/products/create`; edit to `/products/{id}/edit`. Delete refreshes current filtered data after success and corrects a now-empty last page.

`Products/Editor/Editor.razor` implements both full-page routes with one ProductEditorState and one workflow. It is never a MudDialog. Natural scrolling and stacked responsive cards cover basic data, hierarchy browser, suppliers and property assignments. Supplier choices use searchable paginated checkboxes; choices persist across search/pages. Available properties are searchable/paged, assigned properties expose required/optional and type context, and removal does not mutate global definitions. No Supplier CRUD is launched.

The editor loads one complete state and one initial category browse through IRequestExecutor, validates through MudForm.ValidateAsync and UI state validation, guards double submit/cancel during saving, keeps failed saves open and navigates to `/products` only after persistence success or an explicit back action. Missing Product/loading/category-browse errors have retry/back paths. No Bootstrap, Tailwind, generic editor, JavaScript dependency, custom CSS, artificial fixed-height scroll area or global DataGrid conversion was introduced. Markup/C# formatting and Arabic strings were manually reviewed. Actual physical viewport/browser screenshots were not collected; responsive layout and rendered interactions are covered by source review and HTML-rendering tests.

## Authorization and architectural boundaries

Index and existing normal Product reads use Products.Read. Mutations and the complete write-editor/browse requests use Products.Write. Products.Write alone loads supplier/property/category lookups; users do not require Categories.Read, Suppliers.Read or Properties.Read. Page policies and writer actions use existing typed authorization/IRequestExecutor infrastructure. Read-only users have no writer actions. Domain/Application remain EF-independent; Web injects no persistence contract/context. No authentication, current-user, Admin, other setup-feature implementation or generic CRUD abstraction changed.

The existing request-metadata inventory test received only the necessary count update 93→96 for the three new permission-decorated requests. All metadata/exemption assertions remain intact. The two known unrelated authorization failures were not altered or weakened.

## Migration and existing metadata inconsistency

No new migration, EF configuration, schema, snapshot or designer change is necessary for Products. No migration was added/applied, and no existing migration history was edited. Both current configurations and the unchanged model snapshot protect the four Product history foreign keys with Restrict; Product-owned property/join deletion remains Cascade.

Inspection also found **pre-existing supplier-link migration metadata drift**: ProductConfiguration specifies Restrict for the supplier side, and `20261006094846_RestrictSupplierProductLinks.cs` changes that FK to Restrict, but the current model snapshot still records supplier-side Cascade. That migration file has no accompanying Designer file. Product-side history deletion rules remain consistent. This unrelated existing supplier migration/snapshot issue was documented without modifying or regenerating applied history. Future migration work must account for it rather than assuming the entire snapshot is synchronized. The Product update/delete implementation does not depend on deleting Suppliers.

## Changed/added files
- `OrderHub.Application/Features/Products/Editor/GetProductEditorCategoriesHandler.cs`
- `OrderHub.Application/Features/Products/Editor/GetProductEditorCategoriesQuery.cs`
- `OrderHub.Application/Features/Products/Editor/GetProductEditorHandler.cs`
- `OrderHub.Application/Features/Products/Editor/GetProductEditorQuery.cs`
- `OrderHub.Application/Features/Products/Index/GetProductIndexHandler.cs`
- `OrderHub.Application/Features/Products/Index/GetProductIndexQuery.cs`
- `OrderHub.Application/Features/Products/ProductInputValidation.cs`
- `OrderHub.Infrastructure/Features/Products/ProductEditorRead.cs`
- `OrderHub.Infrastructure/Features/Products/ProductIndexRead.cs`
- `OrderHub.Tests/Domain/ProductFeatureDomainTests.cs`
- `OrderHub.Tests/Infrastructure/ProductsArchitectureTests.cs`
- `OrderHub.Tests/Infrastructure/ProductsFeatureTests.cs`
- `OrderHub.Tests/Infrastructure/ProductsPermissionsTests.cs`
- `OrderHub.Tests/Web/ProductsWebTests.cs`
- `OrderHub.Web/Components/Pages/Features/Products/Editor/Editor.razor`
- `OrderHub.Web/Components/Pages/Features/Products/Editor/ProductEditorState.cs`
- `OrderHub.Web/Components/Pages/Features/Products/Index.razor`
- `OrderHub.Web/Components/Pages/Features/Products/_Imports.razor`
- `reports/products-implementation-report.md`
- `OrderHub.Application/Features/Products/Contracts/IProductReader.cs`
- `OrderHub.Application/Features/Products/Contracts/IProductRepository.cs`
- `OrderHub.Application/Features/Products/Create/CreateProductHandler.cs`
- `OrderHub.Application/Features/Products/Delete/DeleteProductHandler.cs`
- `OrderHub.Application/Features/Products/Update/UpdateProductHandler.cs`
- `OrderHub.Domain/Models/Product.cs`
- `OrderHub.Infrastructure/Features/Products/ProductReader.cs`
- `OrderHub.Infrastructure/Features/Products/ProductRepository.cs`
- `OrderHub.Tests/Infrastructure/AuthorizationIntegrationTests.cs`

Only Product implementation files, Product tests, this report, and the one request-inventory count assertion were changed. Existing Clients work and its report remain untouched.

## Verification and exact results

Verification uses `--no-restore -p:OutputPath=bin/ProductsVerification/`, avoiding the running Web app's normal locked output directory and reusing restored packages. No process was stopped, dependency installed or live database migration applied.

| Check | Result |
|---|---|
| Solution build | Passed; final incremental result 0 warnings, 0 errors |
| Focused Product tests | 45 passed, 0 failed, 0 skipped |
| Affected architecture/permission/request-metadata checks | 21 passed, 0 failed, 0 skipped |
| Full baseline suite | 458 total: 450 passed, 3 failed, 5 skipped |
| Final full suite | 503 total: 495 passed, 3 failed, 5 skipped |
| Failure comparison | Exact same three failing tests; no new regression |
| git diff --check | Passed |
| Git index | Empty; nothing staged |

Non-incremental verification builds emitted pre-existing MudBlazor/Blazor analyzer warnings in unrelated role/editor/tests. The final incremental solution build was clean.

Added test cases:
- `OrderHub.Tests.Domain.ProductFeatureDomainTests`: 6 passing cases.
- `OrderHub.Tests.Infrastructure.ProductsArchitectureTests`: 2 passing cases.
- `OrderHub.Tests.Infrastructure.ProductsFeatureTests`: 21 passing cases.
- `OrderHub.Tests.Infrastructure.ProductsPermissionsTests`: 2 passing cases.
- `OrderHub.Tests.Web.ProductsWebTests`: 14 passing cases.

Coverage includes valid/invalid Domain updates and normalization; zero price; list search/paging/order/counts; complete edit reads and missing targets; root/parent/deep categories; supplier addition/removal and stable entities; property addition/removal/requirement/identity and unchanged options; duplicate/missing references; atomic rollback; all five deletion-reference categories; direct FK enforcement and untouched snapshots; permission independence; architecture; routes and writer navigation; failed/successful saves and cancel/submit guards; hierarchy/breadcrumb state; supplier/property interactions; retryable load/browse errors; and delete confirmation/refresh.

Unchanged pre-existing failures:
- `OrderHub.Tests.Infrastructure.ProductPropertiesTests.Migration_changes_only_the_order_property_delete_action`
- `OrderHub.Tests.Web.PermissionAuthorizationTests.Application_denial_maps_to_generic_HTTP_403`
- `OrderHub.Tests.Web.PermissionAuthorizationTests.HTTP_permission_policy_denial_returns_403_without_invoking_endpoint`

The HTTP tests fail their existing response expectations; the historical migration test expects a single FK addition while that migration already has more than one. No unrelated test was fixed opportunistically.

Unchanged optional PostgreSQL skips:
- `OrderHub.Tests.Infrastructure.PostgresCategoryTests.PostgreSQL_ancestry_query_rejects_deep_cycle_and_allows_other_branch`
- `OrderHub.Tests.Infrastructure.PostgresCategoryTests.PostgreSQL_browser_uses_one_query_for_recursive_counts_and_breadcrumbs`
- `OrderHub.Tests.Infrastructure.PostgresCategoryTests.PostgreSQL_concurrent_opposite_moves_cannot_create_a_cycle`
- `OrderHub.Tests.Infrastructure.PostgresPropertyTests.PostgreSQL_counts_in_one_read_and_updates_option_identity_atomically`
- `OrderHub.Tests.Infrastructure.PostgresPropertyTests.PostgreSQL_restricts_order_references_instead_of_cascading_history`

Evidence is retained under ignored `TestResults/Products/{baseline,focused,architecture,full}/*.trx` and `TestResults/products-*.log`.

## Deferred concerns and limitations

- Product-level property values/defaults/options are absent from the current model; introducing them requires a separate business/schema decision. This task preserves assignment/IsRequired semantics.
- Current category/supplier/property definitions in the Order editor remain live projections, while saved item values/snapshots remain historical.
- Draft Product references have no relational FK. Existing drafts are protected by the JSON existence check, but concurrent new draft saves after that check can retain a missing ProductId. A guaranteed cross-workflow concurrency rule requires a coordinated draft persistence change; no unrelated schema/workflow redesign was inferred. Relational history remains protected by FKs under concurrent writes.
- No disposable PostgreSQL test server was configured. The PostgreSQL JSON branch was reviewed and compiled; complete mutation/FK behavior ran on SQLite. Existing opt-in PostgreSQL tests remain skipped.
- Supplier/property editor lookup projections intentionally load minimal definitions/options once for searchable choices. Extremely large catalogs may warrant server-side lookup paging separately; the Product management index is already database-paged.
- The pre-existing supplier-link snapshot/migration metadata mismatch needs separate review before future migration generation.
- No browser viewport screenshot validation or production database inspection/application was performed.

## Final Git status

Branch/revision remain the initial `feature/web-app` / `9efdbc77b8ed34902a078933ca34f1d888bc1dc5`. Final status before filling this already-untracked report:
```text
 M OrderHub.Application/Features/Products/Contracts/IProductReader.cs
 M OrderHub.Application/Features/Products/Contracts/IProductRepository.cs
 M OrderHub.Application/Features/Products/Create/CreateProductHandler.cs
 M OrderHub.Application/Features/Products/Delete/DeleteProductHandler.cs
 M OrderHub.Application/Features/Products/Update/UpdateProductHandler.cs
 M OrderHub.Domain/Models/Product.cs
 M OrderHub.Infrastructure/Features/Products/ProductReader.cs
 M OrderHub.Infrastructure/Features/Products/ProductRepository.cs
 M OrderHub.Tests/Infrastructure/AuthorizationIntegrationTests.cs
?? OrderHub.Application/Features/Products/Editor/
?? OrderHub.Application/Features/Products/Index/
?? OrderHub.Application/Features/Products/ProductInputValidation.cs
?? OrderHub.Infrastructure/Features/Products/ProductEditorRead.cs
?? OrderHub.Infrastructure/Features/Products/ProductIndexRead.cs
?? OrderHub.Tests/Domain/ProductFeatureDomainTests.cs
?? OrderHub.Tests/Infrastructure/ProductsArchitectureTests.cs
?? OrderHub.Tests/Infrastructure/ProductsFeatureTests.cs
?? OrderHub.Tests/Infrastructure/ProductsPermissionsTests.cs
?? OrderHub.Tests/Web/ProductsWebTests.cs
?? OrderHub.Web/Components/Pages/Features/Products/
?? reports/products-implementation-report.md

```
Nothing was staged, committed, pushed, reset, rebased or switched to another branch. Build/test evidence is ignored.

Suggested Conventional Commit message only:
```text
feat(products): add full-page editor with hierarchical category selection
```

