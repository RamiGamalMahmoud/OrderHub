using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Features.Administration.Users.GetUsers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Administration.Users;
internal sealed class UserReadService :
    IUserListingService
{
    private readonly AppDbContextFactory _dbContextFactory;

    public UserReadService(AppDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<UserListItem>> GetAsync(CancellationToken cancellationToken = default)
    {
        AppDbContext dbContext = _dbContextFactory.CreateDbContext();

        return await dbContext.Users
            .AsNoTracking()
            .Select(user => new UserListItem(
                user.Id,
                user.UserName,
                user.DisplayName,
                user.IsActive,
                user.IsAdmin,
                user.Permissions.Select(premission => new UserListItemPermission(
                     premission.Id,
                     premission.Feature,
                     premission.CanRead,
                     premission.CanWrite))
                .ToList(),
                user.CreatedAt))
            .ToListAsync(cancellationToken);
        ;
    }
}
