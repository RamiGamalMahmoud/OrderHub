using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Update;

internal sealed class UpdateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateProductCommand, Result<int>>
{
    public async Task<Result<int>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var input = request.ProductUpdateInput;
        if (input is null)
        {
            return Result<int>.Failure("بيانات المنتج مطلوبة.");
        }
        var supplierIds = input.SelectedSuppliersIds?.ToArray() ?? [];
        var assignments = input.ProductProperties?.ToArray() ?? [];
        if (assignments.Any(item => item is null))
        {
            return Result<int>.Failure("بيانات الخصائص غير صالحة.");
        }
        var propertyIds = assignments.Select(item => item.PropertyId).ToArray();
        var validation = ProductInputValidation.Validate(input.Name, input.Code, input.Price, input.CategoryId, supplierIds, propertyIds);
        if (!validation.IsSuccess)
        {
            return Result<int>.Failure(validation.ErrorMessage);
        }
        var product = await repository.GetForUpdateAsync(request.id, cancellationToken);
        if (product is null)
        {
            return Result<int>.Failure("المنتج غير موجود.");
        }

        var category = await repository.FindCategoryAsync(input.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result<int>.Failure("القسم المحدد لم يعد موجودًا. اختر قسمًا آخر.");
        }
        var suppliers = await repository.GetSuppliersAsync(supplierIds, cancellationToken);
        if (suppliers.Count != supplierIds.Length)
        {
            return Result<int>.Failure("أحد الموردين المحددين لم يعد موجودًا.");
        }
        var existingProperties = await repository.GetExistingPropertyIdsAsync(propertyIds, cancellationToken);
        if (existingProperties.Count != propertyIds.Length)
        {
            return Result<int>.Failure("إحدى الخصائص المحددة لم تعد موجودة.");
        }
        product.UpdateDetails(input.Name, input.Code, category, new Money(input.Price));
        ReconcileSuppliers(product, suppliers, supplierIds);
        ReconcileProperties(product, assignments);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(product.Id);
        }
        catch (PersistenceException)
        {
            return Result<int>.Failure("تعذر حفظ المنتج. يرجى المحاولة مرة أخرى.");
        }
    }

    private static void ReconcileSuppliers(Product product,
        System.Collections.Generic.IReadOnlyList<Supplier> suppliers, int[] supplierIds)
    {
        var selected = supplierIds.ToHashSet();
        foreach (var supplier in product.Suppliers.Where(item => !selected.Contains(item.Id)).ToArray())
        {
            product.RemoveSupplier(supplier);
        }

        foreach (var supplier in suppliers.Where(item => !product.HasSupplier(item.Id)))
        {
            product.AddSupplier(supplier);
        }
    }

    private static void ReconcileProperties(Product product, ProductPropertyUpdate[] assignments)
    {
        var selected = assignments.Select(item => item.PropertyId).ToHashSet();
        foreach (var assignment in product.Properties.Where(item => !selected.Contains(item.PropertyId)).ToArray())
        {
            product.RemoveProperty(assignment.PropertyId);
        }

        foreach (var assignment in assignments)
        {
            product.AddProperty(assignment.PropertyId, assignment.IsRequired);
            product.ChangePropertyRquirement(assignment.PropertyId, assignment.IsRequired);
        }
    }

}
