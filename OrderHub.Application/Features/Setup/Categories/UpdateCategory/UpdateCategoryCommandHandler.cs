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
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        bool transactionEnded = false;
        try
        {
            await repository.LockHierarchyAsync(cancellationToken);
            Result result = await UpdateAsync(request, cancellationToken);
            if (result.IsSuccess)
            {
                transactionEnded = true;
                await unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            return result;
        }
        finally
        {
            if (!transactionEnded)
                await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
    }

    private async Task<Result> UpdateAsync(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {


        Category category = await repository.FindAsync(request.CategoryUpdateInput.Id, cancellationToken);

        if (category is null)
        {
            return Result.Failure("القسم غير موجود.");
        }

        string name = request.CategoryUpdateInput.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 100)
            return Result.Failure("اسم القسم يجب أن يتكون من 2 إلى 100 حرف.");

        Category parent = null;
        if (request.CategoryUpdateInput.ParentId is int parentId)
        {
            if (parentId == category.Id)
                return Result.Failure("لا يمكن اختيار القسم نفسه كقسم رئيسي.");
            parent = await repository.FindAsync(parentId, cancellationToken);
            if (parent is null)
                return Result.Failure("القسم الرئيسي غير موجود.");
            if (await repository.WouldCreateCycleAsync(category.Id, parentId, cancellationToken))
                return Result.Failure("لا يمكن نقل القسم إلى أحد أقسامه الفرعية.");
        }

        if ((category.Name.Value != name || category.ParentCategoryId != request.CategoryUpdateInput.ParentId)
            && await repository.ExistsAsync(name, request.CategoryUpdateInput.ParentId, cancellationToken))
            return Result.Failure("هذا القسم موجود بالفعل.");

        category.UpdateName(name);
        category.MoveTo(parent);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (PersistenceException)
        {
            return Result.Failure("تعذر حفظ التعديلات. يرجى المحاولة مرة أخرى.");
        }
    }
}
