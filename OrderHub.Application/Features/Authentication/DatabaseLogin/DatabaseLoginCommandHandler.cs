using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Authentication.DatabaseLogin;
internal class DatabaseLoginCommandHandler : IRequestHandler<DatabaseLoginCommand, Result<UserSession>>
{
    private readonly IUserRepository _userRepository;
    private readonly IHashShervice _hashShervice;

    public DatabaseLoginCommandHandler(
        IUserRepository userRepository,
        IHashShervice hashShervice)
    {
        _userRepository = userRepository;
        _hashShervice = hashShervice;
    }

    public async Task<Result<UserSession>> Handle(DatabaseLoginCommand request, CancellationToken cancellationToken)
    {
        User user = await _userRepository.GetByUsernameAsync(request.UserName);

        if (user is null)
            return Result<UserSession>.Failure("بيانات تسجيل الدخول غير صحيحة.");

        if (!_hashShervice.Verify(request.Password, user.PasswordHash))
            return Result<UserSession>.Failure("بيانات تسجيل الدخول غير صحيحة.");

        UserSession userSession = new UserSession(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.IsAdmin,
            user.Permissions.Select(permission => new UserPermissionSession(
                permission.Feature,
                permission.CanRead,
                permission.CanWrite))
            .ToList());

        return Result<UserSession>.Success(userSession);
    }
}
