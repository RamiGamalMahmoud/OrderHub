using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Authorization;

public interface IUserAdminReader
{
    Task<bool> IsAdminAsync(int userId, CancellationToken cancellationToken = default);
}
