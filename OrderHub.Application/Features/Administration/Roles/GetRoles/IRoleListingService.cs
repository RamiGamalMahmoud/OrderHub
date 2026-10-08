using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Roles.GetRoles;

public interface IRoleListingService
{
    Task<IReadOnlyList<RoleListItem>> GetAsync(CancellationToken cancellationToken = default);
}
