using MediatR;
using OrderHub.Application.Authorization;
using OrderHub.Application.Context;
using OrderHub.Application.Features.Inventory.Approvals.Contracts;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Inventory.Approvals.GetApprovals;

internal sealed class GetApprovalsQueryHandler(
    IInventoryApprovalRepository approvals,
    IAuthorizationService authorization,
    ICurrentUser currentUser) : IRequestHandler<GetApprovalsQuery, Result<IReadOnlyList<InventoryApprovalProjectionRow>>>
{
    public async Task<Result<IReadOnlyList<InventoryApprovalProjectionRow>>> Handle(
        GetApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.Id is not int userId || userId <= 0)
            return Result<IReadOnlyList<InventoryApprovalProjectionRow>>.Failure(
                "يجب تسجيل الدخول لعرض الاعتمادات.",
                "Unauthorized");

        if (!await authorization.IsAllowedAsync(
                PermissionFeature.Inventory,
                PermissionLevel.Approve,
                cancellationToken))
        {
            return Result<IReadOnlyList<InventoryApprovalProjectionRow>>.Failure(
                "عرض الاعتمادات متاح للمدير المختص.",
                "Forbidden");
        }

        IReadOnlyList<InventoryApprovalProjectionRow> rows = await approvals.GetRowsAsync(
            request.Status,
            request.RequestType,
            cancellationToken);

        return Result<IReadOnlyList<InventoryApprovalProjectionRow>>.Success(rows);
    }
}
