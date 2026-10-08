using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Suppliers;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Suppliers.Contracts;

namespace OrderHub.Application.Features.Setup.Suppliers;

internal sealed class DeleteSupplierCommandHandler(ISupplierRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteSupplierCommand, Result>
{
    public async Task<Result> Handle(DeleteSupplierCommand request, CancellationToken cancellationToken)
    {


        Supplier supplier = await repository.FindAsync(request.Id, cancellationToken);
        if (supplier == null)
        {
            return Result.Failure("المورد غير موجود.");
        }

        if (await repository.IsUsedByOrderItemsAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المورد لأنه مرتبط بطلبات.");
        }

        if (await repository.IsUsedByProductsAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المورد لأنه مرتبط بمنتجات.");
        }

        try
        {
            repository.Remove(supplier);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حذف المورد.");
        }
    }
}
