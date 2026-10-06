using MediatR;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class UpdateSupplierCommandHandler(
    ISupplierRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<UpdateSupplierCommand, Result>
{
    public async Task<Result> Handle(UpdateSupplierCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var supplier = await repository.GetForUpdateAsync(input.Id, cancellationToken);

        if (supplier is null)
        {
            return Result.Failure("المورد غير موجود.");
        }

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

        supplier.UpdateName(input.Name);
        supplier.UpdateBuisnessHours(input.OpenAt, input.CloseAt);

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
