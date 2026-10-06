using MediatR;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Authorization;

internal sealed class AuthorizationBehavior<TRequest, TResponse>(IAuthorizationService authorization)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requirement = request.GetType().GetCustomAttribute<RequirePermissionAttribute>();
        if (requirement is not null &&
            !await authorization.IsAllowedAsync(requirement.Feature, requirement.RequiredLevel, cancellationToken))
            throw new AuthorizationDeniedException();

        return await next();
    }
}
