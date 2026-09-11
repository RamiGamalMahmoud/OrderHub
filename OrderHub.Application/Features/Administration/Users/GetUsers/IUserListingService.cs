using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.GetUsers;
public interface IUserListingService
{
    Task<IReadOnlyList<UserListItem>> GetAsync(
        CancellationToken cancellationToken = default);
}