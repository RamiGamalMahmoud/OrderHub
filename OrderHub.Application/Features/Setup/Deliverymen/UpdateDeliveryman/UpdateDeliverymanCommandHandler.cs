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

internal sealed class UpdateDeliverymanCommandHandler(IDeliverymanRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<UpdateDeliverymanCommand, Result>
{
    public async Task<Result> Handle(UpdateDeliverymanCommand request, CancellationToken cancellationToken)
    {

        Deliveryman deliveryman = await repository.FindAsync(request.Id, cancellationToken);

        City city = await cities.FindAsync(request.Deliveryman.CityId, cancellationToken);

        if (deliveryman is null)
        {
            return Result.Failure();
        }

        deliveryman.Rename(request.Deliveryman.Name);
        deliveryman.PhoneNumber = request.Deliveryman.PhoneNumber;
        deliveryman.ChangeCity(city);

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
