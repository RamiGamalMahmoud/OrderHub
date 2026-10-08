# Authorization implementation result

Date: 2026-10-06. Scope: current OrderHub.Application, Infrastructure and Web, with Domain preserved.

## 1. Summary

Implemented the agreed single-role authorization path. All 93 concrete business MediatR requests now declare typed feature/level metadata. The Application pipeline denies unauthorized requests before handler execution. Normal users obtain their current permission from the database on every check; valid authenticated admins bypass permission lookup. Web route policies, endpoint policies and reusable UI checks delegate to this same Application decision service.

Validation: 35 focused authorization cases passed; the full test project passed all 242 cases. Domain, Application, Infrastructure, Web and the production solution build with zero errors. Two pre-existing MudBlazor warnings remain on a full rebuild. No commit, staging, push or branch change was performed.

## 2. Architecture

```text
AuthenticationStateProvider / request ClaimsPrincipal
  -> AuthenticatedUser -> CurrentUserIdentity
  -> AuthenticatedRequestExecutor -> fresh operation scope
  -> OperationUser / ICurrentUser -> MediatR request
  -> AuthorizationBehavior reads RequirePermissionAttribute
  -> Application IAuthorizationService
       invalid/anonymous identity -> deny
       valid authenticated IsAdmin -> allow without permission query
       normal user -> IUserPermissionReader -> current User.Role.Permissions
                   -> explicit Read/Write comparison
  -> authorized handler
```

Web uses `PermissionAccess -> IRequestExecutor -> CheckPermissionQuery` for policies and UI. No Web EF permission query exists. ASP.NET authorization requirements are adapters, not a second permission decision engine. Internal ASP.NET policy names encode typed metadata solely for framework lookup; string permissions are neither accepted by Application nor persisted.

Application authorization, permission reader, Web permission adapter and ASP.NET requirement handler are scoped. The existing executor captures identity and initializes a fresh operation scope as before. Policy definitions and the stateless HTTP result adapter contain no per-user state. No permissions or RoleId were added to ICurrentUser, CurrentUserIdentity, UserSession or cookies.

## 3. Files Added

| File | Responsibility |
| --- | --- |
| `OrderHub.Application/Authorization/AuthorizationBehavior.cs` | MediatR guard before protected handlers. |
| `OrderHub.Application/Authorization/AuthorizationDeniedException.cs` | Single generic Application access-denied representation. |
| `OrderHub.Application/Authorization/AuthorizationService.cs` | Identity validation, admin bypass and explicit level comparison. |
| `OrderHub.Application/Authorization/CheckPermissionQuery.cs` | Identity-based Application permission check and handler for Web adapters. |
| `OrderHub.Application/Authorization/IAuthorizationService.cs` | Application authorization decision contract. |
| `OrderHub.Application/Authorization/IUserPermissionReader.cs` | Application effective permission read contract. |
| `OrderHub.Application/Authorization/RequirePermissionAttribute.cs` | Typed request requirement metadata, independent of ASP.NET. |
| `OrderHub.Infrastructure/Features/Administration/UserPermissionReader.cs` | Narrow, uncached EF projection from user to current role permissions. |
| `OrderHub.Tests/Application/AuthorizationTests.cs` | Service matrix, invalid identity/metadata and pipeline side-effect tests. |
| `OrderHub.Tests/Infrastructure/AuthorizationIntegrationTests.cs` | Production DI/pipeline mutation denial, dynamic changes and complete request coverage. |
| `OrderHub.Tests/Infrastructure/UserPermissionReaderTests.cs` | Existing SQLite fixture tests for role mapping and current database changes. |
| `OrderHub.Tests/Web/PermissionAuthorizationTests.cs` | Concurrent executors, policy delegation, endpoints, routes and HTTP denial tests. |
| `OrderHub.Web/Components/Common/PermissionErrorBoundary.cs` | Interactive Application denials lead to a generic access-denied page. |
| `OrderHub.Web/Components/Common/PermissionView.razor` | Reusable Read/Write-aware UI rendering. |
| `OrderHub.Web/Components/Pages/AccessDenied.razor` | Generic access-denied destination. |
| `OrderHub.Web/Infrastructure/Authorization/AuthorizationDeniedMiddleware.cs` | Maps propagated Application denials to HTTP 403. |
| `OrderHub.Web/Infrastructure/Authorization/PermissionAuthorizationResultHandler.cs` | Returns generic HTTP 403 for failed permission policies before endpoint execution. |
| `OrderHub.Web/Infrastructure/Authorization/PermissionAuthorizeAttribute.cs` | Typed feature/level ASP.NET policy metadata. |
| `OrderHub.Web/Infrastructure/Authorization/PermissionRequirement.cs` | ASP.NET requirement and handler delegating to the Application check. |
| `OrderHub.Web/Infrastructure/Authorization/ServiceCollectionExtensions.cs` | Registers fixed policy metadata, scoped Web authorization adapters and the stateless HTTP result adapter. |
| `OrderHub.Web/Services/PermissionAccess.cs` | Web adapter that dispatches Application checks through IRequestExecutor. |
| `Authorization-Result.md` | Complete implementation, validation and security review report. |

## 4. Files Modified

Every modified tracked file is listed below. Request changes add metadata/usings; business handlers and Domain mutations are preserved.

| File | Why |
| --- | --- |
| `OrderHub.Application/Features/Administration/Roles/Create/CreateRoleCommand.cs` | Declares Users/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Roles/Delete/DeleteRoleCommand.cs` | Declares Users/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Roles/GetRole/GetRoleQuery.cs` | Declares Users/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Roles/GetRoles/GetRolesQuery.cs` | Declares Users/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Roles/Update/UpdateRoleCommand.cs` | Declares Users/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Users/ActivateUser/ActivateUserCommand.cs` | Declares Users/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Users/Create/CreateUserCommand.cs` | Declares Users/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Users/DeactivateUser/DeactivateUserCommand.cs` | Declares Users/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Users/GetUser/GetUserQuery.cs` | Declares Users/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Users/GetUsers/GetUsersQuery.cs` | Declares Users/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Administration/Users/Update/UpdateUserCommand.cs` | Declares Users/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Documents/Invoices/CreateInvoiceCommand.cs` | Declares Documents/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Documents/ProformaInvoices/CreateProformaInvoiceCommand.cs` | Declares Documents/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Documents/Quotations/CreateQuotationCommand.cs` | Declares Documents/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/OrderDrafts/Delete/DeleteDraftCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/OrderDrafts/Get/GetDraftQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/OrderDrafts/List/GetDraftListQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/OrderDrafts/Save/SaveDraftCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/ChangePaymentMethod/ChangePaymentMethodCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/ChangeStatus/CancelOrderCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/ChangeStatus/DeliverOrderCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/ChangeStatus/ReturnOrderToPendingCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/ChangeStatus/ShipOrderCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/ChangeStatus/StartOrderProcessingCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/Create/CreateOrderCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/Delete/DeleteOrder.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/GetDetails/GetOrderDetailsQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/GetEditorLookups/GetOrderEditorLookupsQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/GetForEdit/GetOrderForEditQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/GetItemEditor/GetOrderItemEditorQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/GetItemsEditor/GetOrderItemsEditorQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/GetPage/GetOrdersPageQuery.cs` | Declares Orders/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Orders/Update/UpdateOrderCommand.cs` | Declares Orders/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Products/Create/CreateProductCommand.cs` | Declares Products/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Products/Delete/DeleteProductCommand.cs` | Declares Products/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Products/Get/GetProductQuery.cs` | Declares Products/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Products/GetProductsInfo/GetProductsInfoQuery.cs` | Declares Products/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Products/List/ListProductsQuery.cs` | Declares Products/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Products/Search/SearchProductsQuery.cs` | Declares Products/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Products/Update/UpdateProductCommand.cs` | Declares Products/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/CreateCategory/CreateCategoryCommand.cs` | Declares Categories/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/DeleteCategory/DeleteCategoryCommand.cs` | Declares Categories/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/GetCategoriesInfo/GetCategoriesInfoQuery.cs` | Declares Categories/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/GetCategoryForEdit/GetCategoryForEditQuery.cs` | Declares Categories/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/GetCategoryList/GetCategoryListQuery.cs` | Declares Categories/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/GetCategoryParentIds/GetCategoryParentIdsQuery.cs` | Declares Categories/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/GetCategoryTree/GetCategoryTreeQuery.cs` | Declares Categories/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/GetRootCategories/GetRootCategoriesQuery.cs` | Declares Categories/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/GetSubCategories/GetSubCategoriesQuery.cs` | Declares Categories/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Categories/UpdateCategory/UpdateCategoryCommand.cs` | Declares Categories/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Cities/CreateCity/CreateCityCommand.cs` | Declares Cities/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Cities/DeleteCity/DeleteCityCommand.cs` | Declares Cities/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Cities/GetAllCities/GetAllCitiesQuery.cs` | Declares Cities/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Cities/GetCitiesInfo/GetCitiesInfoQuery.cs` | Declares Cities/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Cities/GetCityForEdit/GetCityForEditQuery.cs` | Declares Cities/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Cities/UpdateCity/UpdateCityCommand.cs` | Declares Cities/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/CreateClient/CreateClientCommand.cs` | Declares Clients/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/DeleteClient/DeleteClientCommand.cs` | Declares Clients/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/GetAllClients/GetAllClientsQuery.cs` | Declares Clients/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/GetAllClientsPaged/GetAllClientsPagedQuery.cs` | Declares Clients/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/GetClientById/GetClientByIdQuery.cs` | Declares Clients/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/GetClientEdit/GetClientEditQuery.cs` | Declares Clients/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/GetClientsByName/GetClientsByNameQuery.cs` | Declares Clients/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/GetClientsInfo/GetClientsInfoQuery.cs` | Declares Clients/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Clients/UpdateClient/UpdateClientCommand.cs` | Declares Clients/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/CreateDeliveryman/CreateDeliverymanCommand.cs` | Declares Deliverymen/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/DeleteDeliveryman/DeleteDeliverymanCommand.cs` | Declares Deliverymen/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenInfo/GetAllDeliverymenInfoQuery.cs` | Declares Deliverymen/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/GetAllDeliverymenList/GetAllDeliverymenListQuery.cs` | Declares Deliverymen/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanById/GetDeliverymanByIdQuery.cs` | Declares Deliverymen/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymanForEdit/GetDeliverymanForEditQuery.cs` | Declares Deliverymen/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/GetDeliverymenByName/GetDeliverymenByNameQuery.cs` | Declares Deliverymen/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Deliverymen/UpdateDeliveryman/UpdateDeliverymanCommand.cs` | Declares Deliverymen/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/PaymentMethods/GetPaymentMethodList/GetPaymentMethodListQuery.cs` | Declares PaymentMethods/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Properties/Create/CreatePropertyCommand.cs` | Declares Properties/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Properties/Delete/DeletePropertyCommand.cs` | Declares Properties/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Properties/Get/GetPropertyQuery.cs` | Declares Properties/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Properties/GetAll/GetPropertiesQuery.cs` | Declares Properties/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Properties/Update/UpdatePropertyCommand.cs` | Declares Properties/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/CreateShippingCarrier/CreateShippingCarrierCommand.cs` | Declares ShippingCarriers/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/DeleteShippingCarrier/DeleteShippingCarrierCommand.cs` | Declares ShippingCarriers/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/GetAllShippingCarriersInfo/GetAllShippingCarriersInfoQuery.cs` | Declares ShippingCarriers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierById/GetShippingCarrierByIdQuery.cs` | Declares ShippingCarriers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarrierForEdit/GetShippingCarrierForEditQuery.cs` | Declares ShippingCarriers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriers/GetShippingCarriersQuery.cs` | Declares ShippingCarriers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/GetShippingCarriersByName/GetShippingCarriersByNameQuery.cs` | Declares ShippingCarriers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/ShippingCarriers/UpdateShippingCarrier/UpdateShippingCarrierCommand.cs` | Declares ShippingCarriers/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Suppliers/CreateSupplier/CreateSupplierCommand.cs` | Declares Suppliers/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Suppliers/DeleteSupplier/DeleteSupplierCommand.cs` | Declares Suppliers/Write; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Suppliers/GetAllSuppliers/GetAllSuppliersQuery.cs` | Declares Suppliers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Suppliers/GetSupplierForEdit/GetSupplierForEditQuery.cs` | Declares Suppliers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Suppliers/GetSuppliersInfo/GetSuppliersInfoQuery.cs` | Declares Suppliers/Read; existing handler logic is unchanged. |
| `OrderHub.Application/Features/Setup/Suppliers/UpdateSupplier/UpdateSupplierCommand.cs` | Declares Suppliers/Write; existing handler logic is unchanged. |
| `OrderHub.Application/ServiceCollectionExtensions.cs` | Scoped Application decision service and authorization pipeline in existing MediatR setup. |
| `OrderHub.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` | Registers the scoped permission reader through existing Infrastructure DI. |
| `OrderHub.Tests/Application/Features/Administration/Roles/RoleFailureAndRegistrationTests.cs` | Supplies an explicit authenticated admin identity and a strict unused permission reader to the existing dispatch test. |
| `OrderHub.Tests/Application/Features/Administration/Users/Update/UpdateUserCommandHandlerTests.cs` | Replaces obsolete per-user permission assumptions with assignment/removal/missing-role/admin-role assertions; retains all five basic/status checks. |
| `OrderHub.Tests/Infrastructure/SetupPersistenceTests.cs` | Supplies an explicit authenticated admin identity for existing business persistence tests. |
| `OrderHub.Web/Components/Common/Buttons/CreateOrderButton.razor` | Only offers order creation with Orders.Write. |
| `OrderHub.Web/Components/Layout/NavMenu.razor` | Feature links use Read checks; existing Admin gate is retained for user/role navigation. |
| `OrderHub.Web/Components/Pages/Features/Administration/Roles/Editor/Editor.razor` | Adds Users.Write route policy while retaining Admin-only Web access. |
| `OrderHub.Web/Components/Pages/Features/Administration/Roles/Index.razor` | Adds Users.Read route policy while retaining Admin-only Web access. |
| `OrderHub.Web/Components/Pages/Features/Administration/Users/Create.razor` | Adds Users.Write route policy while retaining Admin-only Web access. |
| `OrderHub.Web/Components/Pages/Features/Administration/Users/Edit.razor` | Adds Users.Write route policy while retaining Admin-only Web access. |
| `OrderHub.Web/Components/Pages/Features/Administration/Users/Index.razor` | Adds Users.Read route policy while retaining Admin-only Web access. |
| `OrderHub.Web/Components/Pages/Features/Orders/Editor/OrderEditor.razor` | All create/edit/continue routes require Orders.Write. |
| `OrderHub.Web/Components/Pages/Features/Orders/Editor/Products/ProductsStep.razor` | Offers product search/add only with Products.Read. |
| `OrderHub.Web/Components/Pages/Features/Orders/Index/Orders.razor` | Orders.Read page policy; order edit controls require Orders.Write. |
| `OrderHub.Web/Components/Pages/Features/Orders/Index/Orders.razor.cs` | Checks PaymentMethods.Read before fetching optional filter choices; Orders.Read alone can load the list. |
| `OrderHub.Web/Components/Pages/Features/Orders/OrderDetials.razor` | Protects formerly public order-details route with Orders.Read. |
| `OrderHub.Web/Components/Pages/Features/Orders/OrderDrafts.razor` | Resume and delete actions require Orders.Write; read listing remains available. |
| `OrderHub.Web/Components/Routes.razor` | Differentiates anonymous login redirect from authenticated denial and contains propagated interactive denials. |
| `OrderHub.Web/Components/_Imports.razor` | Makes typed policies, enums and permission UI helpers available to components. |
| `OrderHub.Web/Controllers/Files/AttachmentsController.cs` | GET requires Orders.Read; upload/delete require Orders.Write. |
| `OrderHub.Web/Controllers/Files/InvoicesController.cs` | GET requires Documents.Read; upload/delete require Documents.Write. |
| `OrderHub.Web/Controllers/Files/ProformaInvoicesController.cs` | GET requires Documents.Read; upload/delete require Documents.Write. |
| `OrderHub.Web/Controllers/Files/QuotationsController.cs` | GET requires Documents.Read; upload/delete require Documents.Write. |
| `OrderHub.Web/Program.cs` | Registers permission policy adapters and the Application denial HTTP middleware. |
| `OrderHub.Web/Components/Pages/Home.razor` | Dashboard create shortcuts require Orders/Products/Clients.Write; the orders-review shortcut requires Orders.Read. |

No Domain entity, EF mapping, migration, package, authentication identity service, executor implementation, cookie settings or ownership rule was modified. No competing active authorization implementation existed in the current Application/Web checkout. Historical migration snapshots referencing UserPermission remain intact for migration history; dormant desktop UI code was not treated as an active Web authorization system.

## 5. Application Contracts

Namespace: `OrderHub.Application.Authorization`.

```csharp
public interface IAuthorizationService
{
    Task<bool> IsAllowedAsync(
        PermissionFeature feature,
        PermissionLevel requiredLevel,
        CancellationToken cancellationToken = default);
}

public interface IUserPermissionReader
{
    Task<PermissionLevel> GetPermissionLevelAsync(
        int userId,
        PermissionFeature feature,
        CancellationToken cancellationToken = default);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequirePermissionAttribute(
    PermissionFeature feature, PermissionLevel requiredLevel) : Attribute
{
    public PermissionFeature Feature { get; } = feature;
    public PermissionLevel RequiredLevel { get; } = requiredLevel;
}

public sealed record CheckPermissionQuery(
    PermissionFeature Feature, PermissionLevel RequiredLevel) : IRequest<bool>;

public sealed class AuthorizationDeniedException()
    : Exception("غير مصرح لك بتنفيذ هذه العملية.");
```

The attribute is the small authorized-request metadata mechanism equivalent to IAuthorizedRequest. It contains no decision logic. CheckPermissionQuery contains no actor input and exposes only the current actor's boolean access decision. Existing command target UserId/RoleId values remain resource identifiers, not actor identity.

The existing Result types represent business failures and are not shared by every request: some return models, lists, strings, integers or no value. A single exception therefore represents authorization denial across all signatures without manufacturing fake results. It is internally distinct from validation, not-found and business failures. Web translates propagated HTTP denial to 403 and interactive denial to a generic access-denied destination. Existing component-specific generic error handling remains where already present.

## 6. Authorization Decision Rules

`AuthorizationService.IsAllowedAsync` is the exact enforcement decision location; `AuthorizationBehavior.Handle` applies it before protected handlers.

1. Unauthenticated users are denied without a permission read.
2. Missing, zero or negative current-user Id is denied, including an admin claim with invalid Id.
3. Unsupported features and requirements fail closed. None is not a protected requirement: omit metadata for a genuinely exempt operation. Explicit None/invalid metadata is denied.
4. Valid authenticated IsAdmin bypasses permission lookup, regardless of whether a role is assigned.
5. Normal users read the effective level for the identity's Id and requested feature.

| Actual | Required Read | Required Write |
| --- | --- | --- |
| None | Deny | Deny |
| Read | Allow | Deny |
| Write | Allow | Allow |
| Invalid enum level | Deny | Deny |

Comparison uses explicit enum cases, never numeric ordering. Role names do not enter the decision. A missing user, role or feature yields None. No existing-session IsActive check was added.

## 7. Permission Reader

`Infrastructure/Features/Administration/UserPermissionReader.cs` implements the Application reader. Each call creates and disposes a read context through the existing AppDbContextFactory, performs one narrow EF query and returns only a nullable level:

```csharp
var level = await context.Users.AsNoTracking()
    .Where(user => user.Id == userId)
    .SelectMany(user => user.Role.Permissions)
    .Where(permission => permission.Feature == feature)
    .Select(permission => (PermissionLevel?)permission.Level)
    .FirstOrDefaultAsync(cancellationToken);
return level ?? PermissionLevel.None;
```

Navigation translation joins the current user's assigned role to its feature row. No aggregate loading, Includes, repository loading, tracking snapshot or cache is used. Missing relationships produce no row and therefore None. Cancellation reaches EF. The composite RoleId/Feature key guarantees at most one matching row.

Tests verify changes, row removal, role reassignment and role removal are visible on the next read, using the same reader instance. The real pipeline test also observes permission changes within the same mediator scope. IsActive is deliberately absent from this query. The existing string conversions, composite key, database check excluding None, and Domain Role.SetPermission behavior are unchanged; no schema migration was needed.

## 8. MediatR Pipeline

Existing AddApplication registers the scoped decision service and adds AuthorizationBehavior via `cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>))` within the existing MediatR registration. Infrastructure does not register a second MediatR scan.

For protected runtime request types, the behavior obtains RequirePermissionAttribute and awaits the decision before invoking `next`. A denial throws AuthorizationDeniedException and never invokes the business handler delegate. This covers Result and non-Result requests and MediatR void requests. Exempt requests continue without calling authorization. Handler construction/DI can occur before invocation; existing handler constructors only receive dependencies and perform no business mutations.

## 9. Protected Use Cases

Classification follows the inspected handler behavior: read projections use Read; persistence and state changes use Write. Role/user administration uses the existing Users feature. Order drafts use Orders, in addition to their existing owner checks. Document creation writes persisted documents/files and uses Documents.Write. Edit-data queries remain read-only Application operations; Web edit routes require Write.

| Request | Feature | Required Level |
| --- | --- | --- |
| `CreateRoleCommand` | Users | Write |
| `DeleteRoleCommand` | Users | Write |
| `GetRoleQuery` | Users | Read |
| `GetRolesQuery` | Users | Read |
| `UpdateRoleCommand` | Users | Write |
| `ActivateUserCommand` | Users | Write |
| `CreateUserCommand` | Users | Write |
| `DeactivateUserCommand` | Users | Write |
| `GetUserQuery` | Users | Read |
| `GetUsersQuery` | Users | Read |
| `UpdateUserCommand` | Users | Write |
| `CreateInvoiceCommand.Command` | Documents | Write |
| `CreateProformaInvoiceCommand.Command` | Documents | Write |
| `CreateQuotationCommand.Command` | Documents | Write |
| `DeleteDraftCommand` | Orders | Write |
| `GetDraftQuery` | Orders | Read |
| `GetDraftListQuery` | Orders | Read |
| `SaveDraftCommand` | Orders | Write |
| `ChangePaymentMethodCommand` | Orders | Write |
| `CancelOrderCommand` | Orders | Write |
| `DeliverOrderCommand` | Orders | Write |
| `ReturnOrderToPendingCommand` | Orders | Write |
| `ShipOrderCommand` | Orders | Write |
| `StartOrderProcessingCommand` | Orders | Write |
| `CreateOrderCommand` | Orders | Write |
| `DeleteOrder.Command` | Orders | Write |
| `GetOrderDetailsQuery` | Orders | Read |
| `GetOrderEditorLookupsQuery` | Orders | Read |
| `GetOrderForEditQuery` | Orders | Read |
| `GetOrderItemEditorQuery` | Orders | Read |
| `GetOrderItemsEditorQuery` | Orders | Read |
| `GetOrdersPageQuery` | Orders | Read |
| `UpdateOrderCommand` | Orders | Write |
| `CreateProductCommand` | Products | Write |
| `DeleteProductCommand` | Products | Write |
| `GetProductQuery` | Products | Read |
| `GetProductsInfoQuery` | Products | Read |
| `ListProductsQuery` | Products | Read |
| `SearchProductsQuery` | Products | Read |
| `UpdateProductCommand` | Products | Write |
| `CreateCategoryCommand` | Categories | Write |
| `DeleteCategoryCommand` | Categories | Write |
| `GetCategoriesInfoQuery` | Categories | Read |
| `GetCategoryForEditQuery` | Categories | Read |
| `GetCategoryListQuery` | Categories | Read |
| `GetCategoryParentIdsQuery` | Categories | Read |
| `GetCategoryTreeQuery` | Categories | Read |
| `GetRootCategoriesQuery` | Categories | Read |
| `GetSubCategoriesQuery` | Categories | Read |
| `UpdateCategoryCommand` | Categories | Write |
| `CreateCityCommand` | Cities | Write |
| `DeleteCityCommand` | Cities | Write |
| `GetAllCitiesQuery` | Cities | Read |
| `GetCitiesInfoQuery` | Cities | Read |
| `GetCityForEditQuery` | Cities | Read |
| `UpdateCityCommand` | Cities | Write |
| `CreateClientCommand` | Clients | Write |
| `DeleteClientCommand` | Clients | Write |
| `GetAllClientsQuery` | Clients | Read |
| `GetAllClientsPagedQuery` | Clients | Read |
| `GetClientByIdQuery` | Clients | Read |
| `GetClientEditQuery` | Clients | Read |
| `GetClientsByNameQuery` | Clients | Read |
| `GetClientsInfoQuery` | Clients | Read |
| `UpdateClientCommand` | Clients | Write |
| `CreateDeliverymanCommand` | Deliverymen | Write |
| `DeleteDeliverymanCommand` | Deliverymen | Write |
| `GetAllDeliverymenInfoQuery` | Deliverymen | Read |
| `GetAllDeliverymenListQuery` | Deliverymen | Read |
| `GetDeliverymanByIdQuery` | Deliverymen | Read |
| `GetDeliverymanForEditQuery` | Deliverymen | Read |
| `GetDeliverymenByNameQuery` | Deliverymen | Read |
| `UpdateDeliverymanCommand` | Deliverymen | Write |
| `GetPaymentMethodListQuery` | PaymentMethods | Read |
| `CreatePropertyCommand` | Properties | Write |
| `DeletePropertyCommand` | Properties | Write |
| `GetPropertyQuery` | Properties | Read |
| `GetPropertiesQuery` | Properties | Read |
| `UpdatePropertyCommand` | Properties | Write |
| `CreateShippingCarrierCommand` | ShippingCarriers | Write |
| `DeleteShippingCarrierCommand` | ShippingCarriers | Write |
| `GetAllShippingCarriersInfoQuery` | ShippingCarriers | Read |
| `GetShippingCarrierByIdQuery` | ShippingCarriers | Read |
| `GetShippingCarrierForEditQuery` | ShippingCarriers | Read |
| `GetShippingCarriersQuery` | ShippingCarriers | Read |
| `GetShippingCarriersByNameQuery` | ShippingCarriers | Read |
| `UpdateShippingCarrierCommand` | ShippingCarriers | Write |
| `CreateSupplierCommand` | Suppliers | Write |
| `DeleteSupplierCommand` | Suppliers | Write |
| `GetAllSuppliersQuery` | Suppliers | Read |
| `GetSupplierForEditQuery` | Suppliers | Read |
| `GetSuppliersInfoQuery` | Suppliers | Read |
| `UpdateSupplierCommand` | Suppliers | Write |

Total: 93 protected concrete business requests. Logs and Settings have no active Application request/handler in this checkout; no imaginary operations or new enum values were added.

Explicit exemptions: DatabaseLoginCommand (establishes authentication), CreateAdminCommand (existing first-admin bootstrap, guarded by AnyAdminAsync) and CheckPermissionQuery (current identity's boolean access check). Logout is an existing HTTP authentication operation and remains outside business permissions.

Order editor lookup and item-editor projections belong to the existing Orders use cases and require Orders.Read. Independent catalog/search/property queries retain Products.Read/Properties.Read. Related data already embedded in order/document projections retains its existing aggregate use-case boundary; no new resource ACL was introduced.

## 10. Web Integration

| Route | Requirement |
| --- | --- |
| /orders | Orders.Read |
| /orders/{id:int} | Orders.Read |
| /orders/create | Orders.Write |
| /orders/continue/{DraftId:guid} | Orders.Write |
| /orders/{OrderId:int}/edit | Orders.Write |
| /admin/users | Users.Read AND existing Admin gate |
| /admin/users/create | Users.Write AND existing Admin gate |
| /admin/users/{UserId:int}/edit | Users.Write AND existing Admin gate |
| /admin/roles | Users.Read AND existing Admin gate |
| /admin/roles/create | Users.Write AND existing Admin gate |
| /admin/roles/{RoleId:int}/edit | Users.Write AND existing Admin gate |

Direct navigation is checked by ASP.NET permission policies / AuthorizeRouteView. Existing `[Authorize(Roles = "Admin")]` attributes remain. Admin is the established global identity flag/claim, not an application Role.Name rule. An authenticated denial shows a generic message; an anonymous route failure retains the login redirect.

PermissionView calls PermissionAccess on parameter updates and renders only when allowed. Read is its default; Write is explicit for create/edit and draft resume/delete. Orders.Read users can see orders and their owned draft list but are not offered create/edit/resume/delete controls. Product search/add controls additionally require Products.Read. All actual use cases still pass through IRequestExecutor and the Application pipeline. UI visibility is display state only and is never accepted as an authorization grant; a permission change after rendering is enforced by the next protected operation.

The navigation links for Orders, catalog/setup, business partners, documents, Cities and administration use their feature's Read requirement. User/role links additionally retain the Admin gate. Existing links to unimplemented pages remain placeholders; they were not converted into new pages. The unrelated Messages link has no approved feature mapping and remains unchanged.

Dashboard create shortcuts also require the corresponding feature Write permission; the orders-review shortcut requires Orders.Read. Home retains its existing authentication requirement and static demonstration statistics; it performs no business data query.

Optional payment-method filter data is fetched only if PaymentMethods.Read is allowed, avoiding an unrelated permission failure for an Orders.Read-only visitor. Independent product search and legacy draft property restoration still require Products.Read/Properties.Read respectively; configure those permissions when those editor capabilities are needed. No business permission was weakened to make these dependencies disappear.

## 11. Controllers / Endpoints

| Endpoint family | GET | POST upload | DELETE |
| --- | --- | --- | --- |
| /files/attachments/{fileName} | Orders.Read | Orders.Write | Orders.Write |
| /files/invoices/{fileName} | Documents.Read | Documents.Write | Documents.Write |
| /files/quotations/{fileName} | Documents.Read | Documents.Write | Documents.Write |
| /files/proforma-invoices/{fileName} | Documents.Read | Documents.Write | Documents.Write |

All 12 file actions have typed PermissionAuthorize metadata. Policies call Application through the executor before endpoint/file-service invocation. Authenticated permission-policy failure returns generic HTTP 403, rather than redirecting to the framework's nonexistent default access-denied path. Anonymous callers are challenged by existing cookie authentication. Existing filename validation and file behavior remain unchanged. Attachments belong to the Orders boundary; commercial document files belong to Documents.

`/auth/login` and `/auth/logout` remain authentication endpoints, without business permissions. Login still rejects an absent or inactive user before password verification and never emits a successful inactive UserSession. `/health` remains deliberately anonymous. Static assets, login, generic error/not-found/access-denied pages, sample Counter/Weather pages and first-admin setup are not protected business data routes. No additional protected Web controller was found.

## 12. Tests

Focused authorization run: 35 passed, 0 failed, 0 skipped.

| Test class | Cases | Coverage |
| --- | --- | --- |
| Application.AuthorizationTests | 21 | Six-case level matrix plus invalid actual level; anonymous/invalid identity; admin bypass without lookup; invalid required level/feature; protected allowed/denied and unprotected pipeline; void request denial and zero side effects. |
| Infrastructure.UserPermissionReaderTests | 5 | No role, absent feature, Read, Write, missing user; permission change/removal, role reassignment/removal; inactive existing identity intentionally still reads permissions. |
| Infrastructure.AuthorizationIntegrationTests | 2 | Real mediator/DI and SQLite mutation denial before persistence; grant/revoke visible immediately; exhaustive metadata coverage of all business requests with only explicit exemptions. |
| Web.PermissionAuthorizationTests | 7 | Concurrent operation identity isolation; Web policy delegation for allow/deny and anonymous rejection; all file action policies; HTTP 403 mappings and endpoint non-execution; business routes and preserved Admin gates. |

Existing SQLite TestDbFactory and the existing OrderHub.Tests project were used. No second test architecture or package was introduced. Existing inactive-login, draft ownership, executor identity/circuit isolation, persistence and editor lifecycle tests pass in the full suite.

Before validation, the test project had a pre-existing CS0246 referencing removed UpdateUserRequestPermission. The old user-update permission tests also used the removed per-user Permissions/SetPermission model and an obsolete handler constructor. They were updated to the explicitly agreed role model: role assignment for normal users/admins, removal and missing-role rejection. The five existing basic-data/status tests were retained. No production user handler was changed.

After enforcement, 13 existing persistence/registration tests initially failed because their mediator fixtures supplied no authenticated identity. The shared persistence fixture and Roles registration fixture now explicitly provide an authenticated admin; this preserves their original business/persistence focus. Dedicated normal-user/denial tests exercise the real authorization service and reader. No unrelated failing test was removed, skipped or weakened.

Evidence is under the existing ignored `OrderHub.Tests/TestResults` directory: Authorization-focused.trx, Authorization-full.trx and authorization build/test logs. These are local validation artifacts, not staged source changes.

## 13. Build Results

| Build target | Errors | Warnings | Result |
| --- | --- | --- | --- |
| OrderHub.Domain.csproj --no-restore | 0 | 0 | Pass |
| OrderHub.Application.csproj --no-restore | 0 | 0 | Pass |
| OrderHub.Infrastructure.csproj --no-restore | 0 | 0 | Pass |
| OrderHub.Web.csproj --no-restore | 0 | 0 in targeted incremental build | Pass |
| OrderHub.sln --no-restore --no-incremental | 0 | 2 existing | Pass |

A full production rebuild confirms the two pre-existing MUD0002 warnings about AlignItems on MudGrid in the Roles Editor. The same two warnings were visible during initial inspection/build. Existing test sources emit BL0005/BL0006 warnings from component parameter assignment and RenderTree test helpers. No new authorization-source warning was introduced or suppressed.

OrderHub.sln contains the four current production projects. The test project is separate and was built/run explicitly. Dormant OrderHub.UI/OrderHub.Api projects are not solution targets and were not expanded into this task. No live PostgreSQL deployment or browser session was used; database behavior was validated through the existing SQLite convention and production EF query/DI.

## 14. Full Test Results

Final command:

```text
dotnet test OrderHub.Tests/OrderHub.Tests.csproj --no-build --no-restore
  --verbosity normal --logger "trx;LogFileName=Authorization-full.trx"
```

The test assembly had already been built by the focused run after the final behavior changes. TRX counters:

```text
Passed: 242
Failed: 0
Skipped: 0
Total: 242
```

The full suite has no remaining failing test. Focused validation was run before the final full suite.

## 15. Ambiguous Cases

- The existing `/messages` placeholder has no matching active Web use case or approved PermissionFeature. It was intentionally left unchanged rather than guessing a permission or extending the enum. It exposes no implemented business page in this checkout.
- First-admin bootstrap is an explicit pre-authentication exception, not Users.Write. CreateAdminCommand's existing AnyAdminAsync guard remains; its Web form currently contains commented-out submission code. This pre-existing setup incompleteness was not repaired as an authorization change.
- No active Logs/Settings request exists to classify. Missing placeholder functionality was not implemented.

No other concrete current business request was left without metadata. Cross-feature editor read dependencies are documented in Sections 9 and 10 rather than silently broadening permission grants.

## 16. Security Review

| Question | Answer |
| --- | --- |
| Can an unauthenticated user invoke protected business operations? | No: Application rejects before handler execution. Login and first-admin bootstrap are explicit non-business-permission exceptions; a permission check only returns false. |
| Can a Read-only user execute Write operations? | No: explicit comparison rejects Read for Write before handler effects. |
| Can a user without a Role access protected features? | A normal user cannot. A valid authenticated admin can, by the agreed global bypass. |
| Can a user access a feature missing from RolePermissions? | A normal user cannot; missing row is None. Admin bypass remains intentional. |
| Does Write imply Read? | Yes, explicitly. |
| Does Read fail to imply Write? | Yes, explicitly. |
| Does Admin bypass RolePermissions? | Yes, with valid authenticated identity/positive Id; no permission lookup occurs. |
| Can Role names affect authorization? | No; neither service nor reader compares names. The legacy global Admin identity claim remains separate from database Role.Name. |
| Can changing RolePermissions take effect without a new login? | Yes, on the next check/protected operation, including the same reader/mediator scope. User role reassignment/removal is also observed. |
| Can Web UI bypass Application authorization? | No: hiding is UX only, and dispatched protected business requests are guarded independently. File endpoints additionally dispatch Application access checks before their existing file-service calls. |
| Are any protected Controllers still publicly accessible? | No current protected file action is public to anonymous/unauthorized HTTP callers. Auth and health endpoints remain intentionally reachable. |
| Are any protected Razor routes still publicly accessible? | No current Orders/User/Role business route lacks route policies. Existing unrelated sample/error/bootstrap routes are explicitly outside this business boundary. |
| Are any Commands/Queries missing authorization metadata? | No concrete business request: 93 are protected; login, first-admin bootstrap and current-identity permission check are the three explicit exemptions. Interfaces are not dispatchable concrete requests. |
| Is there any direct EF authorization query in Web? | No; Infrastructure alone implements permission reads. Existing architecture tests pass. |
| Is there authorization state stored globally/static/singleton? | No per-user state or permission snapshot. Only fixed policy metadata and a stateless HTTP result adapter are singleton; identity/decision dependencies are operation-compatible scoped services. |

Source audit confirmed Web's only mediator calls remain inside AuthenticatedRequestExecutor. No new direct handler/database dispatch, actor field, role-name rule, cache, permission table or current-user architecture was introduced. Existing draft owner validation and Domain invariants are preserved. `git diff --check` passes.

## 17. Remaining Risks

- Users.Write is a highly privileged feature: existing user commands accept IsAdmin and role assignment. Assign it only to trusted administration roles. The existing Admin-only Web management gate is preserved, while Application correctly follows the agreed Users.Write requirement. No additional admin-only Application tier was invented.
- First-admin bootstrap retains its pre-existing public onboarding guard. Its check-then-create sequence is not a concurrency-safe provisioning design, and the current Web submission is incomplete. Deployment/onboarding access and bootstrap hardening need separate attention.
- File permissions are feature-level. Existing arbitrary filename endpoints do not introduce resource ownership checks in this task; holders of the relevant feature permission can access files within that feature. Draft ownership remains enforced separately as before.
- Already issued identity claims, including IsAdmin, are not refreshed here. Admin demotion/deactivation and existing cookies/circuits require the deferred authentication work. For normal users, role permission and assignment changes are current database reads.
- PermissionView represents UI visibility between renders; a role change may leave an old control visible briefly, but the next protected operation still rechecks and denies. Already delivered data cannot be recalled by revoking permission.
- Every non-admin check incurs a narrow database read; permission-aware menus/row controls can generate multiple checks. No permission cache was added. SQL behavior was tested through SQLite, not against a live PostgreSQL deployment.

## 18. Deferred Work

- Existing-session IsActive revalidation, cookie/circuit revocation and handling already issued identity claims.
- Permission caching, only if measurements justify it and an explicit invalidation strategy is designed.
- Authentication hardening, including provisioning/session concerns and existing broader cookie/antiforgery/HTTPS decisions.

None of these deferred changes was implemented. Inactive-user prevention during a new login remains intact. No multi-role model, per-user overrides, inheritance, Manage level, resource ACL, tenant authorization or authentication redesign was introduced.
