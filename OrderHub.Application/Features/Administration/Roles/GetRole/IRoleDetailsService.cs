using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Roles.GetRole;

public interface IRoleDetailsService
{
    /// <summary>Returns role edit data, or null when the role does not exist.</summary>
    Task<GetRoleResponse> GetRoleDetailsAsync(
        int roleId,
        CancellationToken cancellationToken = default);
}
