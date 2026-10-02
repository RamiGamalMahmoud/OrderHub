using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Context;

// Compatibility for the single-user desktop host. Web overrides this registration.
internal sealed class DesktopCurrentUser(IApplicationContext context) : ICurrentUser
{
    public Task<int?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<int?>(context.User?.Id);
}
