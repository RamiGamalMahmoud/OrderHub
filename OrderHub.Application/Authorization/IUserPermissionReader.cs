using OrderHub.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Authorization;

public interface IUserPermissionReader
{
    Task<PermissionLevel> GetPermissionLevelAsync(int userId, PermissionFeature feature, CancellationToken cancellationToken = default);
}
