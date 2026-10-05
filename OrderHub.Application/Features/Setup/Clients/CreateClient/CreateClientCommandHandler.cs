using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class CreateClientCommandHandler(IClientRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<CreateClientCommand, Result>
{
    public async Task<Result> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {

        City city = await cities.FindAsync(request.Client.CityId, cancellationToken);
        Client client = new Client(
            request.Client.Name,
            request.Client.Street,
            city,
            request.Client.PhoneNumber,
            request.Client.CountryCode);

        client.Location = request.Client.Location;

        repository.Add(client);

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
