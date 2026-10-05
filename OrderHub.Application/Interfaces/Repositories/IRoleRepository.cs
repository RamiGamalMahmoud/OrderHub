using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;
public interface IRoleRepository
{
    Task<Role> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    void Add(Role role);
    void Remove(Role role);
}
