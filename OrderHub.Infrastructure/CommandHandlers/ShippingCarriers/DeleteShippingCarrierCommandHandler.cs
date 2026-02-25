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

        appDbContext.ShippingCarriers.Remove(shippingCarrier);

        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch(DbUpdateException)
        {
            return Result.Failure($"خطأ في حذف شركة الشحن {shippingCarrier.Name}.");
        }
    }
}
