using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Services;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;
internal class InitialAdminService : IInitialAdminService
{
    private readonly AppDbContextFactory _contextFactory;

    public InitialAdminService(AppDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> HasAdminAsync()
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();
        return await dbContext.Users.AnyAsync(x => x.IsAdmin);
    }
}
