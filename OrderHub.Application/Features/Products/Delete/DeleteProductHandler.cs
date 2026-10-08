using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Delete;

internal sealed class DeleteProductHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await repository.FindAsync(request.Id, cancellationToken);
        if (product is null)
        {
            return Result.Failure("المنتج غير موجود.");
        }

        if (await repository.HasBusinessReferencesAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المنتج لأنه مرتبط بطلبات أو مستندات أو مسودات.");
        }

        repository.Remove(product);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حذف المنتج.");
        }
    }
}
