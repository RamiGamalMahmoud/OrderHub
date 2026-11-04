using MediatR;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class UpdateSupplierCommandHandler(
    ISupplierRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<UpdateSupplierCommand, Result<int>>
{
    public async Task<Result<int>> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var supplier = await repository.GetForUpdateAsync(input.Id, cancellationToken);

        if (supplier is null)
        {
            return Result<int>.Failure("المورد غير موجود.");
        }

        var productIds = input.SelectedProductsIds?.ToArray() ?? [];

        Result validation = SupplierInputValidation.Validate(
            input.Name,
            input.Street,
            input.PhoneNumber,
            input.CountryCode,
            input.OpenAt,
            input.CloseAt,
            productIds);

        if (!validation.IsSuccess)
        {
            return Result<int>.Failure(validation.ErrorMessage);
        }

        var city = await cities.FindAsync(input.CityId, cancellationToken);

        if (city is null)
        {
            return Result<int>.Failure("المدينة غير موجودة.");
        }

        var selectedProducts = await repository.GetProductsForUpdateAsync(productIds, cancellationToken);

        if (selectedProducts.Count != productIds.Length)
        {
            return Result<int>.Failure("أحد المنتجات المحددة لم يعد موجودًا.");
        }

        var connectedProducts = await repository.GetConnectedProductsForUpdateAsync(input.Id, cancellationToken);

        var phone = Phone.Create(input.PhoneNumber.Trim(), input.CountryCode.Trim()).Value;
        var address = Address.Create(input.Street.Trim(), city).Value;

        supplier.UpdateName(input.Name);
        supplier.UpdateBusinessHours(input.OpenAt, input.CloseAt);

        if (supplier.Phone is null)
        {
            supplier.UpdatePhone(phone);
        }
        else
        {
            supplier.Phone.ChangeNumber(input.PhoneNumber.Trim(), input.CountryCode.Trim());
        }

        if (supplier.Address is null)
        {
            supplier.UpdateAddress(address);
        }
        else
        {
            supplier.Address.ChangeCity(city);
            supplier.Address.ChangeStreet(input.Street.Trim());
        }

        ReconcileProducts(supplier, connectedProducts, selectedProducts);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(supplier.Id);
        }
        catch (PersistenceException)
        {
            return Result<int>.Failure("تعذر حفظ المورد. يرجى المحاولة مرة أخرى.");
        }
    }

    private static void ReconcileProducts(
        Supplier supplier,
        IReadOnlyList<Product> connectedProducts,
        IReadOnlyList<Product> selectedProducts)
    {
        var selectedIds = selectedProducts.Select(product => product.Id).ToHashSet();
        var connectedIds = connectedProducts.Select(product => product.Id).ToHashSet();

        // إزالة الربط عن المنتجات غير المحددة.
        foreach (var product in connectedProducts.Where(product => !selectedIds.Contains(product.Id)).ToArray())
        {
            product.RemoveSupplier(supplier.Id);
        }

        // إضافة الربط للمنتجات الجديدة.
        foreach (var product in selectedProducts.Where(product => !connectedIds.Contains(product.Id)).ToArray())
        {
            product.AddSupplier(supplier);
        }
    }
}
