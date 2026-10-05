using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Features.OrderDrafts.Contracts;

namespace OrderHub.Application.Features.OrderDrafts.Get;

public sealed record GetDraftQuery(Guid DraftId) : IRequest<Draft>;

internal sealed class GetDraftQueryHandler(ICurrentUser currentUser, IDraftReadService reads) : IRequestHandler<GetDraftQuery, Draft>
{
    public async Task<Draft> Handle(GetDraftQuery request, CancellationToken cancellationToken) =>
        await reads.GetOwnedAsync(request.DraftId, await DraftUser.RequireAsync(currentUser, cancellationToken), cancellationToken);
}
