using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.GetUser;

internal sealed class GetUserQueryHandler
    : IRequestHandler<GetUserQuery, GetUserResponse>
{
    private readonly IUserDetailsService _userDetailsService;

    public GetUserQueryHandler(IUserDetailsService userDetailsService)
    {
        _userDetailsService = userDetailsService;
    }

    public Task<GetUserResponse> Handle(
        GetUserQuery request,
        CancellationToken cancellationToken)
    {
        return _userDetailsService.GetUserDetailsAsync(
            request.UserId,
            cancellationToken);
    }
}