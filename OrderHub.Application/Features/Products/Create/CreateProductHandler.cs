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

namespace OrderHub.Application.Features.Products.Create;

internal sealed class CreateProductHandler(IProductRepository repository, IUnitOfWork unitOfWork) : IRequestHandler<CreateProductCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var productDto = request.Product;



        Category category = await repository.FindCategoryAsync(productDto.CategoryId, cancellationToken);

        Product product = new Product(
            productDto.Name,
            productDto.Code,
            category,
            new Money(productDto.Price));

        foreach (ProductPropertyInput productProperties in productDto.ProductProperties)
        {
            product.AddProperty(productProperties.PropertyId, productProperties.IsRequired);
        }

        if (productDto.SelectedSuppliersIds.Any())
        {
            IEnumerable<Supplier> suppliers = await repository.GetSuppliersAsync(productDto.SelectedSuppliersIds, cancellationToken);
            foreach (Supplier supplier in suppliers)
            {
                product.AddSupplier(supplier);
            }
        }

        repository.Add(product);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result<int>.Success(product.Id);
        }

        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message);
        }
    }
}
