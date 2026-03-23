using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.SupplierCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Suppliers;

internal class DeleteSupplierCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<DeleteSupplierCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Supplier supplier = await appDbContext.Suppliers.FindAsync(new object[] { request.Id }, cancellationToken);
        if (supplier == null)
        {
            return Result.Failure("المورد غير موجود.");
        }

        if (await appDbContext.OrderItems.AnyAsync(item => item.SupplierId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المورد لأنه مرتبط بطلبات.");
        }

        if (await appDbContext.SupplierRecipients.AnyAsync(recipient => recipient.SupplierId == request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المورد لأنه مرتبط بسجل الرسائل.");
        }

        if (await appDbContext.Products.AnyAsync(product => product.Suppliers.Any(s => s.Id == request.Id), cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المورد لأنه مرتبط بمنتجات.");
        }

        try
        {
            appDbContext.Suppliers.Remove(supplier);
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure("تعذر حذف المورد.");
        }
    }
}
