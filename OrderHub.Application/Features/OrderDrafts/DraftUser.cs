using System;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Context;

namespace OrderHub.Application.Features.OrderDrafts;

internal static class DraftUser
{
    public static async Task<int> RequireAsync(ICurrentUser currentUser, CancellationToken cancellationToken)
    {
        var id = await currentUser.GetUserIdAsync(cancellationToken);
        return id is > 0 ? id.Value
            : throw new UnauthorizedAccessException("Authentication is required for draft operations.");
    }
}
