using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Context;

public interface ICurrentUser
{
    Task<int?> GetUserIdAsync(CancellationToken cancellationToken = default);
}
