using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.Create;

internal sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHashShervice _hashService;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork,
        IHashShervice hashService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
        _hashService = hashService;
    }

    public async Task<Result> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Request;

        bool usernameExists = await _userRepository.UsernameExistsAsync(
            input.UserName,
            cancellationToken: cancellationToken);

        if (usernameExists)
            return Result.Failure("اسم المستخدم مستخدم بالفعل.");

        Role role = null;

        if (input.RoleId.HasValue)
        {
            role = await _roleRepository.GetByIdAsync(
                input.RoleId.Value,
                cancellationToken);

            if (role is null)
                return Result.Failure("الدور المحدد غير موجود.");
        }

        string passwordHash = _hashService.Hash(input.Password);

        var user = User.Create(
            input.UserName,
            passwordHash,
            input.DisplayName,
            input.IsActive,
            input.IsAdmin);

        if (role is not null)
            user.AssignRole(role);

        _userRepository.Add(user);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}