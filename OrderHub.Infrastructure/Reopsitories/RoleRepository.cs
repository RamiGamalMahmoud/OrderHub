using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Repositories;

internal sealed class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _dbContext;

    public RoleRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Role> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<Role>()
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public void Add(Role role)
    {
        _dbContext.Set<Role>().Add(role);
    }

    public void Remove(Role role)
    {
        _dbContext.Set<Role>().Remove(role);
    }
}