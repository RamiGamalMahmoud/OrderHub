using OrderHub.Application.Authorization;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Enums;

namespace OrderHub.Web.Services;

public sealed class PermissionAccess(IRequestExecutor executor)
{
    public Task<bool> IsAllowedAsync(PermissionFeature feature, PermissionLevel level, CancellationToken cancellationToken = default) =>
        executor.ExecuteAsync(new CheckPermissionQuery(feature, level), cancellationToken);
}
