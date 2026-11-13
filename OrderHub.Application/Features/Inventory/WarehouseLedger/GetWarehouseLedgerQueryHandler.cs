using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Presentation;
using OrderHub.Application.Features.Inventory.WarehouseStock;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.WarehouseLedger;

internal sealed class GetWarehouseLedgerQueryHandler(
    IWarehouseRepository warehouses,
    IInventoryRepository inventory,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetWarehouseLedgerQuery, Result<WarehouseLedgerPage>>
{
    public async Task<Result<WarehouseLedgerPage>> Handle(
        GetWarehouseLedgerQuery request,
        CancellationToken cancellationToken)
    {
        Result<InventoryScope> scopeResult = await InventoryScope.ResolveAsync(
            currentUser,
            authorization,
            warehouses,
            cancellationToken);
        if (!scopeResult.IsSuccess)
            return Result<WarehouseLedgerPage>.Failure(scopeResult.ErrorMessage, scopeResult.ErrorCode);

        Result<InventoryWarehouseHeaderRow> warehouseResult = await InventoryScope.ResolveWarehouseAsync(
            scopeResult.Value,
            warehouses,
            request.WarehouseId,
            cancellationToken);
        if (!warehouseResult.IsSuccess)
            return Result<WarehouseLedgerPage>.Failure(warehouseResult.ErrorMessage, warehouseResult.ErrorCode);

        WarehouseProductStockRow product = await inventory.GetWarehouseProductStockAsync(
            request.WarehouseId,
            request.ProductId,
            cancellationToken);
        if (product is null)
        {
            return Result<WarehouseLedgerPage>.Failure(
                "لا يوجد رصيد لهذا المنتج في المخزن.",
                "NotFound");
        }

        (int page, int pageSize) = StockPaging.Normalize(request.Page, request.PageSize);
        int skip = (page - 1) * pageSize;

        IReadOnlyList<WarehouseLedgerProjectionRow> rows = await inventory.GetWarehouseLedgerPageAsync(
            request.WarehouseId,
            request.ProductId,
            skip,
            pageSize,
            cancellationToken);
        int totalCount = await inventory.CountLedgerAsync(
            request.WarehouseId,
            request.ProductId,
            cancellationToken);

        return Result<WarehouseLedgerPage>.Success(new WarehouseLedgerPage(
            warehouseResult.Value.Name,
            new WarehouseLedgerProduct(
                product.ProductId,
                product.ProductName,
                product.ProductCode,
                product.StockOnHand,
                product.ReservedOnHand,
                product.DamagedQty),
            rows.Select(row => new WarehouseLedgerRow(
                row.Id,
                row.MovementType,
                row.Direction,
                row.Status,
                row.Quantity,
                row.EffectiveAtUtc,
                row.CreatedAtUtc,
                row.PerformedByName,
                row.Note,
                row.ReceiptReferenceNote,
                row.IssueOrderNumber,
                row.IssueOrderYear,
                row.IssueOrderMonth,
                row.ReversalOfMovementId)).ToList(),
            totalCount,
            page,
            pageSize));
    }
}
