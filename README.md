# OrderHub Web

OrderHub is an ASP.NET Core Blazor application for managing orders, products, customers, delivery workflows, commercial documents, users, and permissions. It uses .NET 9, MudBlazor, MediatR, Entity Framework Core, PostgreSQL, and QuestPDF.

## Repository structure

- `OrderHub.Web`: Web host, Razor components, authentication, authorization, file controllers, static assets, and runtime configuration.
- `OrderHub.Application`: Commands, queries, authorization, and persistence/service contracts.
- `OrderHub.Domain`: Business entities, value objects, lifecycle rules, and pricing calculations.
- `OrderHub.Infrastructure`: PostgreSQL persistence, EF Core migrations, document generation, and file storage.
- `OrderHub.Tests`: Application, domain, infrastructure, and Web tests. SQLite is used only for isolated tests.
- `docs`: Order lifecycle release runbook and its version-specific migration SQL.

`OrderHub.sln` includes these five projects. The retired WPF client and standalone API are no longer part of the repository. Historical development notes are retained in [CHANGELOG.md](CHANGELOG.md).

## Development and verification

Install a .NET SDK capable of targeting .NET 9, the .NET 9 ASP.NET Core runtime, and PostgreSQL. Run commands from the repository root:

```powershell
dotnet restore OrderHub.sln
dotnet build OrderHub.sln --configuration Release
dotnet test OrderHub.sln --configuration Release
node OrderHub.Tests/Web/order-local-time.test.mjs
dotnet run --project OrderHub.Web
```

Node.js is needed only for the browser-local time test. Configure `ConnectionStrings:DefaultConnection` and `ApplicationData:RootPath` using Web configuration, environment variables, or user secrets. Keep credentials and operational data outside source control.

PostgreSQL integration tests require explicit configuration of a disposable test server and otherwise report skips:

- `ORDERHUB_CATEGORY_TEST_POSTGRES`
- `ORDERHUB_PROPERTY_TEST_POSTGRES`
- `ORDERHUB_ORDER_SYNC_TEST_POSTGRES`
- `ORDERHUB_LIFECYCLE_TEST_POSTGRES`

The release rehearsal is separately opt-in via `ORDERHUB_REHEARSAL_SOURCE`; see the [release runbook](docs/order-lifecycle-release-runbook.md) for backup tools, isolation, and additional options.

## Publishing and production

```powershell
dotnet publish OrderHub.Web/OrderHub.Web.csproj --configuration Release --output ./publish
```

The Web host supports Windows service operation as `OrderHubWeb`. In Production it requires the external configuration file `%ProgramData%/OrderHub/Config/appsettings.Server.json`. Provision database access, application data directories, document storage, and service-account permissions for the target environment.

All EF Core migrations remain in `OrderHub.Infrastructure/Migrations`. Follow the [release runbook](docs/order-lifecycle-release-runbook.md) for backups, migration prerequisites, verification, and rollback. Its SQL script has a specific starting migration; verify the target schema before using it. The current Web `--migrate` branch is a placeholder and does not apply migrations.

Build outputs, test results, logs, temporary files, and publish output are ignored by Git. Keep source assets, migration files, deployment configuration, and operational documentation tracked.
