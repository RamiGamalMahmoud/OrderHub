using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses;

internal sealed class CreateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUserRepository users,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateWarehouseCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        CreateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        if (!WarehouseInputValidation.TryNormalizeName(request.Input.Name, out string name))
            return Result<int>.Failure("اسم المخزن يجب أن يتكون من 2 إلى 100 حرف.");

        if (await warehouses.NameExistsAsync(name, cancellationToken: cancellationToken))
            return Result<int>.Failure("اسم المخزن مستخدم بالفعل.");

        var (keeper, keeperError) = await WarehouseInputValidation.ResolveKeeperAsync(
            users,
            warehouses,
            request.Input.KeeperUserId,
            excludingWarehouseId: null,
            cancellationToken);

        if (keeperError is not null)
            return Result<int>.Failure(keeperError);

        var warehouse = new Warehouse(name, keeper.Id);
        warehouses.Add(warehouse);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(warehouse.Id);
        }
        catch (PersistenceException)
        {
            return Result<int>.Failure("تعذر إضافة المخزن. يرجى المحاولة مرة أخرى.");
        }
    }
}
