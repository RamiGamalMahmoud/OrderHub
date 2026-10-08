using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.GetUser;

public interface IUserDetailsService
{
    Task<GetUserResponse> GetUserDetailsAsync(
        int userId,
        CancellationToken cancellationToken = default);
}