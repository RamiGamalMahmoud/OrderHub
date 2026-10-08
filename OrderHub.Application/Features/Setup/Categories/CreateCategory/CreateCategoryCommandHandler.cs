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


        string name = request.CategoryCreateInput.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
            return Result.Failure("اسم القسم يجب أن يتكون من 2 إلى 100 حرف.");

        Category parent = null;
        if (request.CategoryCreateInput.ParentId is int parentId)
        {
            parent = await repository.FindAsync(parentId, cancellationToken);
            if (parent is null)
                return Result.Failure("القسم الرئيسي غير موجود.");
        }

        bool isCategoryExists = await repository.ExistsAsync(name, request.CategoryCreateInput.ParentId, cancellationToken);

        if (isCategoryExists)
        {
            return Result.Failure("هذا القسم موجود بالفعل.");
        }

        Category category = new(name, parent);

        repository.Add(category);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ القسم. يرجى المحاولة مرة أخرى.");
        }
    }
}
