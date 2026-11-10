using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.ShippingCarriers;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.ShippingCarriers.Contracts;
using OrderHub.Domain.Enums;

namespace OrderHub.Application.Features.Setup.ShippingCarriers;

internal sealed class DeleteShippingCarrierCommandHandler(
    IShippingCarrierRepository repository,
    IUnitOfWork unitOfWork,
    ITripUsageReader tripUsageReader) : IRequestHandler<DeleteShippingCarrierCommand, Result>
{
    public async Task<Result> Handle(DeleteShippingCarrierCommand request, CancellationToken cancellationToken)
    {

        ShippingCarrier shippingCarrier = await repository.FindAsync(request.Id, cancellationToken);
        if (shippingCarrier is null)
            return Result.Failure("شركة الشحن غير موجودة.");

        if (await repository.IsUsedByOrdersAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف شركة الشحن لأنها مرتبطة بطلبات.");
        }

        if (await repository.IsUsedByOrderDeliveryStepsAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف شركة الشحن لأنها مستخدمة في سلسلة التوصيل.");
        }

        if (await tripUsageReader.HasPartyTripsAsync(TripPartyType.ShippingCarrier, request.Id, cancellationToken))
        {
            return Result.Failure(
                "لا يمكن حذف شركة الشحن لارتباطها بمشاوير مستقلة. أغلق المشاوير أولاً ثم أعد المحاولة.",
                "Conflict");
        }

        repository.Remove(shippingCarrier);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure($"خطأ في حذف شركة الشحن {shippingCarrier.Name}.");
        }
    }
}
