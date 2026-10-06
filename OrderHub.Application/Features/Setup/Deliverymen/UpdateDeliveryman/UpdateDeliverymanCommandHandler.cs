using MediatR;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;
using OrderHub.Application.Features.Setup.Cities.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class UpdateDeliverymanCommandHandler(
    IDeliverymanRepository repository,
    IUnitOfWork unitOfWork,
    ICityRepository cities) : IRequestHandler<UpdateDeliverymanCommand, Result>
{
    public async Task<Result> Handle(UpdateDeliverymanCommand request, CancellationToken cancellationToken)
    {
        var input = request.Deliveryman;
        var deliveryman = await repository.FindAsync(request.Id, cancellationToken);

        if (deliveryman is null)
        {
            return Result.Failure("المندوب غير موجود.");
        }

        Result validation = DeliverymanInputValidation.Validate(input.Name, input.PhoneNumber);

        if (!validation.IsSuccess)
        {
            return validation;
        }

        var city = await cities.FindAsync(input.CityId, cancellationToken);

        if (city is null)
        {
            return Result.Failure("المدينة غير موجودة.");
        }

        deliveryman.Rename(input.Name);
        deliveryman.ChangePhoneNumber(input.PhoneNumber);
        deliveryman.ChangeCity(city);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ المندوب. يرجى المحاولة مرة أخرى.");
        }
    }
}
