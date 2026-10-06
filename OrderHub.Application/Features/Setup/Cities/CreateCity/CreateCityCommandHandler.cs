using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Cities.Contracts;

namespace OrderHub.Application.Features.Setup.Cities;

internal sealed class CreateCityCommandHandler(
    ICityRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateCityCommand, Result>
{
    public async Task<Result> Handle(CreateCityCommand request, CancellationToken cancellationToken)
    {
        string name = request.CityCreateInput.Name?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
        {
            return Result.Failure("اسم المدينة يجب أن يتكون من 2 إلى 100 حرف.");
        }

        if (await repository.NameExistsAsync(name, null, cancellationToken))
        {
            return Result.Failure("هذه المدينة موجودة بالفعل.");
        }

        City city = new(name);
        repository.Add(city);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر إضافة المدينة. يرجى المحاولة مرة أخرى.");
        }
    }
}
