using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class CreateDeliverymanCommandHandler(IDeliverymanRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<CreateDeliverymanCommand, Result>
{
    public async Task<Result> Handle(CreateDeliverymanCommand request, CancellationToken cancellationToken)
    {

        City city = await cities.FindAsync(request.Deliveryman.CityId, cancellationToken);

        Deliveryman deliveryman = new Deliveryman(
            request.Deliveryman.Name,
            city,
            request.Deliveryman.PhoneNumber);

        repository.Add(deliveryman);
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
