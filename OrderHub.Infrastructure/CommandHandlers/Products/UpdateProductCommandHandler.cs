using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderHub.Domain.Common;
using OrderHub.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OrderHub.Application.Commands.ProductCommands;
using static OrderHub.Application.DTOs.ProductDtos;

namespace OrderHub.Infrastructure.CommandHandlers.Products;

internal class UpdateProductCommandHandler(AppDbContextFactory appDbContextFactory) : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly AppDbContextFactory _appDbContextFactory = appDbContextFactory;

    public async Task<Result> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _appDbContextFactory.CreateDbContext();
        ProductUpdateDto dto = request.ProductUpdateDto;
        Product product = await appDbContext.Products
            .Where(p => p.Id == dto.Id)
            .Include(p => p.Category)
            .Include(p => p.Suppliers)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        //if (dto.SelectedSuppliersIds.Any())
        {
            product.ClearSuppliers();
            IEnumerable<Supplier> selectedSuppliers = await appDbContext
                .Suppliers
                .Where(s => dto.SelectedSuppliersIds.Contains(s.Id))
                .ToListAsync(cancellationToken: cancellationToken);

            foreach (Supplier supplier in selectedSuppliers)
            {
                product.AddSupplier(supplier);
            }
        }

        try
        {
            product.Rename(dto.Name);
            product.ChangeProductCode(dto.Code);
            product.UpdatePrice(new Domain.ValueObjects.Money(dto.Price));
            Category category = await appDbContext.Categories.FindAsync(dto.CategoryId);
            if (category is null)
            {
                return Result.Failure($"Category with Id {dto.CategoryId} not found.");
            }
            product.UpdateCategory(category);
            await appDbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception)
        {
            return Result.Failure("An error occurred while updating the product.");
        }
    }
}
