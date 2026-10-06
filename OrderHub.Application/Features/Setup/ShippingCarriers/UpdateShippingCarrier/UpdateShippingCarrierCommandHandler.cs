using MediatR;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class UpdateShippingCarrierCommandHandler(
    IShippingCarrierRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<UpdateShippingCarrierCommand, Result>
{
    public async Task<Result> Handle(UpdateShippingCarrierCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
        var carrier = await repository.GetForUpdateAsync(input.Id, cancellationToken);

        if (carrier is null)
        {
            return Result.Failure("شركة الشحن غير موجودة.");
        }

        Result validation = ShippingCarrierInputValidation.Validate(
            input.Name,
            input.ShippingCost,
            input.Street,
            input.PhoneNumber,
            input.CountryCode);

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

        carrier.Rename(input.Name);
        carrier.ChangeShippingCost(input.ShippingCost);

        if (carrier.Phone is null)
        {
            carrier.UpdatePhone(phone);
        }
        else
        {
            carrier.Phone.ChangeNumber(input.PhoneNumber.Trim(), input.CountryCode.Trim());
        }

        if (carrier.Address is null)
        {
            carrier.ChangeAddress(address);
        }
        else
        {
            carrier.ChangeAddress(city, input.Street.Trim());
        }

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ شركة الشحن. يرجى المحاولة مرة أخرى.");
        }
    }
}
