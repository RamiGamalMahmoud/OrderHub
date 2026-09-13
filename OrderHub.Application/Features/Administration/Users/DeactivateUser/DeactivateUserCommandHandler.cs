using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.DeactivateUser;
internal sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        User user = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
            return Result.Failure("المستخدم غير موجود.");

        if (!user.IsActive)
            return Result.Success();

        if (user.IsAdmin)
        {
            bool hasOtherActiveAdmin =
                await _userRepository.AnyOtherActiveAdminAsync(
                    request.UserId,
                    cancellationToken);

            if (!hasOtherActiveAdmin)
            {
                return Result.Failure(
                    "لا يمكن تعطيل الحساب لأنه المدير النشط الوحيد في النظام.");
            }
        }

        user.SetActive(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}