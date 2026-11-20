# Database

PostgreSQL is the only supported provider for development, tests, and production.
SQLite and EF Core InMemory are not used anywhere in the solution.

## Connection and naming

- The Web host reads `ConnectionStrings:DefaultConnection` and fails to start when it
  is missing.
- The model uses snake_case table and column names (`EFCore.NamingConventions`). Raw SQL
  must use those names and quote them when needed.
- Timestamps are `timestamp with time zone`; values must be UTC. Npgsql rejects
  non-UTC `DateTime` values for those columns.
- Uploaded files and generated documents live under `ApplicationData:RootPath`, not in
  the database or the repository.

## Migrations

All migrations live in `OrderHub.Infrastructure/Migrations`. Some are hand-written and
have no `.Designer.cs`:

- `20261006094846_RestrictSupplierProductLinks.cs`
- `20261030000000_AddOrderDiscountAndDeliveryCosts.cs`
- `20261030010000_AddCommercialDocumentFinancialSnapshots.cs`

The model snapshot must still match the hand-written migrations. Do not hand-edit a
migration that has already been applied; add a new migration instead. If an applied
migration is genuinely broken, report it rather than rewriting history.

The app does **not** apply migrations at startup (`IDatabaseService.MigrateAsync` has no
caller). Apply them explicitly or through the deployment SQL.

Use `OrderHub.Infrastructure` as both the project and the startup project. Using
`OrderHub.Web` as startup fails because EF Design is referenced only by Infrastructure.

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project OrderHub.Infrastructure --startup-project OrderHub.Infrastructure --output-dir Migrations
dotnet ef database update --project OrderHub.Infrastructure --startup-project OrderHub.Infrastructure
```

The design-time factory (`DesignTimeAppDbContextFactrory.cs`) hardcodes a localhost
database (`orderhub-dev` on port `5432`), so `database update` targets that local
database. Never run it against production. For any other database, pass an explicit
connection string, for example:

```powershell
dotnet ef database update --project OrderHub.Infrastructure --startup-project OrderHub.Infrastructure --connection "<connection string>"
```

`dotnet ef` builds Debug output by default. Use a fresh build for verification;
`--no-build` can load stale assemblies.

## Test database

Database-backed tests use one dedicated PostgreSQL database, `orderhub_test`, on host
port `5433`. A compose file starts it:

```powershell
$env:TEST_DB_PASSWORD = '<local test password>'
docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml up -d --wait
$env:ORDERHUB_TEST_POSTGRES = 'Host=localhost;Port=5433;Database=orderhub_test;Username=orderhub_test;Password=<local test password>;Include Error Detail=true'
dotnet test OrderHub.sln --configuration Release
```

The compose file:

- uses `postgres:18`, database/user `orderhub_test`, and the password from
  `TEST_DB_PASSWORD`;
- publishes `5433:5432` so it cannot collide with a Development instance on `5432`;
- keeps data in the named volume `orderhub-test-data`;
- has a `pg_isready` health check.

The test runner never starts, stops, or deletes the container or its volume. Stop it
with `docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml down`
(never `down -v` unless you intend to discard the volume).

`PostgresTestDatabase` enforces the harness:

- `ORDERHUB_TEST_POSTGRES` is required; the run fails without it.
- The database name must be exactly `orderhub_test`; any other target is refused.
- A PostgreSQL advisory lock prevents two test processes from sharing the schema.
- Every migration is applied with `Database.Migrate()` on first use, so the test run
  exercises the real migration chain and PostgreSQL-specific SQL.
- `Reset()` truncates every `public` table except `__EFMigrationsHistory` with
  `RESTART IDENTITY CASCADE`; database-backed test fixtures call it before seeding.
- xUnit parallelization is disabled for the assembly, so tests cannot interleave
  truncations.

Tests must seed required parent rows (users, cities, clients, suppliers, deliverymen,
shipping carriers) instead of disabling foreign keys. Constraint tests assert
`PostgresException.SqlState` values:

| Constraint | SqlState |
| --- | --- |
| foreign key | `23503` (or `23001` for `RESTRICT`) |
| unique | `23505` |
| check | `23514` |
| string too long | `22001` |

Seed rows inserted by migrations (for example payment methods and sealed roles) do not
survive the truncation; tests that need them must insert their own rows or apply the
seed SQL explicitly.

## CI

- `.github/workflows/dotnet-tests.yml` runs on pushes to `master` and on pull requests.
  It starts a `postgres:18` service on port `5433`, builds Release, runs all .NET tests
  against `orderhub_test`, then runs the Node local-time test.
- `.github/workflows/web-release.yml` keeps its tag-based, `master`-only release gate.
  A Linux `test` job with the same `postgres:18` service must pass before the Windows
  `build` job validates the tag, packages the release, and publishes it.

## Schema map

Migration-created tables (snake_case):

- **Orders:** `orders`, `order_items`, `order_item_properties`,
  `order_status_history`, `order_delivery_steps`, `delivery_step_status_history`,
  `order_delivery_movements`, `order_drafts`.
- **Work records:** `supplies`, `shipments`, `delivery_trips`,
  `work_record_status_histories`, `stock_issue_orders`.
- **Master data:** `cities`, `clients`, `suppliers`, `deliverymen`,
  `shipping_carriers`, `categories`, `products`, `product_supplier`,
  `product_properties`, `properties`, `property_options`, `payment_methods`,
  `phones`, `addresses`.
- **Inventory:** `warehouses`, `stock_balances`, `inventory_movements`,
  `order_inventory_allocations`, `stock_receipts`, `stock_receipt_lines`,
  `stock_receipt_attachments`, `stock_issue_batches`, `stock_issue_lines`,
  `stock_delivery_records`, `inventory_returns`, `inventory_return_lines`,
  `inventory_approval_requests`, `inventory_counts`, `inventory_count_lines`,
  `inventory_rebuild_logs`, `inventory_rebuild_log_lines`.
- **Documents:** `quotations`, `quotation_items`, `proforma_invoices`,
  `proforma_invoice_items`, `invoices`, `invoice_items`, `document_sequences`.
- **Standalone trips:** `standalone_trips`, `standalone_trip_attempts`,
  `standalone_trip_histories`.
- **Identity and numbering:** `users`, `roles`, `role_permissions`,
  `number_sequences`.
- **EF Core:** `__EFMigrationsHistory`.

## Immutability and correction rules

`AppDbContext` rejects updates and deletes for:

- `inventory_movements` (only the status may change, and only to `Reversed`);
- `order_status_history`, `delivery_step_status_history`,
  `work_record_status_histories`, `order_delivery_movements`,
  `standalone_trip_histories`;
- `stock_receipts` and `stock_receipt_lines`; receipt attachments may only be changed
  while they are drafts.

Corrections are new records (reversals, adjustments), never edits. Monthly work-record
numbers are allocated in `IUnitOfWork.SaveChangesAsync` using
`number_sequences`, which relies on PostgreSQL `INSERT ... ON CONFLICT ... RETURNING`.
