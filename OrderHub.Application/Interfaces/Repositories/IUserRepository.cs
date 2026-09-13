using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Repositories;
public interface IUserRepository
{
    Task<User> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<User> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<bool> UsernameExistsAsync(
        string username,
        int? excludeUserId = null,
        CancellationToken cancellationToken = default);

    Task<bool> AnyAdminAsync(
        CancellationToken cancellationToken = default);

    Task<bool> AnyOtherActiveAdminAsync(
        int userId,
        CancellationToken cancellationToken = default);

    void Add(User user);
}