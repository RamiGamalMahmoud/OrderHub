using MediatR;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class CreateShippingCarrierCommandHandler(
    IShippingCarrierRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<CreateShippingCarrierCommand, Result>
{
    public async Task<Result> Handle(CreateShippingCarrierCommand request, CancellationToken cancellationToken)
    {
        var input = request.Input;
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

        var carrier = new ShippingCarrier(input.Name, input.ShippingCost, phone, address);
        repository.Add(carrier);

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
