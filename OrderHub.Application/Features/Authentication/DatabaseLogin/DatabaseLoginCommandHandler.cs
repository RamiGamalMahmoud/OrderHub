using MediatR;
using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Application.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Authentication.DatabaseLogin;
internal class DatabaseLoginCommandHandler : IRequestHandler<DatabaseLoginCommand, Result<UserInfoDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IHashShervice _hashShervice;
    private readonly IApplicationContext _applicationContext;
    private readonly IUserSessionStorage _userSessionStorage;

    public DatabaseLoginCommandHandler(
        IUserRepository userRepository,
        IHashShervice hashShervice,
        IApplicationContext applicationContext,
        IUserSessionStorage userSessionStorage)
    {
        _userRepository = userRepository;
        _hashShervice = hashShervice;
        _applicationContext = applicationContext;
        _userSessionStorage = userSessionStorage;
    }

    public async Task<Result<UserInfoDto>> Handle(DatabaseLoginCommand request, CancellationToken cancellationToken)
    {
        User user = await _userRepository.GetByUsername(request.UserName);

        if (user is null)
            return Result<UserInfoDto>.Failure("بيانات تسجيل الدخول غير صحيحة.");

        if (!_hashShervice.Verify(request.Password, user.PasswordHash))
            return Result<UserInfoDto>.Failure("بيانات تسجيل الدخول غير صحيحة.");

        UserSession userSession = new UserSession(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Roles);

        _applicationContext.SetUser(userSession);

        if (request.RememberMe)
            _userSessionStorage.Save(userSession);
        else
            _userSessionStorage.Clear();

        UserInfoDto userInfo = new UserInfoDto(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.Roles);

        return Result<UserInfoDto>.Success(userInfo);
    }
}
