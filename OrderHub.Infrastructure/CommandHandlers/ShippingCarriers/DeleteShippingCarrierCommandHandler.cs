using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ShippingCarriersCommands;

namespace OrderHub.Infrastructure.CommandHandlers.ShippingCarriers;

internal class DeleteShippingCarrierCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteShippingCarrierCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteShippingCarrierCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        ShippingCarrier shippingCarrier = await appDbContext.ShippingCarriers.FindAsync(request.Id);
        if(shippingCarrier is null)
            return Result.Failure("شركة الشحن غير موجودة.");

        if (await appDbContext.Orders.AnyAsync(order => order.ShippingCarrierId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف شركة الشحن لأنها مرتبطة بطلبات.");
        }

        if (await appDbContext.OrderDeliverySteps.AnyAsync(step => step.ShippingCarrierId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف شركة الشحن لأنها مستخدمة في سلسلة التوصيل.");
        }

        if (await appDbContext.ShippingCarrierRecipients.AnyAsync(recipient => recipient.ShippingCarrierId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف شركة الشحن لأنها مرتبطة بسجل الرسائل.");
        }

        appDbContext.ShippingCarriers.Remove(shippingCarrier);

        try
        {
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch(DbUpdateException)
        {
            return Result.Failure($"خطأ في حذف شركة الشحن {shippingCarrier.Name}.");
        }
    }
}
