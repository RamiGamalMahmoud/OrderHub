# Task 00 — baseline and impact map

## Verification

- Branch: `feature/web-app`; baseline commit: `eedca10`.
- Existing implementation plan is included in this task's commit.
- Baseline: `dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-restore --verbosity quiet -p:WarningLevel=0`.
- Result: 532 tests; 523 passed, 3 failed, 6 optional PostgreSQL tests skipped.
- Pre-existing failures: `PermissionAuthorizationTests.Application_denial_maps_to_generic_HTTP_403`, `PermissionAuthorizationTests.HTTP_permission_policy_denial_returns_403_without_invoking_endpoint`, `ProductPropertiesTests.Migration_changes_only_the_order_property_delete_action`.
- The migration assertion expects two operations, but the existing migration also renames the users/roles foreign key. Do not modify an already-applied historical migration to satisfy this assertion; update the assertion to verify the actual intended constraints.

## Local database audit

Read-only transaction against configured development database `orderhub`, PostgreSQL 18.6. This is not evidence about a different production database. No data or schema was modified. No credentials or personal records were printed.

| Data | Count |
|---|---:|
| Orders | 15 |
| Order items | 18 |
| Delivery steps | 6 |
| Invoices | 1 |
| Quotations | 0 |
| Proforma invoices | 0 |

| Current status | Method | Orders |
|---|---|---:|
| Pending | DeliveryChain | 2 |
| Pending | DeliveryMan | 2 |
| Pending | Pickup | 6 |
| Pending | ShippingCompany | 4 |
| Processing | Pickup | 1 |

Invalid/null/infinite order creation timestamps: 0. Missing delivery methods: 0. Invalid step ranks/types/handler combinations: 0. Duplicate order numbers: 0.

14 applied migrations, through `20261006092315_RestrictPropertyDeletionForOrders`. Repository migration `20261006094846_RestrictSupplierProductLinks` is not applied here. Include it in isolated migration rehearsal, without applying anything to this database during development.

Observed orders/steps schema matches the lifecycle baseline: status strings, default Pending, nullable order method/handler/payment/source fields, UTC-capable timestamps, and no history/version/archive/step status columns.

This dataset has no Delivered/Cancelled orders. Test fixtures must explicitly cover those migration branches, missing legacy methods, empty chains, and rollback. The audit is a point-in-time observation; repeat before eventual cutover.

## Impact map

| Area | Action | Integrity requirement |
|---|---|---|
| Order and OrderDeliveryStep | Modify | central rules, protected execution, completed preservation |
| OrderStatus / DeliveryStatus | Replace assumptions | stable numeric meanings; migrate Pending strings before removal |
| CreateOrder | Modify | Processing and history share creation time; keep draft linking atomic |
| UpdateOrder / payment | Modify | whole-order edit lock and concurrency, retain step identity and item snapshots |
| ChangeStatus handlers | Replace | no unrestricted status writes or missing reasons/history |
| DeleteOrder | Replace | irreversible archive; never delete persisted order children |
| Repository + UnitOfWork | Extend | shared context, version classification, atomic staging/reordering |
| EF configurations + snapshot | Extend | additive lifecycle schema; preserve business columns and FKs |
| Read projections | Modify | missing/null safe; explicit archived reads; paged history |
| Details / editor / list | Modify | read-safe routes, locked edit, working filters |
| Workflow / dialogs / archive | Add | actions from rules, mixed direct-delivery results, local browser time |
| Documents + draft linking | Keep behavior | no status linkage; archived orders remain accessible to document reads |
| Catalog/contact usage checks | Preserve | archived references still prevent destructive deletion |
| Existing tests | Modify + extend | incompatible old behavior becomes explicit rejection tests |
| Old WPF/API writers | Close incompatible entries | no deployed old writer bypassing version/history/rules |

## Migration decisions and comparisons

- Nonterminal old orders become Processing, retaining original status as migration provenance.
- Delivered/Cancelled keep their final state; creation baseline then final baseline after one second.
- Existing step IDs remain; status is Pending/Completed/Cancelled according to the parent migration policy.
- Null actor and missing reason are allowed only for marked migration facts, not new operations.
- Compare order/item/property/step/document IDs, values, timestamps and relationships before/after using a restored isolated database.
- Do not derive operational status from the invoice or silently fix invalid source data.
- PostgreSQL isolated test connection must be configured separately from the audited source; no tests may reset or migrate the audited source.

## Outcome

Task 00 code baseline, impact map and local read-only audit complete. Production cutover still requires its own refreshed audit and verified backup/restore. Tasks 01 onward can proceed with isolated verification.
