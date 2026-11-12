using MediatR;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Interfaces.Repositories;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Warehouses;

internal sealed class UpdateWarehouseCommandHandler(
    IWarehouseRepository warehouses,
    IUserRepository users,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateWarehouseCommand, Result>
{
    public async Task<Result> Handle(
        UpdateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        Warehouse warehouse = await warehouses.GetByIdAsync(request.Input.Id, cancellationToken);

        if (warehouse is null)
            return Result.Failure("المخزن غير موجود.");

        if (!WarehouseInputValidation.TryNormalizeName(request.Input.Name, out string name))
            return Result.Failure("اسم المخزن يجب أن يتكون من 2 إلى 100 حرف.");

        if (await warehouses.NameExistsAsync(name, warehouse.Id, cancellationToken))
            return Result.Failure("اسم المخزن مستخدم بالفعل.");

        var (keeper, keeperError) = await WarehouseInputValidation.ResolveKeeperAsync(
            users,
            warehouses,
            request.Input.KeeperUserId,
            warehouse.Id,
            cancellationToken);

        if (keeperError is not null)
            return Result.Failure(keeperError);

        bool deactivating = warehouse.IsActive && !request.Input.IsActive;

        if (deactivating && await warehouses.HasPositiveStockAsync(warehouse.Id, cancellationToken))
            return Result.Failure("لا يمكن إيقاف المخزن لوجود أرصدة موجبة أو كميات تالفة فيه.");

        // Pending issue orders and open counts join this guard once those tables land (T06).
        warehouse.Rename(name);
        warehouse.AssignKeeper(keeper.Id);

        if (request.Input.IsActive)
            warehouse.Activate();
        else
            warehouse.Deactivate();

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ تعديلات المخزن. يرجى المحاولة مرة أخرى.");
        }
    }
}
