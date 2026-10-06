using OrderHub.Domain.Enums;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Authorization;

public interface IAuthorizationService
{
    Task<bool> IsAllowedAsync(PermissionFeature feature, PermissionLevel requiredLevel, CancellationToken cancellationToken = default);
}
