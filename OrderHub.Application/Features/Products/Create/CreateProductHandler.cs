using MediatR;
using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Interfaces;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Application.Features.Products.Create;

internal sealed class CreateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var input = request.Product;
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
        var product = new Product(input.Name, input.Code, category, new Money(input.Price));
        foreach (var supplier in suppliers)
        {
            product.AddSupplier(supplier);
        }
        foreach (var assignment in assignments)
        {
            product.AddProperty(assignment.PropertyId, assignment.IsRequired);
        }
        repository.Add(product);

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
}
