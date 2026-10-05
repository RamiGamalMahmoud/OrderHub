using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Roles.GetRole;

internal sealed class GetRoleQueryHandler : IRequestHandler<GetRoleQuery, GetRoleResponse>
{
    private readonly IRoleDetailsService _roleDetailsService;

    public GetRoleQueryHandler(IRoleDetailsService roleDetailsService)
    {
        _roleDetailsService = roleDetailsService;
    }

    public Task<GetRoleResponse> Handle(GetRoleQuery request, CancellationToken cancellationToken)
    {
        return _roleDetailsService.GetRoleDetailsAsync(request.RoleId, cancellationToken);
    }
}
