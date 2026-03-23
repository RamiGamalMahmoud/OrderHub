using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.DeliverymanCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Deliverymen;

internal class DeleteDeliverymanCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteDeliverymanCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteDeliverymanCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        Deliveryman deliveryman = await appDbContext.Deliverymen.FindAsync(request.Id);

        if (deliveryman == null)
        {
            return Result.Failure("المندوب غير موجود.");
        }

        if (await appDbContext.Orders.AnyAsync(order => order.DeliverymanId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المندوب لأنه مرتبط بطلبات.");
        }

        if (await appDbContext.OrderDeliverySteps.AnyAsync(step => step.DeliverymanId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المندوب لأنه مستخدم في سلسلة التوصيل.");
        }

        if (await appDbContext.DeliverymanRecipients.AnyAsync(recipient => recipient.DeliveryManId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المندوب لأنه مرتبط بسجل الرسائل.");
        }

        appDbContext.Deliverymen.Remove(deliveryman);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure("تعذر حذف المندوب.");
        }
    }
}
