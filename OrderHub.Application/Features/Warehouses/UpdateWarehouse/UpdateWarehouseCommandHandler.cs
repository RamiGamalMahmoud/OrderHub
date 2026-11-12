using MediatR;
using OrderHub.Application.Features.Setup.Cities.Contracts;
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
    ICityRepository cities,
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

        var (city, addressError) = await WarehouseInputValidation.ResolveAddressAsync(
            cities,
            request.Input.CityId,
            request.Input.Street,
            cancellationToken);

        if (addressError is not null)
            return Result.Failure(addressError);

        if (!WarehouseInputValidation.TryNormalizePhone(
                request.Input.CountryCode,
                request.Input.PhoneNumber,
                out string countryCode,
                out string phoneNumber,
                out string phoneError))
            return Result.Failure(phoneError);

        bool deactivating = warehouse.IsActive && !request.Input.IsActive;

        if (deactivating && await warehouses.HasPositiveStockAsync(warehouse.Id, cancellationToken))
            return Result.Failure("لا يمكن إيقاف المخزن لوجود أرصدة موجبة أو كميات تالفة فيه.");

        // Pending issue orders and open counts join this guard once those tables land (T06).
        Phone previousPhone = warehouse.Phone;

        warehouse.Rename(name);
        warehouse.AssignKeeper(keeper.Id);
        warehouse.UpdateAddress(city, request.Input.Street.Trim());
        warehouse.UpdatePhone(countryCode, phoneNumber);

        if (previousPhone is not null && warehouse.Phone is null)
            warehouses.RemovePhone(previousPhone);

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
