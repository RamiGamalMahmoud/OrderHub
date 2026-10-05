using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Roles.Create;

internal sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var input = request.Request;
        bool nameExists = await _roleRepository.NameExistsAsync(
            input.Name?.Trim(),
            cancellationToken: cancellationToken);

        if (nameExists)
            return Result.Failure("اسم الدور مستخدم بالفعل.");

        var role = new Role(input.Name);

        foreach (var permission in input.Permissions)
            role.SetPermission(permission.Feature, permission.Level);

        _roleRepository.Add(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
