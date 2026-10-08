using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.Update;

internal sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        User user = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
            return Result.Failure("المستخدم غير موجود.");

        bool usernameExists = await _userRepository.UsernameExistsAsync(
            request.Request.UserName,
            user.Id,
            cancellationToken);

        if (usernameExists)
            return Result.Failure("اسم المستخدم مستخدم بالفعل.");

        Role role = null;

        if (request.Request.RoleId.HasValue)
        {
            role = await _roleRepository.GetByIdAsync(
                request.Request.RoleId.Value,
                cancellationToken);

            if (role is null)
                return Result.Failure("الدور المحدد غير موجود.");
        }

        Result statusResult = await UpdateStatusAsync(
            user,
            request,
            cancellationToken);

        if (!statusResult.IsSuccess)
            return statusResult;

        user.ChangeUserName(request.Request.UserName);
        user.ChangeDisplayName(request.Request.DisplayName);

        if (role is null)
            user.RemoveRole();
        else
            user.AssignRole(role);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<Result> UpdateStatusAsync(
        User user,
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        bool removingActiveAdmin =
            user.IsAdmin &&
            user.IsActive &&
            (!request.Request.IsAdmin || !request.Request.IsActive);

        if (removingActiveAdmin)
        {
            bool hasOtherActiveAdmin =
                await _userRepository.AnyOtherActiveAdminAsync(
                    user.Id,
                    cancellationToken);

            if (!hasOtherActiveAdmin)
            {
                return Result.Failure(
                    "لا يمكن إلغاء صلاحية المدير أو تعطيل الحساب لأنه المدير النشط الوحيد في النظام.");
            }
        }

        user.SetActive(request.Request.IsActive);
        user.SetAdmin(request.Request.IsAdmin);

        return Result.Success();
    }
}