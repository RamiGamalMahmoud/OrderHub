using MediatR;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Enums;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses.GetKeeperCandidates;

internal sealed class GetWarehouseKeeperCandidatesQueryHandler
    : IRequestHandler<GetWarehouseKeeperCandidatesQuery, IReadOnlyList<WarehouseKeeperCandidate>>
{
    private readonly IUserRepository _users;

    public GetWarehouseKeeperCandidatesQueryHandler(
        IUserRepository users)
    {
        _users = users;
    }

    public async Task<IReadOnlyList<WarehouseKeeperCandidate>> Handle(
        GetWarehouseKeeperCandidatesQuery request,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<User> users =
            await _users.GetAllWithRolePermissionsAsync(cancellationToken);

        return users
            .Select(user => new WarehouseKeeperCandidate(
                user.Id,
                user.DisplayName,
                user.IsActive,
                CanWriteInventory(user)))
            .ToList();
    }

    private static bool CanWriteInventory(User user) =>
        user.IsAdmin ||
        user.Role?.Permissions.Any(permission =>
            permission.Feature == PermissionFeature.Inventory &&
            (permission.Level == PermissionLevel.Write ||
             permission.Level == PermissionLevel.Approve)) == true;
}
