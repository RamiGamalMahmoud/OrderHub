using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Deliverymen;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Deliverymen.Contracts;

namespace OrderHub.Application.Features.Setup.Deliverymen;

internal sealed class DeleteDeliverymanCommandHandler(IDeliverymanRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteDeliverymanCommand, Result>
{
    public async Task<Result> Handle(DeleteDeliverymanCommand request, CancellationToken cancellationToken)
    {

        Deliveryman deliveryman = await repository.FindAsync(request.Id, cancellationToken);

        if (deliveryman == null)
        {
            return Result.Failure("المندوب غير موجود.");
        }

        if (await repository.IsUsedByOrdersAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المندوب لأنه مرتبط بطلبات.");
        }

        if (await repository.IsUsedByOrderDeliveryStepsAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المندوب لأنه مستخدم في سلسلة التوصيل.");
        }

        repository.Remove(deliveryman);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حذف المندوب.");
        }
    }
}
