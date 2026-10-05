using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Clients;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class UpdateClientCommandHandler(IClientRepository repository, IUnitOfWork unitOfWork, ICityRepository cities) : IRequestHandler<UpdateClientCommand, Result>
{
    public async Task<Result> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {

        var dto = request.Client;

        City newCity = await cities.FindAsync(dto.CityId, cancellationToken);

        Client client = await repository.GetForUpdateAsync(request.Id, cancellationToken);

        client.Location = dto.Location;

        client.UpdateName(dto.Name);
        client.UpdatePhone(dto.CountryCode, dto.PhoneNumber);
        client.UpdateAddress(newCity, dto.Street);

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
