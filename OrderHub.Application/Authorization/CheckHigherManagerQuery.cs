using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Authorization;

// This identity-only check exposes no business data and needs no feature permission itself.
public sealed record CheckHigherManagerQuery : IRequest<bool>;

internal sealed class CheckHigherManagerQueryHandler(IAuthorizationService authorization) : IRequestHandler<CheckHigherManagerQuery, bool>
{
    public Task<bool> Handle(CheckHigherManagerQuery request, CancellationToken cancellationToken) =>
        authorization.IsHigherManagerAsync(cancellationToken);
}
