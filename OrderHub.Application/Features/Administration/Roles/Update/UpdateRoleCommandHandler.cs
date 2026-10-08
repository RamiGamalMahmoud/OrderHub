using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Enums;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Administration.Roles.Update;

internal sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure("الدور غير موجود.");

        var input = request.Request;
        bool nameExists = await _roleRepository.NameExistsAsync(
            input.Name?.Trim(),
            request.RoleId,
            cancellationToken);

        if (nameExists)
            return Result.Failure("اسم الدور مستخدم بالفعل.");

        role.Rename(input.Name);

        // The request is complete state. Remove omitted features through Domain behavior,
        // retaining existing permission objects for features that remain in the role.
        var requestedFeatures = input.Permissions.Select(permission => permission.Feature).ToHashSet();
        var omittedFeatures = role.Permissions.Select(permission => permission.Feature)
            .Where(feature => !requestedFeatures.Contains(feature)).ToArray();

        foreach (var feature in omittedFeatures)
            role.SetPermission(feature, PermissionLevel.None);

        foreach (var permission in input.Permissions)
            role.SetPermission(permission.Feature, permission.Level);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
