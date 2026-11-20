# AGENTS.md

## 1. Project Context

OrderHub is an ASP.NET Core 9 Blazor Server application for order, inventory, and internal business operations management.

Technology stack:
- .NET 9 / ASP.NET Core 9
- Blazor Server
- C#
- Entity Framework Core
- PostgreSQL
- MediatR
- MudBlazor 9.11.0
- Tailwind CSS
- QuestPDF
- xUnit
- Node.js for isolated frontend tests

The application provides an Arabic RTL user interface and is designed primarily for desktop and laptop use over a local network or company VPN, with responsive support for smaller screens.

The solution contains five projects in `OrderHub.sln`. There are no other application entry points.

## 2. Engineering Philosophy

The human developer owns the architecture, business decisions, technical direction, and final acceptance of changes.

AI agents are engineering assistants. They must implement approved requirements within established project boundaries, not independently redefine the system.

The primary engineering goals, in order of importance, are:

1. Correct business behavior and data integrity.
2. Architectural consistency and clear separation of responsibilities.
3. Maintainability and readability.
4. Simplicity and minimum necessary complexity.
5. Reliability and testability.
6. Performance where it materially affects the user experience.
7. Consistency and usability of the user interface.

### Core Principles

Apply established software engineering principles pragmatically:

- SOLID: Apply the principles where they improve maintainability and responsibility separation.
- DRY: Avoid meaningful duplication, but do not create premature shared abstractions.
- KISS: Prefer straightforward solutions over unnecessary sophistication.
- YAGNI: Do not implement speculative requirements or future capabilities.
- Separation of Concerns: Keep business rules, application orchestration, persistence, and presentation responsibilities separate.
- Encapsulation: Protect domain invariants and prevent invalid state transitions.
- Explicitness: Prefer understandable behavior and explicit dependencies over hidden conventions.
- Fail Fast: Detect invalid states and unsupported operations as early as practical.
- Least Privilege: Enforce permissions at the appropriate application boundaries.
- Defense in Depth: Do not rely on UI restrictions as a replacement for server-side validation and authorization.

These principles must be balanced rather than applied mechanically.

### Simplicity and Scope Control

Always prefer the smallest maintainable change that correctly satisfies the approved requirements.

Before introducing anything new:

1. Inspect the existing implementation.
2. Search for reusable domain behavior, services, components, and abstractions.
3. Understand why the existing design works the way it does.
4. Determine whether an existing implementation can be extended safely.
5. Introduce a new abstraction only when it provides a concrete architectural or maintenance benefit.

Avoid:

- Over-engineering.
- Premature optimization.
- Speculative extensibility.
- Unnecessary interfaces, factories, managers, providers, and wrapper classes.
- Excessive layering and indirection.
- Generic frameworks for isolated use cases.
- Duplicate implementations of existing functionality.
- Unnecessary design patterns.
- Unrelated refactoring.
- Redundant validation or defensive logic without a meaningful risk.
- Unnecessary configuration options.
- Excessive comments that merely restate the code.

Do not interpret minimum code as the goal. The goal is minimum unnecessary complexity.

A slightly longer implementation that is easier to understand and maintain is preferable to a compact but obscure implementation.

Do not change existing behavior merely because another implementation appears cleaner.

## 3. Mandatory Development Workflow

Follow a controlled, incremental development process.

### Phase 1: Understand

Before making changes:

- Read this file and relevant project documentation.
- Inspect the existing implementation.
- Identify affected layers and dependencies.
- Understand existing business rules and workflows.
- Inspect relevant entities, handlers, services, database mappings, and tests.
- Identify compatibility requirements and potential regressions.

Do not assume that a feature is missing because its implementation is not immediately visible.

### Phase 2: Analyze Requirements

Identify:

- The actual business problem.
- Expected user behavior.
- Business rules and invariants.
- Normal and exceptional scenarios.
- State transitions.
- Authorization requirements.
- Data ownership and lifecycle.
- Existing behavior that must remain unchanged.
- Explicitly excluded functionality.

Never invent business rules to fill important gaps.

If an unresolved ambiguity can materially affect business behavior, data integrity, architecture, or user permissions, stop and ask the human developer.

For minor implementation details already covered by existing conventions, use reasonable judgment.

### Phase 3: Design

Prepare a proportionate technical design before implementing non-trivial changes.

The design should explain:

- Which existing components are affected.
- Which new components are genuinely necessary.
- Where each responsibility belongs.
- How data flows through the system.
- How existing workflows are preserved.
- Database and migration implications.
- Authorization and validation requirements.
- Testing strategy.
- Potential risks.

Do not produce extensive design documentation for trivial changes.

For significant architectural changes, present alternatives and their trade-offs.

### Phase 4: Approval

Obtain explicit approval before:

- Changing established business rules.
- Changing architectural boundaries.
- Introducing new architectural patterns.
- Introducing new external dependencies.
- Modifying critical data lifecycle behavior.
- Making potentially breaking database changes.
- Changing authorization semantics.
- Expanding the approved feature scope.

Routine implementation decisions within an approved design do not require repeated approval.

### Phase 5: Implementation

Implement incrementally.

- Follow the approved design.
- Keep changes focused.
- Prefer cohesive tasks with clear acceptance criteria.
- Preserve existing conventions.
- Avoid mixing unrelated improvements with feature work.
- Report unexpected architectural or business conflicts instead of silently working around them.

### Phase 6: Verification

Run the relevant tests and build commands.

Inspect the resulting Git diff.

Verify that implementation matches the approved requirements and that no unrelated files or behaviors were changed.

### Phase 7: Completion Report

Every implementation task must end with a factual report containing:

- Implemented behavior.
- Files created, modified, or deleted.
- Important technical decisions.
- Database and migration changes.
- Tests actually executed and their results.
- Build results.
- Known limitations.
- Deviations from the approved design.
- Remaining risks.

Never report unexecuted tests as passing.

---

## 4. Architecture and Project Responsibilities

Follow the existing architecture. Do not introduce a different architectural style without approval.

### Project Responsibilities

**OrderHub.Web**
- Blazor host and UI components.
- File controllers.
- Cookie authentication.
- Permission authorization.
- Dependency injection composition in `Program.cs`.

**OrderHub.Application**
- MediatR commands and queries.
- Handlers.
- Application contracts.
- Validation.
- Authorization attributes such as `[RequirePermission]`.
- `Result` and `Result<T>`.

**OrderHub.Domain**
- Domain entities.
- Value objects.
- Business rules.
- Lifecycle and state transitions.
- Pricing and domain calculations.

The Domain project must not depend on Infrastructure or presentation technologies.

**OrderHub.Infrastructure**
- EF Core `AppDbContext`.
- Repositories.
- Database mappings and migrations.
- Read services.
- Monthly number sequence allocation.
- PDF generation.
- Local file storage.

**OrderHub.Tests**
- xUnit tests for Domain, Application, and Infrastructure.
- Node.js tests under `OrderHub.Tests/Web`.
- No reference to `OrderHub.Web`.

### Architectural Rules

- Keep business logic out of Blazor components.
- Keep persistence concerns out of Domain entities.
- Do not make Application handlers responsible for presentation formatting.
- Do not bypass established application request execution.
- Do not create duplicate business rule implementations in multiple layers.
- Do not introduce unnecessary mediator, repository, or service abstractions.
- Keep dependencies pointing toward the appropriate inner layers.
- Avoid circular dependencies and service locator patterns.
- Keep dependency injection registrations explicit and discoverable.

### Existing Request Execution Flow

Blazor components must not call repositories directly.

Use `IRequestExecutor`.

`AuthenticatedRequestExecutor` resolves the current identity, creates the appropriate DI scope, initializes the current `OperationUser`, then dispatches requests through MediatR.

Do not bypass this execution flow for production application operations.

### Authorization

Permissions are enforced through `AuthorizationBehavior` and attributes on MediatR request records.

New commands and queries must include the appropriate:

- `[RequirePermission]`
- `[RequireHigherManager]`

as applicable.

Calling handlers directly bypasses authorization. This is intentional in many existing unit tests, but production execution must use the established request pipeline.

UI visibility restrictions are not authorization boundaries.

Never assume that hiding a button or disabling an input protects an operation.

### Persistence and Unit of Work

Save through `IUnitOfWork.SaveChangesAsync`, not directly through `DbContext`.

The unit of work:

- Allocates monthly numbers for new `IWorkRecord` entities.
- Translates persistence errors.

`AppDbContext` also blocks updates and deletes of ledger, history, and receipt records.

Corrections must be represented by new reversing records, not by modifying historical records.

Preserve these data integrity guarantees.

### Database Naming

The database uses snake_case naming through `UseSnakeCaseNamingConvention`.

All raw SQL must use snake_case table and column names.

---

## 5. Domain Modeling and Business Rules

Domain behavior must be explicit, consistent, and protected.

### Domain Design

- Model business concepts rather than merely reflecting database tables.
- Keep entities responsible for protecting their invariants.
- Keep lifecycle transitions explicit.
- Prevent invalid state transitions.
- Prefer meaningful domain operations over arbitrary property mutation.
- Use value objects when they provide meaningful domain semantics.
- Avoid anemic domain models when important business rules naturally belong to the domain.
- Do not introduce DDD patterns merely for architectural appearance.

### Lifecycle and Historical Records

When a process has a lifecycle, identify:

- Valid states.
- Allowed transitions.
- Terminal states.
- Retry behavior.
- Cancellation behavior.
- Failure behavior.
- Historical record requirements.
- Ownership and responsibility at each stage.

Do not silently overwrite historical information.

When the domain requires an audit trail, preserve the history rather than retaining only the latest state.

Do not create parallel status systems when an existing domain concept already represents the required behavior.

### Business Rule Authority

Business rules must have a clear authoritative location.

Do not duplicate important rules between:

- Blazor components.
- MediatR handlers.
- Domain entities.
- Database triggers or constraints.

Use the appropriate layer for each rule and preserve necessary database-level integrity constraints.

For every important operation, consider:

- Repeated execution.
- Concurrent requests.
- Partial failures.
- Transaction boundaries.
- Data consistency.
- Invalid or stale state.

Do not introduce additional transaction or concurrency mechanisms without a demonstrated need.

Refer to `docs/domain-rules.md` for established project-specific business rules.

---

## 6. Database and Migrations

All migrations are located in `OrderHub.Infrastructure/Migrations`.

Some migrations are hand-written and have no `.Designer.cs`. The model snapshot must still match the current EF Core model. The hand-written migrations are:

- `20261006094846_RestrictSupplierProductLinks.cs`
- `20261030000000_AddOrderDiscountAndDeliveryCosts.cs`
- `20261030010000_AddCommercialDocumentFinancialSnapshots.cs`

The application does not automatically apply migrations at startup. `IDatabaseService.MigrateAsync` currently has no caller.

### Migration Rules

- Inspect the current model and migration history before changing the schema.
- Generate migrations using the established EF Core workflow.
- Review generated migration code rather than accepting it blindly.
- Verify the model snapshot.
- Consider existing production data and compatibility.
- Avoid destructive operations without explicit approval.
- Never assume a database is empty.
- Preserve historical and financial data.
- Use explicit backfill strategies when existing records require new values.
- Ensure migrations can be exercised against PostgreSQL.
- Do not apply production migrations as part of ordinary development or testing.

### EF Core Commands

Use Infrastructure as both the project and startup project.

```powershell
dotnet tool restore

dotnet ef migrations add <Name> `
  --project OrderHub.Infrastructure `
  --startup-project OrderHub.Infrastructure `
  --output-dir Migrations

dotnet ef database update `
  --project OrderHub.Infrastructure `
  --startup-project OrderHub.Infrastructure
```

`.config/dotnet-tools.json` pins `dotnet-ef` version `9.0.9`.

The design-time factory is named `DesignTimeAppDbContextFactrory.cs` and uses a hardcoded localhost connection with a local development password. Treat it as a development-only artifact; never run `database update` against production and never reuse or extend that credential.

---

## 7. Coding Standards

### C# and .NET

- Follow established .NET and C# conventions.
- Prefer clear, descriptive names.
- Keep methods focused and cohesive.
- Avoid excessively long methods with multiple unrelated responsibilities.
- Use asynchronous APIs for I/O-bound operations.
- Pass and respect `CancellationToken` where appropriate.
- Avoid unnecessary allocations and repeated database queries.
- Handle exceptions at appropriate boundaries.
- Do not swallow exceptions.
- Do not use exceptions as ordinary business control flow.
- Avoid hidden side effects.
- Avoid unnecessary mutable state.
- Use nullable reference types correctly.
- Avoid suppressing compiler warnings without a justified reason.
- Do not introduce unnecessary static utility classes.
- Prefer explicit dependency injection.
- Do not use reflection or dynamic dispatch without a concrete need.

### Naming and Consistency

Existing project naming conventions take precedence over personal preferences.

Do not rename existing types, namespaces, or members simply to correct historical naming inconsistencies.

Search before assuming an unusual name is incorrect.

### Validation

Validate inputs at the appropriate application boundary.

Use domain invariants to protect business correctness.

Avoid duplicating identical validation rules in multiple places unless each layer has a distinct responsibility.

Validation messages presented to users must be clear and in Arabic.

### Error Handling

- Distinguish validation failures from unexpected technical failures.
- Return established `Result` types where appropriate.
- Preserve meaningful error information.
- Do not expose internal exceptions or sensitive infrastructure details to users.
- Avoid generic success messages when an operation partially failed.
- Never report success before persistence has actually succeeded.

### Comments and Documentation

Write comments to explain important reasoning, constraints, or non-obvious decisions.

Do not comment on obvious code.

Update documentation when a change materially affects architecture, business rules, workflows, or operational procedures.

Do not generate documentation simply to increase the number of project documents.

---

## 8. UI/UX Design Principles

UI quality is a core engineering requirement, not a cosmetic finishing step.

All new and modified screens must follow established professional UI/UX principles.

Do not invent new design patterns when established and proven interaction patterns already solve the problem.

### Primary Design Philosophy

The interface should be:

- Clear.
- Compact.
- Consistent.
- Predictable.
- Efficient.
- Easy to learn.
- Easy to navigate.
- Responsive.
- Accessible.
- Appropriate for business operations and data-intensive workflows.

The application is primarily a desktop business application. Optimize its normal experience for desktop and laptop productivity while maintaining responsive behavior on smaller screens.

### User-Centered Design

Every screen must help the user understand:

1. Where they are.
2. What information they are viewing.
3. What the current situation is.
4. What actions are available.
5. What action should logically come next.

The user should not have to guess how to complete an ordinary workflow.

Prefer interfaces that guide users naturally through the task rather than interfaces that merely expose a collection of controls.

### Compact UI and Information Density

Compact design is a major project requirement, particularly for screens containing many records, fields, tables, filters, or operational details.

Apply compact density intelligently.

- Minimize unnecessary whitespace.
- Avoid oversized headers and excessive section spacing.
- Use appropriately compact MudBlazor components.
- Keep table rows reasonably dense.
- Reduce redundant labels and repeated information when meaning remains clear.
- Keep related fields grouped together.
- Use available horizontal space efficiently.
- Avoid unnecessary cards, nested containers, and decorative panels.
- Avoid excessive vertical scrolling when information can be organized more effectively.
- Prefer meaningful information grouping over arbitrary visual separation.

However:

- Never sacrifice readability for density.
- Do not make text excessively small.
- Preserve adequate spacing between interactive controls.
- Maintain clear visual separation between unrelated actions.
- Do not compress forms until they become difficult to understand.
- Do not hide important information merely to make the screen smaller.

The goal is high information density with low cognitive load.

### Visual Hierarchy

Use consistent visual hierarchy.

Users should immediately distinguish:

- Page title and context.
- Primary actions.
- Secondary actions.
- Important status information.
- Main content.
- Supporting details.
- Warnings and errors.

Use typography, spacing, alignment, contrast, and component hierarchy consistently.

Avoid giving every element equal visual importance.

Primary actions must be visually distinguishable without making every button prominent.

Use color semantically and consistently, not decoratively.

### Navigation and Screen Organization

- Keep navigation predictable.
- Preserve the application's established navigation patterns.
- Use meaningful page titles and section headings.
- Avoid unnecessary navigation levels.
- Do not overload menus with unrelated actions.
- Keep important actions easy to locate.
- Preserve context when moving between related records and details.
- Provide clear ways to return to the parent context.

Do not introduce new navigation patterns without a clear usability benefit.

### Task-Oriented Screens

A screen should support the user's actual task from beginning to end.

Where appropriate:

- Show relevant context before asking the user to act.
- Group information according to the user's workflow.
- Present the most important information first.
- Place primary actions near the content they affect.
- Keep related actions together.
- Make the next logical step discoverable.
- Show progress or current status when the process has multiple stages.
- Provide clear feedback after actions.

Avoid forcing users to navigate between multiple pages for information that belongs to one coherent workflow.

However, do not place unrelated business processes on the same screen merely to reduce navigation.

### Forms and Data Entry

Forms must be efficient and easy to understand.

- Group related fields logically.
- Use meaningful labels.
- Use appropriate input types.
- Provide sensible defaults only when they are genuinely safe.
- Make required fields clear.
- Show validation messages near the relevant fields.
- Preserve user-entered values after validation failures.
- Avoid unnecessary fields and redundant inputs.
- Use logical field ordering.
- Keep primary submission actions easy to locate.
- Distinguish between saving, submitting, confirming, and destructive actions.
- Prevent accidental duplicate submissions when appropriate.

Use progressive disclosure when advanced or optional information would otherwise overwhelm the normal workflow.

Do not hide essential information behind unnecessary interactions.

### Tables and Data-Heavy Screens

Tables are central to business management workflows.

Apply established data-table usability principles:

- Use clear and descriptive column headers.
- Choose appropriate column widths.
- Align values consistently.
- Use consistent formatting for dates, amounts, quantities, and identifiers.
- Keep important identifying information visible.
- Make row actions discoverable and predictable.
- Avoid excessive columns with little practical value.
- Support sorting and filtering where useful.
- Keep filters logically organized.
- Preserve useful context when navigating or modifying records.
- Use pagination or other appropriate data-loading strategies for large datasets.
- Provide meaningful empty states.
- Distinguish totals and summaries from ordinary records.
- Highlight exceptional statuses without excessive decoration.

Do not use horizontal scrolling as the default solution to poor table organization.

When there are too many columns, evaluate:

1. Whether every column is necessary.
2. Whether related information can be grouped.
3. Whether secondary details belong in a row expansion or details view.
4. Whether the table should use a different responsive presentation.

Do not remove information required for operational decisions.

### Status and Workflow Presentation

For business processes:

- Make the current status immediately understandable.
- Use consistent status labels and semantic colors.
- Distinguish current state from historical state.
- Show relevant timestamps and responsible parties when useful.
- Make available next actions clear.
- Disable or hide invalid transitions according to established authorization and workflow rules.
- Explain why an action is unavailable when that information helps the user.

Never rely on color alone to communicate a status.

### Dialogs and Confirmations

Use dialogs only when they provide a meaningful interaction benefit.

- Avoid nested dialogs.
- Keep confirmation messages concise.
- Clearly describe the action being confirmed.
- Explain consequences for destructive or irreversible operations.
- Require reasons where business rules demand them.
- Distinguish cancellation from deletion.
- Keep forms within dialogs appropriately compact.
- Avoid using dialogs for workflows that require extensive information or navigation.

### Feedback and System States

Every interactive screen must handle relevant UI states:

- Initial loading.
- Empty results.
- No search results.
- Successful operations.
- Validation failures.
- Permission restrictions.
- Unexpected errors.
- Long-running operations.

Feedback must be immediate, clear, and actionable where possible.

Do not leave users wondering whether an action succeeded.

Use loading indicators appropriately and avoid unnecessarily blocking the entire screen.

### Responsive Design

Desktop and laptop layouts are the primary optimization target.

Smaller screens must remain usable.

- Avoid fixed widths that cause layout overflow.
- Use responsive grids and layouts.
- Adapt dense tables appropriately.
- Keep important actions accessible.
- Prevent controls from overlapping.
- Preserve readable typography.
- Do not simply shrink desktop layouts to fit mobile screens.

Responsive behavior should preserve task usability, not merely visual appearance.

### Accessibility and Usability

Follow established accessibility principles:

- Sufficient text and control contrast.
- Visible keyboard focus.
- Logical keyboard navigation.
- Meaningful labels.
- Appropriate semantic elements.
- Clear validation and error messages.
- Adequate interactive target sizes.
- No color-only communication.
- Consistent interaction behavior.

### Arabic RTL Requirements

The interface language is Arabic.

- Preserve correct RTL layout and text direction.
- Use logical alignment rather than unnecessary hardcoded left/right positioning.
- Ensure mixed Arabic and Latin content is displayed correctly.
- Handle identifiers, phone numbers, emails, and technical values carefully.
- Format dates, amounts, and numbers consistently.
- Use Arabic user-facing messages.
- Preserve appropriate Latin digits where established by existing application behavior.

UI culture is pinned to `ar`.

Existing code uses `CultureInfo.InvariantCulture` for Latin digits in displayed numbers.

Do not change the application's established number-formatting behavior without approval.

---

## 9. UI Technology and Component Standards

### MudBlazor First

MudBlazor is the primary UI component library.

Prefer existing MudBlazor components and established project components before writing custom implementations.

Use existing project theme, component patterns, and styling conventions.

Do not recreate functionality that MudBlazor already provides adequately.

### Styling

- Prefer MudBlazor parameters and theme configuration.
- Use existing Tailwind utilities where appropriate.
- Use minimal custom CSS.
- Avoid large custom styling systems.
- Avoid unnecessary CSS overrides of MudBlazor internals.
- Do not introduce another UI framework.
- Do not introduce additional component libraries without approval.
- Avoid inline styling when an existing reusable styling approach is available.

Do not introduce custom components merely to wrap a single existing component without providing meaningful reuse or behavior.

### Consistency

Before implementing a new screen or component, inspect comparable existing screens.

Reuse established patterns for:

- Page layouts.
- Data tables.
- Forms.
- Search and filtering.
- Dialogs.
- Status indicators.
- Notifications.
- Loading states.
- Permission-aware actions.

Consistency is more important than introducing a new visual idea for every feature.

### UI Review Criteria

Before considering a UI task complete, verify:

- Is the primary task obvious?
- Is the most important information visible first?
- Is the screen unnecessarily tall or spacious?
- Is the information density appropriate?
- Are related controls grouped logically?
- Are primary and secondary actions distinguishable?
- Can users understand current statuses?
- Are loading, empty, error, and success states handled?
- Does the screen work at its intended responsive sizes?
- Is Arabic RTL correct?
- Does the screen follow existing MudBlazor conventions?
- Has unnecessary custom CSS been avoided?

Do not claim a screen is complete merely because it compiles.

---

## 10. Testing and Quality Assurance

Testing must provide evidence of correctness.

### Testing Strategy

Use the appropriate testing level:

**Domain Unit Tests**
- Business rules.
- State transitions.
- Invariants.
- Calculations.
- Invalid operations.

**Application Tests**
- Handler behavior.
- Validation.
- Authorization-related behavior where applicable.
- Business workflow orchestration.
- Failure scenarios.

**Infrastructure Integration Tests**
- EF Core behavior.
- PostgreSQL queries.
- Persistence.
- Transactions.
- Migrations.
- Constraints.
- Database-specific behavior.

**UI-Related Tests**
- Existing automated frontend tests.
- Relevant behavior and regression verification.

Do not introduce unnecessary testing frameworks or complicated test abstractions.

### PostgreSQL Is the Authoritative Test Database

Database-backed tests must use the dedicated PostgreSQL test database.

Never replace PostgreSQL integration testing with:

- SQLite.
- EF Core InMemory.
- `EnsureCreated()`.

Tests apply actual migrations using `Database.Migrate()`.

This ensures PostgreSQL-specific SQL, mappings, migrations, and database behavior are exercised.

### Test Database Isolation

All database-backed tests share one `orderhub_test` schema:

- xUnit parallelization is disabled for the whole assembly in `OrderHub.Tests/AssemblyInfo.cs`.
- `PostgresTestDatabase.Reset()` truncates every `public` table except `__EFMigrationsHistory` with `RESTART IDENTITY CASCADE`; database-backed fixtures call it before seeding.
- Reset runs immediately after the initial `Database.Migrate()`, so rows inserted by migrations (payment methods, sealed roles) do not exist from the first test onward. Tests must insert the rows they depend on.
- Tests must seed required parent rows (users, cities, clients, suppliers, deliverymen, shipping carriers) instead of disabling foreign keys.
- Constraint tests assert PostgreSQL `SqlState` values, for example `23001` (`RESTRICT`), `23505` (unique), and `23514` (check).

### Test Quality

- Test meaningful business scenarios, not implementation details unnecessarily.
- Cover important edge cases.
- Test failure and invalid-state scenarios.
- Consider concurrency and repeated execution where relevant.
- Keep tests independent and deterministic.
- Avoid unnecessary mocks when a real integration test provides better evidence.
- Do not modify tests merely to make an incorrect implementation pass.
- Do not weaken existing assertions without justification.

### Verification Reporting

Clearly distinguish:

- Passed.
- Failed.
- Not Run.
- Blocked.

A successful build does not prove business correctness.

A passing unit test does not prove database integration correctness.

---

## 11. Git and Change Management

Treat the Git diff as an essential review artifact.

Before finishing:

- Inspect all changed files.
- Identify unexpected changes.
- Remove temporary files.
- Avoid generated artifacts unrelated to the task.
- Preserve existing user modifications.
- Never discard unrelated changes.
- Do not reset or overwrite user work.
- Do not commit unless explicitly requested.

Keep changes logically focused.

Do not mix feature development, broad refactoring, and unrelated cleanup in one change set.

### Reviewability

Implementation should make it easy for the human developer to understand:

- What changed.
- Why it changed.
- How it works.
- What existing behavior it affects.
- How it was verified.

Prefer incremental changes that can be reviewed independently.

---

## 12. Operational and Security Rules

- Never commit new credentials or secrets.
- The checked-in `http` launch profile (`OrderHub.Web/Properties/launchSettings.json`) and the design-time factory already contain a local development password. Do not reuse or extend those credentials for shared or production environments.
- Never expose sensitive configuration values in logs or reports.
- Do not change production deployment behavior without approval.
- Do not introduce external services without approval.
- Validate uploaded files and protect file operations.
- Respect existing authentication and authorization boundaries.
- Do not expose internal exceptions to users.
- Do not bypass application security through direct handler or repository calls.
- Do not use production data for ordinary testing.

Uploaded files and generated documents live under `ApplicationData:RootPath`, not in the repository.

HTTPS redirection is intentionally disabled for offline/LAN deployment. Do not enable or otherwise "fix" it without approval.

---

## 13. Project-Specific Commands

Run from the repository root using PowerShell.

### Restore and Build

```powershell
dotnet tool restore
dotnet restore OrderHub.sln
dotnet build OrderHub.sln --configuration Release
```

There is no separate lint, format, or typecheck step currently configured. The Release build is the compilation/type-check verification.

The build currently contains pre-existing nullable, EF Core, and MudBlazor warnings.

The build must finish with `0 Error(s)`.

Do not attempt unrelated warning cleanup as part of an ordinary feature task.

Report newly introduced warnings separately.

### Database-Backed Tests

```powershell
$env:TEST_DB_PASSWORD = '<local test password>'

docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml up -d --wait

$env:ORDERHUB_TEST_POSTGRES = 'Host=localhost;Port=5433;Database=orderhub_test;Username=orderhub_test;Password=<local test password>;Include Error Detail=true'

dotnet test OrderHub.sln --configuration Release
```

The test harness requires `ORDERHUB_TEST_POSTGRES` targeting the `orderhub_test` database.

The harness refuses any other database name.

Tests apply all migrations on first use.

Tests never start, stop, or clear the PostgreSQL container.

The test process must actually see `ORDERHUB_TEST_POSTGRES`. On Windows, Visual Studio does not inherit a `setx` value in an already-open session; restart Visual Studio or use a git-ignored `local.runsettings`. Never commit the test connection string or password.

Stop the test container when appropriate:

```powershell
docker compose -f OrderHub.Tests/Infrastructure/docker-compose.test.yml down
```

Never use `down -v`.

### Single Test Class

```powershell
dotnet test OrderHub.Tests/OrderHub.Tests.csproj `
  --configuration Release `
  --filter "FullyQualifiedName~TripLifecycleHandlerTests"
```

### Node.js Test

CI uses Node.js 24.

```powershell
node OrderHub.Tests/Web/order-local-time.test.mjs
```

Node.js is used only for this isolated frontend test and is not part of `dotnet test`.

### Run the Application

```powershell
dotnet run --project OrderHub.Web
```

The first launch profile (`http`) already contains a local PostgreSQL connection string and:

`ApplicationData:RootPath=C:\ProgramData\OrderHub`

Use the `http` profile locally. The `https` profile does not define the connection string or application data path.

PostgreSQL must be running.

Override configuration through environment variables or user secrets. Never commit credentials.

### Tailwind Generated File

Every build rewrites the tracked file:

`OrderHub.Web/wwwroot/css/tailwind.css`

If the task did not intentionally change styles, revert only the generated changes:

```powershell
git checkout -- OrderHub.Web/wwwroot/css/tailwind.css
```

If Tailwind classes or styles were intentionally changed, inspect and retain the generated output.

Never use this command when it would discard pre-existing user modifications. Inspect the file's Git diff first.

---

## 14. Repository-Specific Quirks

Some existing names contain deliberate-looking typos. Search before assuming a path or type is missing.

Examples:

- `OrderHub.Infrastructure/Reopsitories`
- Namespace `...Reopsitories`
- `HashShervice`
- `IHashShervice`
- `DesignTimeAppDbContextFactrory.cs`

Do not rename these as unrelated cleanup.

Planning documentation is partly stale.

- `design_draft.md` is the inventory design record; implementation is complete.
- `README.md` is maintained with the five-project layout and test database information.

When documentation conflicts with actual implementation, verify against:

1. Source code.
2. Project files.
3. Database model and migrations.
4. CI workflows.
5. Relevant maintained documentation.

Do not assume an old planning document represents current behavior.

### Detailed Documentation

Read the relevant documents when working on affected areas:

- `docs/architecture.md`: Projects, request flow, authorization, testing.
- `docs/domain-rules.md`: Order lifecycle, work records, inventory.
- `docs/database.md`: Migrations, test database, schema map.

Do not read every project document for every minor change. Read the documentation relevant to the task.

---

## 15. CI and Release

CI workflows:

- `.github/workflows/dotnet-tests.yml`
- `.github/workflows/web-release.yml`

The test workflow runs:

- Release build.
- PostgreSQL-backed tests.
- Node.js test.

It runs on pushes to `master` and pull requests.

The release workflow is tag-triggered.

The `vX.Y.Z` tag must point to the latest `master` commit.

Version comes from the tag.

Do not bump version files.

Do not change release workflow behavior without explicit approval.

---

## 16. Definition of Done

A task is complete only when all applicable conditions are satisfied:

### Requirements
- The approved requirements are implemented.
- Business rules are preserved.
- Important edge cases are addressed.
- No unresolved critical assumptions remain.

### Architecture
- Responsibilities remain in the correct layers.
- Existing architectural conventions are respected.
- No unnecessary abstractions or dependencies were introduced.
- Existing workflows remain compatible.

### Code
- Code is readable and maintainable.
- Changes are focused.
- No unrelated refactoring was performed.
- Existing user changes were preserved.

### Database
- Required migrations are present and reviewed.
- PostgreSQL compatibility is verified where applicable.
- Existing data integrity is preserved.
- No production migration was executed without authorization.

### UI
- The interface follows established UI/UX principles.
- Information hierarchy is clear.
- Compact density is balanced with readability.
- User workflows are understandable.
- RTL and responsive behavior are correct.
- Loading, empty, success, and error states are handled where applicable.

### Verification
- Relevant builds and tests were executed.
- Results are reported accurately.
- The final Git diff was inspected.
- Unexpected changes are explained or removed.

### Documentation
- Relevant maintained documentation is updated when necessary.
- No redundant or speculative documentation was added.

The human developer makes the final acceptance decision.
