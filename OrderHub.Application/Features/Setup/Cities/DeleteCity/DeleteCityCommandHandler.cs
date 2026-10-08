using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Cities;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class DeleteCityCommandHandler(ICityRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteCityCommand, Result>
{
    public async Task<Result> Handle(DeleteCityCommand request, CancellationToken cancellationToken)
    {


        City city = await repository.FindAsync(request.Id, cancellationToken);
        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        if (await repository.IsUsedByAddressesAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المدينة لأنها مرتبطة بعناوين.");
        }

        if (await repository.IsUsedByDeliverymenAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المدينة لأنها مرتبطة بمناديب.");
        }

        repository.Remove(city);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure($"تعذر حذف المدينة ({city.Name.Value}).");
        }
    }
}
