using OrderHub.Application.Features.Products.Contracts;
using OrderHub.Application.Features.Products.Create;
using OrderHub.Application.Features.Products.Get;
using OrderHub.Application.Features.Products.List;
using OrderHub.Application.Features.Products.Update;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OrderHub.Application.Interfaces;

namespace OrderHub.Application.Features.Products.Update;

internal sealed class UpdateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand, Result<int>>
{
    public async Task<Result<int>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var productDto = request.ProductUpdateInput;
        var productId = request.id;



        Product product = await repository.GetForUpdateAsync(productId, cancellationToken);

        SynchronizeProperties(productDto, product);

        await SynchronizeSuppliers(productDto, repository, product, cancellationToken);

        try
        {
            product.Rename(productDto.Name);
            product.ChangeProductCode(productDto.Code);
            product.UpdatePrice(new Domain.ValueObjects.Money(productDto.Price));
            Category category = await repository.FindCategoryAsync(productDto.CategoryId, cancellationToken);
            if (category is null)
            {
                return Result<int>.Failure($"Category with Id {productDto.CategoryId} not found.");
            }
            product.UpdateCategory(category);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<int>.Success(product.Id);
        }
        catch (Exception)
        {
            return Result<int>.Failure("An error occurred while updating the product.");
        }
    }

    private static void SynchronizeProperties(ProductUpdateInput productDto, Product product)
    {
        var existedProperties = product.Properties.ToDictionary(p => p.PropertyId);
        var requestedProperties = productDto.ProductProperties.ToDictionary(p => p.PropertyId);

        // Remove
        foreach (var property in existedProperties.Values)
        {
            if (!requestedProperties.ContainsKey(property.PropertyId))
            {
                product.RemoveProperty(property.PropertyId);
            }
        }

        // Add & Update
        foreach (var item in requestedProperties.Values)
        {
            if (!existedProperties.TryGetValue(item.PropertyId, out var currentProperty))
            {
                product.AddProperty(item.PropertyId, item.IsRequired);
                continue;
            }

            if (currentProperty.IsRequired != item.IsRequired)
            {
                product.ChangePropertyRquirement(item.PropertyId, item.IsRequired);
            }
        }
    }

    private static async Task SynchronizeSuppliers(ProductUpdateInput productDto, IProductRepository repository, Product product, CancellationToken cancellationToken)
    {
        product.ClearSuppliers();
        IEnumerable<Supplier> selectedSuppliers = await repository.GetSuppliersAsync(productDto.SelectedSuppliersIds, cancellationToken);

        foreach (Supplier supplier in selectedSuppliers)
        {
            product.AddSupplier(supplier);
        }
    }
}
