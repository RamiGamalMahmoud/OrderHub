using MediatR;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class CreateSupplierCommandHandler(
    ISupplierRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<CreateSupplierCommand, Result>
{
    public async Task<Result> Handle(CreateSupplierCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        Result validation = SupplierInputValidation.Validate(
            input.Name,
            input.Street,
            input.PhoneNumber,
            input.CountryCode,
            input.OpenAt,
            input.CloseAt);

        if (!validation.IsSuccess)
        {
            return validation;
        }

        var city = await cities.FindAsync(input.CityId, cancellationToken);

        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        var phone = Phone.Create(input.PhoneNumber.Trim(), input.CountryCode.Trim()).Value;
        var address = Address.Create(input.Street.Trim(), city).Value;

        var supplier = new Supplier(input.Name, input.OpenAt, input.CloseAt, address, phone);
        repository.Add(supplier);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ المورد. يرجى المحاولة مرة أخرى.");
        }
    }
}
