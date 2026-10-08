using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;
public interface IRoleRepository
{
    /// <summary>Loads the role and its complete permission state for domain mutation.</summary>
    Task<Role> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Checks the normalized role name, optionally excluding the role being updated.</summary>
    Task<bool> NameExistsAsync(
        string name,
        int? excludeRoleId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Checks all user assignments, including inactive users and administrators.</summary>
    Task<bool> IsAssignedToUsersAsync(
        int roleId,
        CancellationToken cancellationToken = default);

    void Add(Role role);
    void Remove(Role role);
}
