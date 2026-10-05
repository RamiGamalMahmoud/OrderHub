using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class CreateCityCommandHandler(ICityRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<CreateCityCommand, Result>
{
    public async Task<Result> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {

        City city = new City(request.CityCreateInput.Name);
        repository.Add(city);
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
