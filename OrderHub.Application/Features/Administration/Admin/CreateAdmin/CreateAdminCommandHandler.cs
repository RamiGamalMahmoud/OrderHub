using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Admin.CreateAdmin;
internal class CreateAdminCommandHandler : IRequestHandler<CreateAdminCommand, Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHashShervice _hashShervice;

    public CreateAdminCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IHashShervice hashShervice)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _hashShervice = hashShervice;
    }

    public async Task<Result> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        bool hasAdmin = await _userRepository.AnyAdminAsync(cancellationToken);

        if (hasAdmin)
            return Result.Failure("تم إنشاء حساب مدير من قبل.");

        string passwordHash = _hashShervice.Hash(request.Password);

        User admin = User.Create(
            request.UserName,
            passwordHash,
            request.DisplayName,
            isActive: true,
            isAdmin: true);

        _userRepository.Add(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
