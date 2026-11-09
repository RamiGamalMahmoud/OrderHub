using OrderHub.Application.Context;
using OrderHub.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Authorization;

internal sealed class AuthorizationService(ICurrentUser currentUser, IUserPermissionReader reader) : IAuthorizationService
{
    public async Task<bool> IsAllowedAsync(PermissionFeature feature, PermissionLevel requiredLevel, CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.Id is not > 0)
            return false;

        if (!Enum.IsDefined(feature) ||
            requiredLevel is not (PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve))
            return false;

        if (currentUser.IsAdmin)
            return true;

        var actual = await reader.GetPermissionLevelAsync(currentUser.Id.Value, feature, cancellationToken);
        return requiredLevel switch
        {
            PermissionLevel.Read => actual is PermissionLevel.Read or PermissionLevel.Write or PermissionLevel.Approve,
            PermissionLevel.Write => actual is PermissionLevel.Write or PermissionLevel.Approve,
            PermissionLevel.Approve => actual is PermissionLevel.Approve,
            _ => false
        };
    }

    public Task<bool> IsHigherManagerAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(
            currentUser.IsAuthenticated &&
            currentUser.Id is > 0 &&
            currentUser.IsAdmin);
}
