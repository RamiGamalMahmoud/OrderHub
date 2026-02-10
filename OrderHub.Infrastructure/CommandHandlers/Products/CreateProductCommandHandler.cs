using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using OrderHub.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ProductCommands;

namespace OrderHub.Infrastructure.CommandHandlers.Products;

internal class CreateProductCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<CreateProductCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();

        Category category = await appDbContext.Categories.FindAsync(request.ProductCreateDto.CategoryId);

        Product product = new Product(
            request.ProductCreateDto.Name,
            request.ProductCreateDto.Code,
            category,
            new Money(request.ProductCreateDto.Price));

        if (request.ProductCreateDto.SelectedSuppliersIds.Any())
        {
            IEnumerable<Supplier> suppliers = await appDbContext
                .Suppliers
                .Where(s => request.ProductCreateDto.SelectedSuppliersIds.Contains(s.Id))
                .ToListAsync();
            foreach (Supplier supplier in suppliers)
            {
                product.AddSupplier(supplier);
            }
        }

        appDbContext.Products.Add(product);

        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }

        catch
        {
            return Result.Failure();
        }
    }
}
