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

internal sealed class CreateSupplierCommandHandler(
    ISupplierRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<CreateSupplierCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
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

        var products = await repository.GetProductsForUpdateAsync(productIds, cancellationToken);

        if (products.Count != productIds.Length)
        {
            return Result<int>.Failure("أحد المنتجات المحددة لم يعد موجودًا.");
        }

        var phone = Phone.Create(input.PhoneNumber.Trim(), input.CountryCode.Trim()).Value;
        var address = Address.Create(input.Street.Trim(), city).Value;

        var supplier = new Supplier(input.Name, input.OpenAt, input.CloseAt, address, phone);
        repository.Add(supplier);

        foreach (var product in products)
        {
            product.AddSupplier(supplier);
        }

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
}
