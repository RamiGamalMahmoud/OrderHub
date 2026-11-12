using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;
internal sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public Task<User> GetWithRolePermissionsAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Include(user => user.Role)
            .ThenInclude(role => role.Permissions)
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public Task<User> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(
                x => EF.Functions.ILike(x.UserName, username),
                cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(
        string username,
        int? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(
            x =>
                EF.Functions.ILike(x.UserName, username) &&
                (!excludeUserId.HasValue || x.Id != excludeUserId.Value),
            cancellationToken);
    }

    public Task<bool> AnyAdminAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(
            x => x.IsAdmin,
            cancellationToken);
    }

    public Task<bool> AnyOtherActiveAdminAsync(
    int userId,
    CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(
            x =>
                x.Id != userId &&
                x.IsAdmin &&
                x.IsActive,
            cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetAllWithRolePermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .ThenInclude(role => role.Permissions)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }
}
