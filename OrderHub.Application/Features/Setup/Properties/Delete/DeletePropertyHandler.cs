using MediatR;
using OrderHub.Application.Features.Setup.Properties.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Setup.Properties.Delete;

internal sealed class DeletePropertyHandler(
    IPropertyRepository repository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeletePropertyCommand, Result>
{
    public async Task<Result> Handle(DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await repository.FindAsync(request.Id, cancellationToken);

        if (property is null)
        {
            return Result.Failure("الخاصية غير موجودة.");
        }

        if (await repository.IsInUseAsync(property.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف خاصية مستخدمة في منتجات أو طلبات.");
        }

        repository.Remove(property);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حذف الخاصية. يرجى المحاولة مرة أخرى.");
        }
    }
}
