using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class CreateClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<CreateClientCommand, Result>
{
    public async Task<Result> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var validation = ClientInputValidation.Validate(request.Client);
        if (!validation.IsSuccess)
        {
            return validation;
        }

        var input = request.Client;
        var city = await cities.FindAsync(input.CityId, cancellationToken);
        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        var client = new Client(input.Name, input.Street, city, input.PhoneNumber, input.CountryCode);
        client.UpdateLocation(input.Location);
        repository.Add(client);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ العميل. يرجى المحاولة مرة أخرى.");
        }
    }
}
