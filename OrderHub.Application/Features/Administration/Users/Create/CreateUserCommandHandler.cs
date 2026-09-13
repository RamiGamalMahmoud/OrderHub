using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.Create;

internal sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHashShervice _hashService;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IHashShervice hashService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _hashService = hashService;
    }

    public async Task<Result> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        bool usernameExists = await _userRepository.UsernameExistsAsync(
            request.Request.UserName,
            cancellationToken: cancellationToken);

        if (usernameExists)
            return Result.Failure("اسم المستخدم مستخدم بالفعل.");

        string passwordHash = _hashService.Hash(
            request.Request.Password);

        var user = User.Create(
            request.Request.UserName,
            passwordHash,
            request.Request.DisplayName);

        ApplyStatus(user, request);
        ApplyPermissions(user, request);

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static void ApplyStatus(
        User user,
        CreateUserCommand request)
    {
        user.SetActive(request.Request.IsActive);
        user.SetAdmin(request.Request.IsAdmin);
    }

    private static void ApplyPermissions(
        User user,
        CreateUserCommand request)
    {
        foreach (var permission in request.Request.Permissions)
        {
            if (!permission.CanRead && !permission.CanWrite)
                continue;

            user.SetPermission(
                permission.Feature,
                permission.CanRead,
                permission.CanWrite);
        }
    }
}