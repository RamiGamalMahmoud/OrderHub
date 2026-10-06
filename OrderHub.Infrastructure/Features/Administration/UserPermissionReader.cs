using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using OrderHub.Domain.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Administration;

internal sealed class UserPermissionReader(AppDbContextFactory factory) : IUserPermissionReader
{
    public async Task<PermissionLevel> GetPermissionLevelAsync(int userId, PermissionFeature feature, CancellationToken cancellationToken = default)
    {
        using var context = factory.CreateDbContext();
        var level = await context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .SelectMany(user => user.Role.Permissions)
            .Where(permission => permission.Feature == feature)
            .Select(permission => (PermissionLevel?)permission.Level)
            .FirstOrDefaultAsync(cancellationToken);
        return level ?? PermissionLevel.None;
    }
}
