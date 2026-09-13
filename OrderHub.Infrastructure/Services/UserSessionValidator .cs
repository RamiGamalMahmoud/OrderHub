using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;
internal sealed class UserSessionValidator : IUserSessionValidator
{
    private readonly AppDbContextFactory _dbContextFactory;

    public UserSessionValidator(AppDbContextFactory appDbContextFactory)
    {
        _dbContextFactory = appDbContextFactory;
    }

    public async Task<bool> IsValidAsync(UserSession session)
    {
        AppDbContext dbContext = _dbContextFactory.CreateDbContext();
        return await dbContext.Users
            .AnyAsync(x => x.Id == session.Id && x.IsActive);
    }
}