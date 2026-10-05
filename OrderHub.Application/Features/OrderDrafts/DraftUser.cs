using System;
using OrderHub.Application.Context;

namespace OrderHub.Application.Features.OrderDrafts;

internal static class DraftUser
{
    public static int Require(ICurrentUser currentUser)
    {
        var id = currentUser.Id;
        return id is > 0 ? id.Value
            : throw new UnauthorizedAccessException("Authentication is required for draft operations.");
    }
}
