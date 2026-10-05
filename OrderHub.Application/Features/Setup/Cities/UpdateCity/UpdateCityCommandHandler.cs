using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class UpdateCityCommandHandler(ICityRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateCityCommand, Result>
{
    public async Task<Result> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
    {


        City city = await repository.FindAsync(request.CityUpdateInput.Id, cancellationToken);
        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        city.Rename(request.CityUpdateInput.Name);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure($"تعذر تحديث المدينة ({request.CityUpdateInput.Name}).");
        }
    }
}
