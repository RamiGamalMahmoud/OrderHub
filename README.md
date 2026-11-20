# OrderHub Web

OrderHub is an ASP.NET Core Blazor Server application for managing orders, products, customers, delivery workflows, commercial documents, users, permissions, and multi-warehouse inventory. It uses .NET 9, MudBlazor, MediatR, Entity Framework Core, PostgreSQL, and QuestPDF.

## Repository structure

- `OrderHub.Web`: Web host, Razor components, authentication, authorization, file controllers, static assets, and runtime configuration.
- `OrderHub.Application`: Commands, queries, authorization, and persistence/service contracts.
- `OrderHub.Domain`: Business entities, value objects, lifecycle rules, inventory invariants, and pricing calculations.
- `OrderHub.Infrastructure`: PostgreSQL persistence, EF Core migrations, document generation, and file storage.
- `OrderHub.Tests`: Application, domain, infrastructure, and Web tests. Database-backed tests run against a dedicated PostgreSQL database.
- `docs`: Architecture, domain rules, and database/testing reference.

`OrderHub.sln` includes these five projects. The retired WPF client and standalone API are no longer part of the repository.

## Development and verification

Install a stable .NET 9 SDK (selected by `global.json`), the .NET 9 ASP.NET Core runtime, Docker, and Node.js 24. Run commands from the repository root:

```powershell
dotnet tool restore
dotnet restore OrderHub.sln
dotnet build OrderHub.sln --configuration Release

# Database-backed tests use a dedicated PostgreSQL container on port 5433.
$env:TEST_DB_PASSWORD = '<local test password>'
docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml up -d --wait
$env:ORDERHUB_TEST_POSTGRES = 'Host=localhost;Port=5433;Database=orderhub_test;Username=orderhub_test;Password=<local test password>;Include Error Detail=true'
dotnet test OrderHub.sln --configuration Release

node OrderHub.Tests/Web/order-local-time.test.mjs

dotnet run --project OrderHub.Web
```

The test harness refuses any database whose name is not `orderhub_test`, applies all EF Core migrations on first use, and truncates the schema between tests. Stop the container with `docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml down`; never use `down -v` unless you intend to discard the volume. PostgreSQL only: SQLite and EF Core InMemory are not supported.

Configure `ConnectionStrings:DefaultConnection` and `ApplicationData:RootPath` using Web configuration, environment variables, or user secrets. Keep credentials and operational data outside source control.

See [docs/architecture.md](docs/architecture.md), [docs/domain-rules.md](docs/domain-rules.md), and [docs/database.md](docs/database.md) for details.

## Publishing and production

```powershell
dotnet publish OrderHub.Web/OrderHub.Web.csproj --configuration Release --output ./publish
```

The Web host supports Windows service operation as `OrderHubWeb`. Production configuration, credentials, application data directories, document storage, and service-account permissions are provisioned for the target environment.

All EF Core migrations remain in `OrderHub.Infrastructure/Migrations`. The app does not apply migrations at startup; apply them explicitly before starting a new version. See [docs/database.md](docs/database.md) for migration commands and safety rules.

Build outputs, test results, logs, temporary files, and publish output are ignored by Git. Keep source assets, migration files, deployment configuration, and operational documentation tracked.

## GitHub workflows

- `.github/workflows/dotnet-tests.yml` builds and tests on pushes to `master` and on pull requests, using a PostgreSQL 18 service and the Node local-time test.
- `.github/workflows/web-release.yml` creates a production release only when an unused `vX.Y.Z` tag points at the latest `master` commit. Releases are self-contained `win-x64` packages; the version comes from the tag.
