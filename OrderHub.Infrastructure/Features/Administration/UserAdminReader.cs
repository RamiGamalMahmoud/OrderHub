using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Authorization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Features.Administration;

internal sealed class UserAdminReader(AppDbContextFactory factory) : IUserAdminReader
{
    public async Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default)
    {
        using var context = factory.CreateDbContext();

        return await context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.IsAdmin)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
