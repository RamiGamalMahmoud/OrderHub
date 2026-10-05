using MediatR;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OrderHub.Application.Features.Setup.Categories;
using OrderHub.Application.Interfaces;
using OrderHub.Application.Features.Setup.Categories.Contracts;

namespace OrderHub.Application.Features.Setup.Categories;

internal sealed class CreateCategoryCommandHandler(ICategoryRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<CreateCategoryCommand, Result>
{
    public async Task<Result> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {


        bool isCategoryExists = await repository.ExistsAsync(request.CategoryCreateInput.Name, request.CategoryCreateInput.ParentId, cancellationToken);

        if (isCategoryExists)
        {
            return Result.Failure("هذا القسم موجود بالفعل.");
        }

        Category category = new(request.CategoryCreateInput.Name)
        {
            ParentCategoryId = request.CategoryCreateInput.ParentId
        };

        repository.Add(category);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure(request.CategoryCreateInput.Name);
        }
    }
}
