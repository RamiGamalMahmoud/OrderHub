using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.GetUsers;

internal sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, IReadOnlyList<UserListItem>>
{
    private readonly IUserListingService _reader;

    public GetUsersQueryHandler(IUserListingService reader)
    {
        _reader = reader;
    }

    public Task<IReadOnlyList<UserListItem>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        return _reader.GetAsync(cancellationToken);
    }
}