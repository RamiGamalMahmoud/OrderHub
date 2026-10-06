using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Clients.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Clients;

internal sealed class UpdateClientCommandHandler(
    IClientRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<UpdateClientCommand, Result>
{
    public async Task<Result> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
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

        var client = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (client is null)
        {
            return Result.Failure("العميل غير موجود.");
        }

        await repository.PrepareContactUpdateAsync(client, input, city, cancellationToken);
        client.UpdateName(input.Name);
        client.UpdatePhone(input.CountryCode, input.PhoneNumber);
        client.UpdateAddress(city, input.Street);
        client.UpdateLocation(input.Location);

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
