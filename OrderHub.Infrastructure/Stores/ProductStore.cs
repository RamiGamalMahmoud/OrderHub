using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium.DevTools.V145.Page;
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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Stores;

internal class ProductStore(AppDbContextFactory contextFactory) : IProductStore
{
    private readonly AppDbContextFactory _contextFactory = contextFactory;

    public async Task<Result<int>> CreateAsync(CreateProduct.ProductDto productDto)
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();

        Category category = await dbContext.Categories.FindAsync(productDto.CategoryId);

        Product product = new Product(
            productDto.Name,
            productDto.Code,
            category,
            new Money(productDto.Price));

        foreach (CreateProduct.ProductPropertiesDto productProperties in productDto.ProductProperties)
        {
            product.AddProperty(productProperties.PropertyId, productProperties.IsRequired);
        }

        if (productDto.SelectedSuppliersIds.Any())
        {
            IEnumerable<Supplier> suppliers = await dbContext
                .Suppliers
                .Where(s => productDto.SelectedSuppliersIds.Contains(s.Id))
                .ToListAsync();
            foreach (Supplier supplier in suppliers)
            {
                product.AddSupplier(supplier);
            }
        }

        dbContext.Products.Add(product);

        try
        {
            await dbContext.SaveChangesAsync();
            return Result<int>.Success(product.Id);
        }

        catch (Exception ex)
        {
            return Result<int>.Failure(ex.Message);
        }
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        using AppDbContext appDbContext = _contextFactory.CreateDbContext();
        Product product = await appDbContext.Products.FindAsync(id);

        if (product is null)
            return Result.Failure("المنتج غير موجود.");

        if (await appDbContext.OrderItems.AnyAsync(item => item.ProductId == id, cancellationToken))
        {
            return Result.Failure("لا يمكن حذف المنتج لأنه مرتبط بطلبات.");
        }

        appDbContext.Products.Remove(product);

        try
        {
            await appDbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateException)
        {
            return Result.Failure("تعذر حذف المنتج.");
        }
    }

    public async Task<IEnumerable<ListProducts.ProductDto>> GetListProductsAsync(CancellationToken cancellationToken = default)
    {
        using (AppDbContext dbContext = _contextFactory.CreateDbContext())
        {
            return await dbContext
                .Products
                .AsNoTracking()
                .Select(p => new ListProducts.ProductDto(
                    p.Id,
                    p.Name.Value,
                    p.Price.Value,
                    p.Code,
                    p.Category.Name.Value,
                    p.Suppliers.Select(s => new ListProducts.ProductSupplier(s.Id, s.Name.Value)).ToImmutableList()))
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }

    public async Task<GetProduct.ProductDetails> GetProductAsync(int id, CancellationToken cancellationToken = default)
    {
        using AppDbContext appDbContext = _contextFactory.CreateDbContext();

        return await appDbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new GetProduct.ProductDetails(
                p.Name.Value,
                p.Code,
                p.Price.Value,
                p.Category.Id,
                p.Suppliers.Select(s => s.Id),
                p.Properties.Select(p => new GetProduct.ProductProperty(p.PropertyId, p.IsRequired))))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ProductLookupItem>> GetProductByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        using AppDbContext appDbContext = _contextFactory.CreateDbContext();

        string sql = """
            WITH RECURSIVE category_tree AS (
                -- Anchor member: start with root category
                SELECT id, name, parent_category_id, 0 as level
                FROM categories
                WHERE id = @p0 

                UNION ALL

                -- Recursive member: get children
                SELECT c.id, c.name, c.parent_category_id, ct.level + 1
                FROM categories c
                JOIN category_tree ct ON c.parent_category_id = ct.id
            )
            SELECT * FROM category_tree
            ORDER BY level, name
            """;

        IEnumerable<int> categories = await appDbContext
            .Categories
            .FromSqlRaw(sql, categoryId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        return await appDbContext.Products
            .Where(p => categories.Contains(p.Category.Id))
            .Select(p => new ProductLookupItem(
                p.Id,
                p.Name.Value,
                p.Price.Value,
                p.Code,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new ProductLookupSupplier(s.Id, s.Name.Value)).ToImmutableList()))
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ProductLookupItem>> GetProductsByName(string name, CancellationToken cancellationToken = default)
    {
        using AppDbContext dbContext = _contextFactory.CreateDbContext();

        string searchTerm = name.Trim();

        if (string.IsNullOrEmpty(searchTerm)) return Enumerable.Empty<ProductLookupItem>();

        return await dbContext
            .Products
            .AsNoTracking()
            .Where(p => p.Name.Value.Contains(searchTerm) || p.Suppliers.Any(s => s.Name.Value.Contains(searchTerm)))
            .Select(p => new ProductLookupItem(
                p.Id,
                p.Name.Value,
                p.Price.Value,
                p.Code,
                p.Category.Name.Value,
                p.Suppliers.Select(s => new ProductLookupSupplier(s.Id, s.Name.Value)).ToImmutableList()))
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<Result<int>> UpdateAsync(int productId, UpdateProduct.ProductDto productDto, CancellationToken cancellationToken = default)
    {
        using AppDbContext appDbContext = _contextFactory.CreateDbContext();

        Product product = await appDbContext.Products
            .Where(p => p.Id == productId)
            .Include(p => p.Category)
            .Include(p => p.Suppliers)
            .Include(p => p.Properties)
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        SynchronizeProperties(productDto, product);

        await SynchronizeSuppliers(productDto, appDbContext, product, cancellationToken);

        try
        {
            product.Rename(productDto.Name);
            product.ChangeProductCode(productDto.Code);
            product.UpdatePrice(new Domain.ValueObjects.Money(productDto.Price));
            Category category = await appDbContext.Categories.FindAsync(productDto.CategoryId);
            if (category is null)
            {
                return Result<int>.Failure($"Category with Id {productDto.CategoryId} not found.");
            }
            product.UpdateCategory(category);

            await appDbContext.SaveChangesAsync(cancellationToken);
            
            return Result<int>.Success(product.Id);
        }
        catch (Exception)
        {
            return Result<int>.Failure("An error occurred while updating the product.");
        }
    }

    private static async Task SynchronizeSuppliers(UpdateProduct.ProductDto productDto, AppDbContext appDbContext, Product product, CancellationToken cancellationToken)
    {
        product.ClearSuppliers();
        IEnumerable<Supplier> selectedSuppliers = await appDbContext
            .Suppliers
            .Where(s => productDto.SelectedSuppliersIds.Contains(s.Id))
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (Supplier supplier in selectedSuppliers)
        {
            product.AddSupplier(supplier);
        }
    }

    private static void SynchronizeProperties(UpdateProduct.ProductDto productDto, Product product)
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
}
