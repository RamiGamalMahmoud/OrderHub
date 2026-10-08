using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Users.ActivateUser;
internal sealed class ActivateUserCommandHandler
    : IRequestHandler<ActivateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        ActivateUserCommand request,
        CancellationToken cancellationToken)
    {
        User user = await _userRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
            return Result.Failure("المستخدم غير موجود.");

        if (user.IsActive)
            return Result.Success();

        user.SetActive(true);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
