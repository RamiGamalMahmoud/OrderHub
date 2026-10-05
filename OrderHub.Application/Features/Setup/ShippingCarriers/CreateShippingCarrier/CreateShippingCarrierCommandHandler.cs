using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class CreateShippingCarrierCommandHandler(IShippingCarrierRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<CreateShippingCarrierCommand, Result>
{
    public async Task<Result> Handle(CreateShippingCarrierCommand request, CancellationToken cancellationToken)
    {


        City city = await cities.FindAsync(request.Dto.CityId, cancellationToken);

        if (city is null)
            return Result.Failure("City not found.");

        Result<Phone> phoneResult = Phone.Create(request.Dto.PhoneNumber, request.Dto.CountryCode);
        if (!phoneResult.IsSuccess)
            return Result.Failure(phoneResult.ErrorMessage);

        Result<Address> addressResult = Address.Create(request.Dto.Street, city);
        if (!addressResult.IsSuccess)
            return Result.Failure(addressResult.ErrorMessage);

        ShippingCarrier shippingCarrier = new ShippingCarrier(
            request.Dto.Name,
            request.Dto.ShippingCost,
            phoneResult.Value,
            addressResult.Value
            );

        repository.Add(shippingCarrier);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure();
        }
    }
}
