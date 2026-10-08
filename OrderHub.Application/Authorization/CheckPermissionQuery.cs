using MediatR;
using OrderHub.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Authorization;

// This identity-only check exposes no business data and needs no feature permission itself.
public sealed record CheckPermissionQuery(PermissionFeature Feature, PermissionLevel RequiredLevel) : IRequest<bool>;

internal sealed class CheckPermissionQueryHandler(IAuthorizationService authorization) : IRequestHandler<CheckPermissionQuery, bool>
{
    public Task<bool> Handle(CheckPermissionQuery request, CancellationToken cancellationToken) =>
        authorization.IsAllowedAsync(request.Feature, request.RequiredLevel, cancellationToken);
}
