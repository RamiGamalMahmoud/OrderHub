using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Roles.GetRoles;

internal sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleListItem>>
{
    private readonly IRoleListingService _reader;

    public GetRolesQueryHandler(IRoleListingService reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<RoleListItem>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        return _reader.GetAsync(cancellationToken);
    }
}
