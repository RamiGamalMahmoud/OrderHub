using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class DeleteCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<DeleteCategoryCommand, Result>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {

        Category category = await repository.FindAsync(request.Id, cancellationToken);
        if (category is null)
        {
            return Result.Failure("القسم غير موجود.");
        }

        if (await repository.IsUsedByProductsAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف القسم لأنه مرتبط بمنتجات.");
        }

        if (await repository.IsUsedByCategoriesAsync(request.Id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف القسم لأنه يحتوي على أقسام فرعية.");
        }

        repository.Remove(category);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure($"لم يتم حذف القسم {category.Name}");
        }
    }
}
