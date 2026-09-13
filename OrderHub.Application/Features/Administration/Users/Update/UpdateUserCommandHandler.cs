using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.Update;

internal class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        User existingUser = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (existingUser is null)
            return Result.Failure("المستخدم غير موجود.");

        existingUser.ChangeUserName(request.Request.UserName);
        existingUser.ChangeDisplayName(request.Request.DisplayName);

        Result statusResult = await UpdateStatusAsync(
            existingUser,
            request,
            cancellationToken);

        if (!statusResult.IsSuccess)
            return statusResult;

        SyncPermissions(existingUser, request);

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
                await _userRepository.AnyOtherActiveAdminAsync(user.Id, cancellationToken);

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

    private static void SyncPermissions(
        User user,
        UpdateUserCommand request)
    {
        var enabledPermissions = request.Request.Permissions
            .Where(x => x.CanRead || x.CanWrite)
            .ToList();

        var enabledFeatures = enabledPermissions
            .Select(x => x.Feature)
            .ToHashSet();

        var featuresToRemove = user.Permissions
            .Where(x => !enabledFeatures.Contains(x.Feature))
            .Select(x => x.Feature)
            .ToList();

        foreach (var feature in featuresToRemove)
        {
            user.RemovePermission(feature);
        }

        foreach (var permission in enabledPermissions)
        {
            user.SetPermission(
                permission.Feature,
                permission.CanRead,
                permission.CanWrite);
        }
    }
}