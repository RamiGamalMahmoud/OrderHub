using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class UpdateCityCommandHandler(
    ICityRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateCityCommand, Result>
{
    public async Task<Result> Handle(UpdateCityCommand request, CancellationToken cancellationToken)
    {
        City city = await repository.FindAsync(
            request.CityUpdateInput.Id,
            cancellationToken);

        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        string name = request.CityUpdateInput.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
        {
            return Result.Failure("اسم المدينة يجب أن يتكون من 2 إلى 100 حرف.");
        }

        if (await repository.NameExistsAsync(name, city.Id, cancellationToken))
        {
            return Result.Failure("هذه المدينة موجودة بالفعل.");
        }

        city.Rename(name);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ التعديلات. يرجى المحاولة مرة أخرى.");
        }
    }
}
