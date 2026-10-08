using OrderHub.Domain.Enums;
using OrderHub.Application.Authorization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;

namespace OrderHub.Application.Features.OrderDrafts.List;

[RequirePermission(PermissionFeature.Orders, PermissionLevel.Read)]
public sealed record GetDraftListQuery : IRequest<IReadOnlyList<OrderDraftSummary>>;

internal sealed class GetDraftListQueryHandler(ICurrentUser currentUser, IDraftReadService reads)
    : IRequestHandler<GetDraftListQuery, IReadOnlyList<OrderDraftSummary>>
{
    public async Task<IReadOnlyList<OrderDraftSummary>> Handle(GetDraftListQuery request, CancellationToken cancellationToken) =>
        await reads.GetUserDraftsAsync(DraftUser.Require(currentUser), cancellationToken);
}
