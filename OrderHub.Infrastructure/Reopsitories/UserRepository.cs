using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Reopsitories;
internal class UserRepository : IUserRepository
{
    public Task<User> GetByIdAsync(int id)
    {
        return Task.FromResult(User.Create(
            "username",
            "tQ9XqipfrJ6g8c3zxvIjZKC+oZ75DTLt87jngZ5ef+YotCJN1OHAiNx7Wd6xOnqr",
            "User Display Name",
            true));
    }

    public Task<User> GetByUsername(string username)
    {
        return Task.FromResult(User.Create(
            username,
            "tQ9XqipfrJ6g8c3zxvIjZKC+oZ75DTLt87jngZ5ef+YotCJN1OHAiNx7Wd6xOnqr",
            "User Display Name",
            true));
    }
}
