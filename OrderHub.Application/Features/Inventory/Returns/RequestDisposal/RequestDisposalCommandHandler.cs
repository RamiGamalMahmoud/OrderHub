using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Returns.RequestDisposal;

internal sealed class RequestDisposalCommandHandler(
    IWarehouseRepository warehouses,
    IInventoryRepository inventory,
    IInventoryApprovalRepository approvals,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : IRequestHandler<RequestDisposalCommand, Result>
{
    public async Task<Result> Handle(RequestDisposalCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result.Failure("يجب تسجيل الدخول لطلب إتلاف التالف.", "Unauthorized");

        if (request.ProductId <= 0)
            return Result.Failure("الصنف غير صالح.");

        if (request.Quantity <= 0)
            return Result.Failure("كمية الإتلاف يجب أن تكون أكبر من صفر.");

        Warehouse warehouse = await warehouses.GetByIdAsync(request.WarehouseId, cancellationToken);
        if (warehouse is null)
            return Result.Failure("المخزن غير موجود.", "NotFound");

        if (!warehouse.IsActive)
            return Result.Failure("المخزن غير نشط؛ لا يمكن طلب إتلاف عليه.", "Conflict");

        if (warehouse.KeeperUserId != userId)
        {
            return Result.Failure(
                "طلب الإتلاف متاح لأمين المخزن المرتبط بهذا المخزن فقط.",
                "Forbidden");
        }

        StockBalance balance = await inventory.GetStockItemAsync(
            warehouse.Id,
            request.ProductId,
            cancellationToken);
        if (balance is null)
            return Result.Failure("لا يوجد رصيد لهذا الصنف في المخزن.", "NotFound");

        if (request.Quantity > balance.DamagedQty)
        {
            return Result.Failure(
                $"كمية الإتلاف تتجاوز الكمية التالفة المسجلة ({balance.DamagedQty.ToString(System.Globalization.CultureInfo.InvariantCulture)}).",
                "Conflict");
        }

        if (await approvals.HasPendingDisposalAsync(warehouse.Id, request.ProductId, cancellationToken))
            return Result.Failure("يوجد طلب إتلاف قيد الاعتماد لهذا الصنف؛ انتظر قراره.", "Conflict");

        var now = clock.GetUtcNow().UtcDateTime;
        Result<InventoryApprovalRequest> created = InventoryApprovalRequest.CreateDamagedDisposal(
            warehouse.Id,
            request.ProductId,
            request.Quantity,
            balance.Version,
            request.Reason,
            userId,
            now);
        if (!created.IsSuccess)
            return Result.Failure(created.ErrorMessage);

        approvals.Add(created.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
