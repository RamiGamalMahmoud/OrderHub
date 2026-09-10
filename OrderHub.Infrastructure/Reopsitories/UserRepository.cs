using Microsoft.EntityFrameworkCore;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;
internal class UserRepository : IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(User user)
    {
        _dbContext.Users.Add(user);
    }

    public async Task<bool> AnyAdminAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users.AnyAsync(x => x.IsAdmin, cancellationToken);
    }

    public async Task<User> GetByIdAsync(int id)
    {
        return await _dbContext.Users
            .Where(x => x.Id == id)
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync();
    }

    public async Task<User> GetByUsername(string username)
    {
        return await _dbContext.Users
            .Where(x => x.UserName == username)
            .Include(x => x.Permissions)
            .FirstOrDefaultAsync();
    }
}
