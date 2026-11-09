using Microsoft.AspNetCore.Authorization;

namespace OrderHub.Web.Infrastructure.Authorization;

public sealed class HigherManagerAuthorizeAttribute : AuthorizeAttribute
{
    public const string PolicyName = "HigherManager";

    public HigherManagerAuthorizeAttribute() => Policy = PolicyName;
}
