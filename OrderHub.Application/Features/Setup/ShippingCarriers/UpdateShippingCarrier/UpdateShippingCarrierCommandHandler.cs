using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class UpdateShippingCarrierCommandHandler(IShippingCarrierRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<UpdateShippingCarrierCommand, Result>
{
    public async Task<Result> Handle(UpdateShippingCarrierCommand request, CancellationToken cancellationToken)
    {

        ShippingCarrier shippingCarrier = await repository.GetForUpdateAsync(request.Dto.Id, cancellationToken);

        if (shippingCarrier is null)
            return Result.Failure("Selected Shipping Carrier not found.");

        City selectedCity = await cities.FindAsync(request.Dto.CityId, cancellationToken);
        if (selectedCity is null)
            return Result.Failure("Selected City not found.");

        shippingCarrier.Rename(request.Dto.Name);
        shippingCarrier.Phone.ChangeNumber(request.Dto.PhoneNumber, request.Dto.CountryCode);
        shippingCarrier.Address.ChangeCity(selectedCity);
        shippingCarrier.Address.ChangeStreet(request.Dto.Street);
        shippingCarrier.ChangeShippingCost(request.Dto.ShippingCost);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure($"Failed to update Shipping Carrier {request.Dto.Name}.");
        }
    }
}
