using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.GetStockInContext;

internal sealed class GetStockInContextQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryRepository inventory,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetStockInContextQuery, Result<StockInContext>>
{
    public async Task<Result<StockInContext>> Handle(
        GetStockInContextQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<StockInContext>.Failure("يجب تسجيل الدخول لعرض صفحة التوريد.", "Unauthorized");

        Warehouse warehouse = await warehouses.GetByKeeperAsync(userId, cancellationToken);
        if (warehouse is null)
        {
            return Result<StockInContext>.Failure(
                "تسجيل التوريد متاح لأمين المخزن المرتبط بمخزن فقط.",
                "Forbidden");
        }

        if (!warehouse.IsActive)
            return Result<StockInContext>.Failure("المخزن المرتبط بحسابك غير نشط.");

        bool openingAllowed = !await inventory.HasMovementsAsync(warehouse.Id, cancellationToken);
        bool canReadProducts = await authorization.IsAllowedAsync(
            PermissionFeature.Products,
            PermissionLevel.Read,
            cancellationToken);
        bool canSelectSupplier = await authorization.IsAllowedAsync(
            PermissionFeature.Suppliers,
            PermissionLevel.Read,
            cancellationToken);

        return Result<StockInContext>.Success(new StockInContext(
            warehouse.Id,
            warehouse.Name.Value,
            openingAllowed,
            canReadProducts,
            canSelectSupplier));
    }
}
