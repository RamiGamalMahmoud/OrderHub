using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class UpdateCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateCategoryCommand, Result>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {


        Category category = await repository.FindAsync(request.CategoryUpdateInput.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure("القسم غير موجود.");
        }

        category.UpdateName(request.CategoryUpdateInput.Name);
        category.ParentCategoryId = request.CategoryUpdateInput.ParentId;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure(request.CategoryUpdateInput.Name);
        }
    }
}
