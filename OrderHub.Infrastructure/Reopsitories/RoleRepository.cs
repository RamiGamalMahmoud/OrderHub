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
        return _dbContext.Roles
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        string name,
        int? excludeRoleId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Roles
            .AnyAsync(
                role =>
                    role.Name == name &&
                    (!excludeRoleId.HasValue || role.Id != excludeRoleId.Value),
                cancellationToken);
    }

    public Task<bool> IsAssignedToUsersAsync(
        int roleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AnyAsync(
                user => user.RoleId == roleId,
                cancellationToken);
    }

    public void Add(Role role)
    {
        _dbContext.Roles.Add(role);
    }

    public void Remove(Role role)
    {
        _dbContext.Roles.Remove(role);
    }
}